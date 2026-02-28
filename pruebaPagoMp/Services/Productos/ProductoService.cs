using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.DTOs;
using pruebaPagoMp.Models;
using System.Globalization;
using ClosedXML.Excel;

namespace pruebaPagoMp.Services
{
    public class ProductoService : IProductoService
    {
        private readonly ApplicationDbContext _context;

        public ProductoService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<IEnumerable<ProductoDto>> GetCatalogoAsync()
        {
            var hoy = DateTime.Today;

            // Buscamos los productos y calculamos su promo activa (si existe) para el catálogo.
            return await _context.Productos
                .Select(p => new
                {
                    Producto = p,
                    Promocion = _context.Promociones
                        .Where(pr =>
                            pr.Activa &&
                            (
                                pr.ProductoId == p.Id
                                || (
                                    pr.ProductoId == null
                                    && (
                                        pr.Genero == null
                                        || (p.Descripcion != null && p.Descripcion.ToLower().Contains(pr.Genero.ToLower()))
                                    )
                                )
                            ) &&
                            pr.FechaInicio.Date <= hoy &&
                            pr.FechaFin.Date >= hoy)
                        .OrderByDescending(pr => pr.ProductoId == p.Id)
                        .ThenByDescending(pr => pr.PorcentajeDescuento ?? 0m)
                        .ThenByDescending(pr => pr.MontoDescuento ?? 0m)
                        .FirstOrDefault()
                })
                .Select(x => new ProductoDto
                {
                    Id = x.Producto.Id,
                    Nombre = x.Producto.Nombre,
                    Descripcion = x.Producto.Descripcion,
                    Precio = x.Producto.Precio,
                    Stock = x.Producto.Stock,
                    ImagenUrl = x.Producto.ImagenUrl,
                    TienePromocionActiva = x.Promocion != null,
                    PromocionNombre = x.Promocion != null ? x.Promocion.Nombre : null,
                    PorcentajeDescuento = x.Promocion != null ? x.Promocion.PorcentajeDescuento : null,
                    MontoDescuento = x.Promocion != null ? x.Promocion.MontoDescuento : null,
                    PrecioFinal = x.Promocion == null
                        ? null
                        : (
                            x.Producto.Precio
                            - ((x.Promocion.PorcentajeDescuento ?? 0m) / 100m * x.Producto.Precio)
                            - (x.Promocion.MontoDescuento ?? 0m)
                          ) < 0m
                            ? 0m
                            : (
                                x.Producto.Precio
                                - ((x.Promocion.PorcentajeDescuento ?? 0m) / 100m * x.Producto.Precio)
                                - (x.Promocion.MontoDescuento ?? 0m)
                              )
                })
                .ToListAsync();
        }

        public async Task<int> CrearAsync(CrearProductoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new InvalidOperationException("Nombre requerido.");
            if (string.IsNullOrWhiteSpace(dto.ImagenUrl))
                throw new InvalidOperationException("La imagen del libro es obligatoria.");
            if (dto.Precio < 0m)
                throw new InvalidOperationException("Precio invalido.");
            if (dto.Stock < 0)
                throw new InvalidOperationException("Stock invalido.");

            var item = new Producto
            {
                Nombre = dto.Nombre.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
                Precio = dto.Precio,
                Stock = dto.Stock,
                ImagenUrl = dto.ImagenUrl.Trim(),
                FechaCreacion = DateTime.UtcNow
            };

            _context.Productos.Add(item);
            await _context.SaveChangesAsync();
            return item.Id;
        }

        public async Task<int> ImportarArchivoAsync(Stream stream, string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (ext == ".xlsx")
            {
                return await ImportarDesdeXlsxAsync(stream);
            }

            if (ext == ".csv")
            {
                return await ImportarDesdeCsvAsync(stream);
            }

            throw new InvalidOperationException("Formato no soportado. Usa un archivo .xlsx o .csv.");
        }

