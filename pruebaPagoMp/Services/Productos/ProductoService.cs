using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.DTOs;
using pruebaPagoMp.Models;
using System.Globalization;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using System.Text.RegularExpressions;
using pruebaPagoMp.Models.Ventas.Enums;

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

            // Buscamos los productos y calculamos su promo activa (si existe) para el catÃ¡logo.
            return await _context.Productos
                .Where(p => !p.EsTemporalSenia)
                .Select(p => new
                {
                    Producto = p,
                    StockReservado = _context.ReservaItems
                        .Where(ri => ri.ProductoId == p.Id
                            && !ri.EsAConseguir
                            && ri.Reserva.Estado == EstadoReserva.Senada)
                        .Sum(ri => (int?)ri.Cantidad) ?? 0,
                    ProveedorId = _context.ProveedorProductos
                        .Where(pp => pp.ProductoId == p.Id)
                        .OrderByDescending(pp => pp.FechaActualizacion)
                        .Select(pp => (int?)pp.ProveedorId)
                        .FirstOrDefault(),
                    Proveedor = _context.ProveedorProductos
                        .Where(pp => pp.ProductoId == p.Id)
                        .OrderByDescending(pp => pp.FechaActualizacion)
                        .Select(pp => pp.Proveedor.RazonSocial)
                        .FirstOrDefault(),
                    Promocion = _context.Promociones
                        .Where(pr =>
                            pr.Activa &&
                            (
                                pr.ProductoId == p.Id
                                || (
                                    pr.ProductoId == null
                                    && (
                                        pr.Genero == null
                                        || (
                                            (p.Descripcion ?? string.Empty)
                                                .ToLower()
                                                .Replace("\u00E1", "a")
                                                .Replace("\u00E9", "e")
                                                .Replace("\u00ED", "i")
                                                .Replace("\u00F3", "o")
                                                .Replace("\u00FA", "u")
                                                .Contains(
                                                    (pr.Genero ?? string.Empty)
                                                        .ToLower()
                                                        .Replace("\u00E1", "a")
                                                        .Replace("\u00E9", "e")
                                                        .Replace("\u00ED", "i")
                                                        .Replace("\u00F3", "o")
                                                        .Replace("\u00FA", "u")
                                                )
                                        )
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
                    Editorial = x.Producto.Editorial,
                    Precio = x.Producto.Precio,
                    Stock = x.Producto.Stock,
                    StockReservado = x.StockReservado,
                    StockDisponible = x.Producto.Stock - x.StockReservado,
                    ImagenUrl = x.Producto.ImagenUrl,
                    TienePromocionActiva = x.Promocion != null,
                    PromocionNombre = x.Promocion != null ? x.Promocion.Nombre : null,
                    PorcentajeDescuento = x.Promocion != null ? x.Promocion.PorcentajeDescuento : null,
                    MontoDescuento = x.Promocion != null ? x.Promocion.MontoDescuento : null,
                    ProveedorId = x.ProveedorId,
                    ProveedorNombre = x.Proveedor,
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
                Editorial = ResolverEditorial(dto.Editorial, dto.Descripcion),
                Precio = dto.Precio,
                Stock = dto.Stock,
                ImagenUrl = dto.ImagenUrl.Trim(),
                FechaCreacion = DateTime.UtcNow
            };

            _context.Productos.Add(item);
            await _context.SaveChangesAsync();

            if (dto.ProveedorId.HasValue)
            {
                var costo = dto.CostoProveedor.GetValueOrDefault(dto.Precio);
                await AsociarProductoAProveedorAsync(dto.ProveedorId.Value, item.Id, costo);
                await _context.SaveChangesAsync();
            }

            return item.Id;
        }

        public async Task<int> ImportarArchivoAsync(Stream stream, string fileName, int? proveedorId = null)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (ext == ".xlsx")
            {
                return await ImportarDesdeXlsxAsync(stream, proveedorId);
            }

            if (ext == ".csv")
            {
                return await ImportarDesdeCsvAsync(stream, proveedorId);
            }

            throw new InvalidOperationException("Formato no soportado. Usa un archivo .xlsx o .csv.");
        }

        public async Task EliminarAsync(int id)
        {
            if (id <= 0)
                throw new InvalidOperationException("Id de producto invalido.");

            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null)
                throw new KeyNotFoundException("Producto no encontrado.");

            var usadoEnVentas = await _context.DetalleVentas.AnyAsync(x => x.ProductoId == id);
            var usadoEnCompras = await _context.DetalleCompras.AnyAsync(x => x.ProductoId == id);
            var usadoEnPedidos = await _context.PedidoItems.AnyAsync(x => x.ProductoId == id);

            if (usadoEnVentas || usadoEnCompras || usadoEnPedidos)
            {
                throw new InvalidOperationException(
                    "No se puede eliminar el libro porque tiene movimientos historicos (ventas/compras/pedidos).");
            }

            var promos = await _context.Promociones.Where(x => x.ProductoId == id).ToListAsync();
            if (promos.Count > 0) _context.Promociones.RemoveRange(promos);

            var proveedorProductos = await _context.ProveedorProductos.Where(x => x.ProductoId == id).ToListAsync();
            if (proveedorProductos.Count > 0) _context.ProveedorProductos.RemoveRange(proveedorProductos);

            var carritoItems = await _context.CarritoItems.Where(x => x.ProductoId == id).ToListAsync();
            if (carritoItems.Count > 0) _context.CarritoItems.RemoveRange(carritoItems);

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();
        }

        public async Task ActualizarAsync(int id, ActualizarProductoDto dto)
        {
            if (id <= 0)
                throw new InvalidOperationException("Id de producto invalido.");
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new InvalidOperationException("Nombre requerido.");
            if (dto.Precio < 0m)
                throw new InvalidOperationException("Precio invalido.");
            if (dto.Stock < 0)
                throw new InvalidOperationException("Stock invalido.");

            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null)
                throw new KeyNotFoundException("Producto no encontrado.");

            producto.Nombre = dto.Nombre.Trim();
            producto.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
            producto.Editorial = ResolverEditorial(dto.Editorial, dto.Descripcion);
            producto.Precio = dto.Precio;
            producto.Stock = dto.Stock;
            if (!string.IsNullOrWhiteSpace(dto.ImagenUrl))
                producto.ImagenUrl = dto.ImagenUrl.Trim();

            if (dto.ProveedorId.HasValue)
            {
                var costo = dto.CostoProveedor.GetValueOrDefault(dto.Precio);
                await AsociarProductoAProveedorAsync(dto.ProveedorId.Value, id, costo);
            }

            await _context.SaveChangesAsync();
        }

        private async Task<int> ImportarDesdeCsvAsync(Stream stream, int? proveedorId)
        {
            using var reader = new StreamReader(stream);
            var rows = new List<(string Nombre, string? Descripcion, decimal Precio, int Stock, string ImagenBase64, string Genero, string Editorial)>();
            var lineNumber = 0;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (lineNumber == 1) continue; // header

                var parts = line.Split(';');
                if (parts.Length < 5)
                    throw new InvalidOperationException($"CSV invalido en linea {lineNumber}. Formato requerido: nombre;descripcion;precio;stock;imagenBase64");

                var nombre = parts[0].Trim();
                var descripcion = string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1].Trim();
                var precioTxt = parts[2].Trim().Replace(',', '.');
                var stockTxt = parts[3].Trim();
                var resto = parts.Skip(4).ToList();
                var imagenBase64 = string.Empty;
                var genero = string.Empty;
                var editorial = string.Empty;

                if (resto.Count >= 3)
                {
                    genero = resto[^2].Trim();
                    editorial = resto[^1].Trim();
                    imagenBase64 = string.Join(";", resto.Take(resto.Count - 2)).Trim();
                }
                else if (resto.Count >= 1)
                {
                    imagenBase64 = string.Join(";", resto).Trim();
                }

                if (string.IsNullOrWhiteSpace(nombre)) continue;
                if (!decimal.TryParse(precioTxt, NumberStyles.Any, CultureInfo.InvariantCulture, out var precio))
                    throw new InvalidOperationException($"CSV invalido en linea {lineNumber}. Precio incorrecto.");
                if (!int.TryParse(stockTxt, out var stock))
                    throw new InvalidOperationException($"CSV invalido en linea {lineNumber}. Stock incorrecto.");
                if (string.IsNullOrWhiteSpace(imagenBase64))
                    throw new InvalidOperationException($"CSV invalido en linea {lineNumber}. La imagenBase64 es obligatoria.");

                var descripcionFinal = ConstruirDescripcionEstandar(descripcion, genero, editorial, $"CSV linea {lineNumber}");
                var generoFinal = ExtraerMetadata(descripcionFinal, "Genero")!;
                var editorialFinal = ExtraerMetadata(descripcionFinal, "Editorial")!;

                rows.Add((nombre, descripcionFinal, precio, stock, imagenBase64, generoFinal, editorialFinal));
            }

            return await GuardarFilasImportadasAsync(rows, proveedorId);
        }

        private async Task<int> ImportarDesdeXlsxAsync(Stream stream, int? proveedorId)
        {
            XLWorkbook wb;
            try
            {
                wb = new XLWorkbook(stream);
            }
            catch (ArgumentException ex) when (ex.Message.Contains("Picture names cannot contain", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "El Excel contiene imagenes incrustadas con nombres invalidos. Quita las imagenes embebidas de la hoja y carga la imagen en la columna imagenBase64 usando una URL http/https, un valor data:image/... o base64 puro.");
            }

            using (wb)
            {
                var ws = wb.Worksheets.FirstOrDefault() ?? throw new InvalidOperationException("El archivo Excel no tiene hojas.");
                var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
                if (lastRow <= 1) return 0;

                var rows = new List<(string Nombre, string? Descripcion, decimal Precio, int Stock, string ImagenBase64, string Genero, string Editorial)>();

                for (var r = 2; r <= lastRow; r++)
                {
                    var nombre = ws.Cell(r, 1).GetString().Trim();
                    var descripcionRaw = ws.Cell(r, 2).GetString().Trim();
                    var precioTxt = ws.Cell(r, 3).GetString().Trim().Replace(',', '.');
                    var stockTxt = ws.Cell(r, 4).GetString().Trim();
                    var imagenBase64 = ws.Cell(r, 5).GetString().Trim();
                    var genero = ws.Cell(r, 6).GetString().Trim();
                    var editorial = ws.Cell(r, 7).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(imagenBase64))
                    {
                        imagenBase64 = ObtenerImagenBase64DesdeFila(ws, r) ?? string.Empty;
                    }

                    if (string.IsNullOrWhiteSpace(nombre)) continue;
                    if (!decimal.TryParse(precioTxt, NumberStyles.Any, CultureInfo.InvariantCulture, out var precio))
                        throw new InvalidOperationException($"Excel invalido en fila {r}. Precio incorrecto.");
                    if (!int.TryParse(stockTxt, out var stock))
                        throw new InvalidOperationException($"Excel invalido en fila {r}. Stock incorrecto.");
                    if (string.IsNullOrWhiteSpace(imagenBase64))
                        throw new InvalidOperationException($"Excel invalido en fila {r}. Debes informar la imagen en la columna imagenBase64 con una URL http/https, un valor data:image/... o base64 puro.");

                    var descripcionFinal = ConstruirDescripcionEstandar(
                        string.IsNullOrWhiteSpace(descripcionRaw) ? null : descripcionRaw,
                        genero,
                        editorial,
                        $"Excel fila {r}");
                    var generoFinal = ExtraerMetadata(descripcionFinal, "Genero")!;
                    var editorialFinal = ExtraerMetadata(descripcionFinal, "Editorial")!;

                    rows.Add((nombre, descripcionFinal, precio, stock, imagenBase64, generoFinal, editorialFinal));
                }

                return await GuardarFilasImportadasAsync(rows, proveedorId);
            }
        }

        private async Task<int> GuardarFilasImportadasAsync(
            List<(string Nombre, string? Descripcion, decimal Precio, int Stock, string ImagenBase64, string Genero, string Editorial)> rows,
            int? proveedorId)
        {
            var created = 0;
            var productosCreados = new List<Producto>();
            foreach (var row in rows)
            {
                var nuevo = new Producto
                {
                    Nombre = row.Nombre,
                    Descripcion = row.Descripcion,
                    Editorial = row.Editorial,
                    Precio = row.Precio,
                    Stock = row.Stock,
                    ImagenUrl = NormalizarReferenciaImagen(row.ImagenBase64),
                    FechaCreacion = DateTime.UtcNow
                };
                _context.Productos.Add(nuevo);
                productosCreados.Add(nuevo);
                created++;
            }

            if (created > 0)
            {
                await _context.SaveChangesAsync();

                if (proveedorId.HasValue)
                {
                    foreach (var p in productosCreados)
                    {
                        await AsociarProductoAProveedorAsync(proveedorId.Value, p.Id, p.Precio);
                    }
                    await _context.SaveChangesAsync();
                }
            }
            return created;
        }

        private async Task AsociarProductoAProveedorAsync(int proveedorId, int productoId, decimal costoUnitario)
        {
            if (proveedorId <= 0) throw new InvalidOperationException("Proveedor invalido.");
            if (productoId <= 0) throw new InvalidOperationException("Producto invalido.");

            var proveedorExiste = await _context.Proveedores.AnyAsync(p => p.Id == proveedorId && p.Activo);
            if (!proveedorExiste)
                throw new InvalidOperationException("Proveedor inexistente o inactivo.");

            var costo = costoUnitario > 0m ? costoUnitario : 0.01m;

            var existente = await _context.ProveedorProductos
                .FirstOrDefaultAsync(x => x.ProveedorId == proveedorId && x.ProductoId == productoId);

            if (existente == null)
            {
                _context.ProveedorProductos.Add(new Models.Compras.ProveedorProducto
                {
                    ProveedorId = proveedorId,
                    ProductoId = productoId,
                    CostoUnitario = costo,
                    FechaActualizacion = DateTime.UtcNow
                });
                return;
            }

            existente.CostoUnitario = costo;
            existente.FechaActualizacion = DateTime.UtcNow;
        }

        private static string ConstruirDescripcionEstandar(string? descripcion, string? genero, string? editorial, string origen)
        {
            var descripcionBase = LimpiarMetadata(descripcion);
            var generoFinal = string.IsNullOrWhiteSpace(genero) ? ExtraerMetadata(descripcion, "Genero") : genero.Trim();
            var editorialFinal = string.IsNullOrWhiteSpace(editorial) ? ExtraerMetadata(descripcion, "Editorial") : editorial.Trim();

            if (string.IsNullOrWhiteSpace(generoFinal) || string.IsNullOrWhiteSpace(editorialFinal))
                throw new InvalidOperationException(
                    $"{origen}: Debes informar genero y editorial (columnas 6 y 7 o incluidos en descripcion como 'Editorial: ... Genero: ...').");

            return $"Editorial: {editorialFinal}. Genero: {generoFinal}. {descripcionBase}".Trim();
        }

        private static string LimpiarMetadata(string? descripcion)
        {
            if (string.IsNullOrWhiteSpace(descripcion))
                return string.Empty;

            var texto = descripcion.Trim();
            texto = Regex.Replace(texto, @"^\s*Editorial\s*:\s*[^.]+\.?\s*", "", RegexOptions.IgnoreCase);
            texto = Regex.Replace(texto, @"^\s*G[Ã©e]nero\s*:\s*[^.]+\.?\s*", "", RegexOptions.IgnoreCase);
            texto = Regex.Replace(texto, @"^\s*Genero\s*:\s*[^.]+\.?\s*", "", RegexOptions.IgnoreCase);
            texto = Regex.Replace(texto, @"^\s*Editorial\s*:\s*[^.]+\.?\s*", "", RegexOptions.IgnoreCase);
            return texto.Trim();
        }

        private static string? ExtraerMetadata(string? descripcion, string campo)
        {
            if (string.IsNullOrWhiteSpace(descripcion))
                return null;

            var campoPattern = campo.Equals("Genero", StringComparison.OrdinalIgnoreCase)
                ? @"(?:Genero|G[Ã©e]nero)"
                : campo;
            var match = Regex.Match(descripcion, $@"{campoPattern}\s*:\s*([^.]+)", RegexOptions.IgnoreCase);
            if (!match.Success) return null;

            var valor = match.Groups[1].Value.Trim();
            return string.IsNullOrWhiteSpace(valor) ? null : valor;
        }

        private static string? ResolverEditorial(string? editorial, string? descripcion)
        {
            var valor = string.IsNullOrWhiteSpace(editorial) ? ExtraerMetadata(descripcion, "Editorial") : editorial.Trim();
            return string.IsNullOrWhiteSpace(valor) ? null : valor;
        }

        private static string NormalizarReferenciaImagen(string input)
        {
            var value = input.Trim().Trim('"', '\'');
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("La imagen informada esta vacia.");

            if (value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                return value;

            if (value.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            {
                value = $"https://{value}";
            }

            if (Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return value;
            }

            if (PareceUrlInvalida(value))
            {
                throw new InvalidOperationException(
                    $"La imagen '{value}' no es una URL valida. Usa una URL absoluta http/https, un valor data:image/... o base64 puro.");
            }

            if (!EsBase64Valido(value))
            {
                throw new InvalidOperationException(
                    $"La imagen '{value}' no tiene un formato valido. Usa una URL absoluta http/https, un valor data:image/... o base64 puro.");
            }

            const string pngPrefix = "data:image/png;base64,";
            return pngPrefix + value;
        }

        private static bool PareceUrlInvalida(string value)
        {
            return value.Contains("://", StringComparison.Ordinal)
                || value.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                || value.Contains('/')
                || value.Contains('\\')
                || value.Contains(".com", StringComparison.OrdinalIgnoreCase)
                || value.Contains(".net", StringComparison.OrdinalIgnoreCase)
                || value.Contains(".org", StringComparison.OrdinalIgnoreCase)
                || value.Contains(".ar", StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsBase64Valido(string value)
        {
            var sanitized = Regex.Replace(value, @"\s+", "");
            if (sanitized.Length < 16 || sanitized.Length % 4 != 0)
                return false;

            if (!Regex.IsMatch(sanitized, @"^[A-Za-z0-9+/]*={0,2}$"))
                return false;

            try
            {
                Convert.FromBase64String(sanitized);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static string? ObtenerImagenBase64DesdeFila(IXLWorksheet ws, int rowNumber)
        {
            var picture = ws.Pictures.FirstOrDefault(p =>
                p.TopLeftCell?.Address.RowNumber == rowNumber
                || (p.TopLeftCell?.Address.RowNumber <= rowNumber && p.BottomRightCell?.Address.RowNumber >= rowNumber));

            if (picture?.ImageStream == null)
                return null;

            var stream = picture.ImageStream;
            if (stream.CanSeek) stream.Position = 0;

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            if (ms.Length == 0) return null;

            var mime = MimeDesdeFormato(picture.Format);
            var base64 = Convert.ToBase64String(ms.ToArray());
            return $"data:{mime};base64,{base64}";
        }

        private static string MimeDesdeFormato(XLPictureFormat format)
        {
            return format.ToString().ToLowerInvariant() switch
            {
                "png" => "image/png",
                "jpeg" => "image/jpeg",
                "jpg" => "image/jpeg",
                "gif" => "image/gif",
                "bmp" => "image/bmp",
                "tiff" => "image/tiff",
                "icon" => "image/x-icon",
                "webp" => "image/webp",
                _ => "image/png"
            };
        }
    }
}