        private async Task<int> ImportarDesdeCsvAsync(Stream stream)
        {
            using var reader = new StreamReader(stream);
            var rows = new List<(string Nombre, string? Descripcion, decimal Precio, int Stock, string ImagenBase64)>();
            var lineNumber = 0;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (lineNumber == 1) continue; // header

                var parts = line.Split(';', 5);
                if (parts.Length < 5)
                    throw new InvalidOperationException($"CSV invalido en linea {lineNumber}. Formato requerido: nombre;descripcion;precio;stock;imagenBase64");

                var nombre = parts[0].Trim();
                var descripcion = string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1].Trim();
                var precioTxt = parts[2].Trim().Replace(',', '.');
                var stockTxt = parts[3].Trim();
                var imagenBase64 = parts[4].Trim();

                if (string.IsNullOrWhiteSpace(nombre)) continue;
                if (!decimal.TryParse(precioTxt, NumberStyles.Any, CultureInfo.InvariantCulture, out var precio))
                    throw new InvalidOperationException($"CSV invalido en linea {lineNumber}. Precio incorrecto.");
                if (!int.TryParse(stockTxt, out var stock))
                    throw new InvalidOperationException($"CSV invalido en linea {lineNumber}. Stock incorrecto.");
                if (string.IsNullOrWhiteSpace(imagenBase64))
                    throw new InvalidOperationException($"CSV invalido en linea {lineNumber}. La imagenBase64 es obligatoria.");

                rows.Add((nombre, descripcion, precio, stock, imagenBase64));
            }

            return await GuardarFilasImportadasAsync(rows);
        }

        private async Task<int> ImportarDesdeXlsxAsync(Stream stream)
        {
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.FirstOrDefault() ?? throw new InvalidOperationException("El archivo Excel no tiene hojas.");
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            if (lastRow <= 1) return 0;

            var rows = new List<(string Nombre, string? Descripcion, decimal Precio, int Stock, string ImagenBase64)>();

            for (var r = 2; r <= lastRow; r++)
            {
                var nombre = ws.Cell(r, 1).GetString().Trim();
                var descripcionRaw = ws.Cell(r, 2).GetString().Trim();
                var precioTxt = ws.Cell(r, 3).GetString().Trim().Replace(',', '.');
                var stockTxt = ws.Cell(r, 4).GetString().Trim();
                var imagenBase64 = ws.Cell(r, 5).GetString().Trim();

                if (string.IsNullOrWhiteSpace(nombre)) continue;
                if (!decimal.TryParse(precioTxt, NumberStyles.Any, CultureInfo.InvariantCulture, out var precio))
                    throw new InvalidOperationException($"Excel invalido en fila {r}. Precio incorrecto.");
                if (!int.TryParse(stockTxt, out var stock))
                    throw new InvalidOperationException($"Excel invalido en fila {r}. Stock incorrecto.");
                if (string.IsNullOrWhiteSpace(imagenBase64))
                    throw new InvalidOperationException($"Excel invalido en fila {r}. La imagenBase64 es obligatoria.");

                rows.Add((nombre, string.IsNullOrWhiteSpace(descripcionRaw) ? null : descripcionRaw, precio, stock, imagenBase64));
            }

            return await GuardarFilasImportadasAsync(rows);
        }

        private async Task<int> GuardarFilasImportadasAsync(List<(string Nombre, string? Descripcion, decimal Precio, int Stock, string ImagenBase64)> rows)
        {
            var created = 0;
            foreach (var row in rows)
            {
                _context.Productos.Add(new Producto
                {
                    Nombre = row.Nombre,
                    Descripcion = row.Descripcion,
                    Precio = row.Precio,
                    Stock = row.Stock,
                    ImagenUrl = NormalizarImagenBase64(row.ImagenBase64),
                    FechaCreacion = DateTime.UtcNow
                });
                created++;
            }

            if (created > 0) await _context.SaveChangesAsync();
            return created;
        }

        private static string NormalizarImagenBase64(string input)
        {
            var value = input.Trim().Trim('"');
            if (value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                return value;

            const string pngPrefix = "data:image/png;base64,";
            return pngPrefix + value;
        }
    }
}
