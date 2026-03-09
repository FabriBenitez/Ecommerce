using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.Dtos.Compras;
using pruebaPagoMp.Models.Bitacora;
using pruebaPagoMp.Models.Caja;
using pruebaPagoMp.Models.Compras;
using pruebaPagoMp.Models.Compras.Enums;
using pruebaPagoMp.Models;
using pruebaPagoMp.Models.Caja.Enums;
using pruebaPagoMp.Models.Ventas.Enums;
using pruebaPagoMp.Security;
using pruebaPagoMp.Services.Bitacora;
using System.Text.RegularExpressions;
using System.Globalization;
using ClosedXML.Excel;

namespace pruebaPagoMp.Services.Compras;

public class ComprasService : IComprasService
{
    private const string CompraProveedorConceptPrefix = "COMPRA_PROVEEDOR:";
    private readonly ApplicationDbContext _context;
    private readonly ICryptoService _cryptoService;
    private readonly IDigitoVerificadorService _dvService;
    private readonly IBitacoraService _bitacoraService;

    public ComprasService(
        ApplicationDbContext context,
        ICryptoService cryptoService,
        IDigitoVerificadorService dvService,
        IBitacoraService bitacoraService)
    {
        _context = context;
        _cryptoService = cryptoService;
        _dvService = dvService;
        _bitacoraService = bitacoraService;
    }
    public async Task<int> CrearProveedorAsync(ProveedorCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RazonSocial)) throw new InvalidOperationException("RazonSocial requerida.");
        if (string.IsNullOrWhiteSpace(dto.CUIT)) throw new InvalidOperationException("CUIT requerido.");
        var cuitNormalizado = NormalizarCuit(dto.CUIT);

        var proveedor = new Proveedor
        {
            RazonSocial = dto.RazonSocial.Trim(),
            CUIT = _cryptoService.Encrypt(cuitNormalizado),
            CuitHash = SecurityHashing.Sha256Normalized(cuitNormalizado),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
            Activo = dto.Activo
        };
        await _dvService.RecalcularEntidadAsync(proveedor);

        _context.Proveedores.Add(proveedor);
        await _context.SaveChangesAsync();
        await _dvService.RecalcularDVVAsync("Proveedores");
        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            Accion = "PROVEEDOR_CREAR",
            Detalle = $"Proveedor #{proveedor.Id} creado.",
            Resultado = "OK"
        });
        return proveedor.Id;
    }

    public async Task<List<ProveedorListDto>> ListarProveedoresAsync(bool? activos)
    {
        var q = _context.Proveedores.AsNoTracking();

        if (activos.HasValue)
            q = q.Where(p => p.Activo == activos.Value);

        var data = await q
            .OrderBy(p => p.RazonSocial)
            .Select(p => new ProveedorListDto
            {
                Id = p.Id,
                RazonSocial = p.RazonSocial,
                CUIT = p.CUIT,
                Email = p.Email,
                Telefono = p.Telefono,
                Activo = p.Activo
            })
            .ToListAsync();

        data.ForEach(p => p.CUIT = SafeDecrypt(p.CUIT));
        return data;
    }

    public async Task<ProveedorListDto?> ObtenerProveedorPorIdAsync(int id)
    {
        var data = await _context.Proveedores
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProveedorListDto
            {
                Id = p.Id,
                RazonSocial = p.RazonSocial,
                CUIT = p.CUIT,
                Email = p.Email,
                Telefono = p.Telefono,
                Activo = p.Activo
            })
            .FirstOrDefaultAsync();
        
        if (data != null) data.CUIT = SafeDecrypt(data.CUIT);
        return data;
    }

    public async Task ActualizarProveedorAsync(int id, ProveedorCreateDto dto)
    {
        var proveedor = await _context.Proveedores.FirstOrDefaultAsync(p => p.Id == id);
        if (proveedor == null) throw new InvalidOperationException("Proveedor inexistente.");
        var cuitNormalizado = NormalizarCuit(dto.CUIT);

        proveedor.RazonSocial = dto.RazonSocial.Trim();
        proveedor.CUIT = _cryptoService.Encrypt(cuitNormalizado);
        proveedor.CuitHash = SecurityHashing.Sha256Normalized(cuitNormalizado);
        proveedor.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        proveedor.Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
        proveedor.Activo = dto.Activo;
        await _dvService.RecalcularEntidadAsync(proveedor);

        await _context.SaveChangesAsync();
        await _dvService.RecalcularDVVAsync("Proveedores");
        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            Accion = "PROVEEDOR_ACTUALIZAR",
            Detalle = $"Proveedor #{proveedor.Id} actualizado.",
            Resultado = "OK"
        });
    }

    public async Task<List<ProveedorProductoListDto>> ListarProductosPorProveedorAsync(int proveedorId)
    {
        return await _context.ProveedorProductos
            .AsNoTracking()
            .Where(x => x.ProveedorId == proveedorId)
            .Include(x => x.Producto)
            .OrderBy(x => x.Producto.Nombre)
            .Select(x => new ProveedorProductoListDto
            {
                ProductoId = x.ProductoId,
                Nombre = x.Producto.Nombre,
                CostoUnitario = x.CostoUnitario,
                StockActual = x.Producto.Stock
            })
            .ToListAsync();
    }

    public async Task UpsertProductoProveedorAsync(int proveedorId, ProveedorProductoUpsertDto dto)
    {
        if (dto.ProductoId <= 0) throw new InvalidOperationException("ProductoId invalido.");
        if (dto.CostoUnitario <= 0m) throw new InvalidOperationException("Costo unitario invalido.");

        var proveedorExiste = await _context.Proveedores.AnyAsync(p => p.Id == proveedorId);
        if (!proveedorExiste) throw new InvalidOperationException("Proveedor inexistente.");

        var productoExiste = await _context.Productos.AnyAsync(p => p.Id == dto.ProductoId);
        if (!productoExiste) throw new InvalidOperationException("Producto inexistente.");

        var existente = await _context.ProveedorProductos
            .FirstOrDefaultAsync(x => x.ProveedorId == proveedorId && x.ProductoId == dto.ProductoId);

        if (existente == null)
        {
            _context.ProveedorProductos.Add(new ProveedorProducto
            {
                ProveedorId = proveedorId,
                ProductoId = dto.ProductoId,
                CostoUnitario = dto.CostoUnitario,
                FechaActualizacion = DateTime.UtcNow
            });
        }
        else
        {
            existente.CostoUnitario = dto.CostoUnitario;
            existente.FechaActualizacion = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<int> ReemplazarProductosProveedorDesdeArchivoAsync(int proveedorId, Stream stream, string fileName)
    {
        var proveedorExiste = await _context.Proveedores.AnyAsync(p => p.Id == proveedorId);
        if (!proveedorExiste) throw new InvalidOperationException("Proveedor inexistente.");

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var rows = ext switch
        {
            ".csv" => await LeerCatalogoCsvAsync(stream),
            ".xlsx" => await LeerCatalogoXlsxAsync(stream),
            _ => throw new InvalidOperationException("Formato no soportado. Usa .csv o .xlsx.")
        };

        if (rows.Count == 0)
            throw new InvalidOperationException("El archivo no contiene filas validas.");

        var anteriores = await _context.ProveedorProductos
            .Where(x => x.ProveedorId == proveedorId)
            .ToListAsync();
        if (anteriores.Count > 0) _context.ProveedorProductos.RemoveRange(anteriores);

        var ahora = DateTime.UtcNow;
        foreach (var row in rows)
        {
            var productoId = await ResolverProductoIdDesdeCatalogoAsync(row);
            _context.ProveedorProductos.Add(new ProveedorProducto
            {
                ProveedorId = proveedorId,
                ProductoId = productoId,
                CostoUnitario = row.CostoUnitario,
                FechaActualizacion = ahora
            });
        }

        await _context.SaveChangesAsync();
        return rows.Count;
    }

    public async Task<int> CrearCompraAsync(CrearCompraDto dto)
    {
        if (dto.ProveedorId <= 0) throw new InvalidOperationException("ProveedorId inválido.");
        if (dto.Items == null || dto.Items.Count == 0) throw new InvalidOperationException("Debe incluir items.");

        var proveedor = await _context.Proveedores.FirstOrDefaultAsync(p => p.Id == dto.ProveedorId);
        if (proveedor == null) throw new InvalidOperationException("Proveedor inexistente.");
        if (!proveedor.Activo) throw new InvalidOperationException("Proveedor inactivo.");

        // validar items
        foreach (var it in dto.Items)
        {
            if (it.ProductoId <= 0) throw new InvalidOperationException("ProductoId inválido.");
            if (it.Cantidad <= 0) throw new InvalidOperationException("Cantidad debe ser > 0.");
            if (it.CostoUnitario <= 0) throw new InvalidOperationException("CostoUnitario debe ser > 0.");
        }

        // validar productos existen
        var ids = dto.Items.Select(i => i.ProductoId).Distinct().ToList();
        var productos = await _context.Productos
            .Where(p => ids.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

        if (productos.Count != ids.Count)
            throw new InvalidOperationException("Hay productos inexistentes en la compra.");

        try
        {
            var asociados = await _context.ProveedorProductos
                .Where(x => x.ProveedorId == dto.ProveedorId)
                .Select(x => x.ProductoId)
                .ToListAsync();
            if (asociados.Count == 0)
                throw new InvalidOperationException("El proveedor no tiene libros asociados.");

            var noAsociados = ids.Where(id => !asociados.Contains(id)).ToList();
            if (noAsociados.Count > 0)
                throw new InvalidOperationException("Hay items que no estan asociados al proveedor seleccionado.");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch
        {
            // Compatibilidad temporal: si la tabla de catalogo por proveedor no existe
            // o hay desfasaje de esquema, no romper la compra con 500.
        }

        var compra = new Compra
        {
            ProveedorId = dto.ProveedorId,
            EstadoCompra = EstadoCompra.Pendiente,
            Total = 0m
        };

        _context.Compras.Add(compra);
        await _context.SaveChangesAsync();

        decimal total = 0m;

        foreach (var it in dto.Items)
        {
            var sub = it.CostoUnitario * it.Cantidad;
            total += sub;

            _context.DetalleCompras.Add(new DetalleCompra
            {
                CompraId = compra.Id,
                ProductoId = it.ProductoId,
                Cantidad = it.Cantidad,
                CostoUnitario = it.CostoUnitario,
                Subtotal = sub
            });
        }

        compra.Total = total;
        await _dvService.RecalcularEntidadAsync(compra);
        await _context.SaveChangesAsync();
        await _dvService.RecalcularDVVAsync("Compras");
        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            Accion = "COMPRA_CREAR",
            Detalle = $"Compra #{compra.Id} creada.",
            Resultado = "OK"
        });

        return compra.Id;
    }

    public async Task ConfirmarCompraAsync(int compraId)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();

        var compra = await _context.Compras
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.Id == compraId);

        if (compra == null) throw new InvalidOperationException("Compra inexistente.");
        if (compra.EstadoCompra != EstadoCompra.Pendiente)
            throw new InvalidOperationException("Solo se puede confirmar una compra Pendiente.");

        // aumentar stock
        foreach (var det in compra.Detalles)
        {
            var prod = await _context.Productos.FirstAsync(p => p.Id == det.ProductoId);
            prod.Stock += det.Cantidad;
        }

        compra.EstadoCompra = EstadoCompra.Confirmada;
        await _dvService.RecalcularEntidadAsync(compra);

        await _context.SaveChangesAsync();
        await tx.CommitAsync();
        await _dvService.RecalcularDVVAsync("Compras");
        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            Accion = "COMPRA_CONFIRMAR",
            Detalle = $"Compra #{compra.Id} confirmada.",
            Resultado = "OK"
        });
    }

    public async Task<List<CompraListDto>> ListarComprasAsync(int? proveedorId)
    {
        var q = _context.Compras
            .AsNoTracking()
            .Include(c => c.Proveedor)
            .AsQueryable();

        if (proveedorId.HasValue)
            q = q.Where(c => c.ProveedorId == proveedorId.Value);

        return await q
            .OrderByDescending(c => c.Fecha)
            .Select(c => new CompraListDto
            {
                Id = c.Id,
                Fecha = c.Fecha,
                Proveedor = c.Proveedor.RazonSocial,
                Total = c.Total,
                EstadoCompra = c.EstadoCompra
            })
            .ToListAsync();
    }

    public async Task<object?> ObtenerCompraDetalleAsync(int compraId)
    {
        var compra = await _context.Compras
            .AsNoTracking()
            .Include(c => c.Proveedor)
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(c => c.FacturaProveedor)
            .FirstOrDefaultAsync(c => c.Id == compraId);

        if (compra == null) return null;

        return new
        {
            compra.Id,
            compra.Fecha,
            compra.Total,
            compra.EstadoCompra,
            Proveedor = new { compra.ProveedorId, compra.Proveedor.RazonSocial, CUIT = SafeDecrypt(compra.Proveedor.CUIT) },
            Factura = compra.FacturaProveedor == null ? null : new
            {
                compra.FacturaProveedor.Id,
                compra.FacturaProveedor.Numero,
                compra.FacturaProveedor.Fecha,
                compra.FacturaProveedor.Monto
            },
            Detalles = compra.Detalles.Select(d => new
            {
                d.ProductoId,
                Producto = d.Producto.Nombre,
                d.Cantidad,
                d.CostoUnitario,
                d.Subtotal
            }).ToList()
        };
    }

    public async Task RegistrarFacturaProveedorAsync(int compraId, RegistrarFacturaProveedorDto dto)
    {
        var compra = await _context.Compras
            .Include(c => c.FacturaProveedor)
            .FirstOrDefaultAsync(c => c.Id == compraId);

        if (compra == null) throw new InvalidOperationException("Compra inexistente.");
        if (compra.EstadoCompra != EstadoCompra.Confirmada)
            throw new InvalidOperationException("La factura se registra solo con compra Confirmada.");

        if (string.IsNullOrWhiteSpace(dto.Numero)) throw new InvalidOperationException("Número requerido.");
        if (dto.Monto <= 0) throw new InvalidOperationException("Monto inválido.");

        if (compra.FacturaProveedor != null)
            throw new InvalidOperationException("Esta compra ya tiene factura registrada.");

        compra.FacturaProveedor = new FacturaProveedor
        {
            CompraId = compraId,
            Numero = dto.Numero.Trim(),
            Fecha = dto.Fecha,
            Monto = dto.Monto
        };

        var movimientoCompra = new MovimientoCaja
        {
            Tipo = TipoMovimientoCaja.Egreso,
            Monto = dto.Monto,
            Concepto = $"{CompraProveedorConceptPrefix} Compra #{compraId} - Factura {dto.Numero.Trim()}",
            MedioPago = MedioPago.Transferencia,
            Referencia = dto.Numero.Trim()
        };

        _context.MovimientosCaja.Add(movimientoCompra);
        await _dvService.RecalcularEntidadAsync(movimientoCompra);

        await _context.SaveChangesAsync();
        await _dvService.RecalcularDVVAsync("MovimientosCaja");
        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            Accion = "COMPRA_FACTURA_REGISTRAR",
            Detalle = $"Factura registrada para compra #{compraId}.",
            Resultado = "OK"
        });
    }

    private static async Task<List<CatalogoProveedorRow>> LeerCatalogoCsvAsync(Stream stream)
    {
        var rows = new List<CatalogoProveedorRow>();
        using var reader = new StreamReader(stream);
        var lineNumber = 0;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (lineNumber == 1) continue; // header

            // Formato: productoId;nombre;costoUnitario
            var parts = line.Split(';');
            if (parts.Length < 3)
                throw new InvalidOperationException($"CSV invalido en linea {lineNumber}. Formato: productoId;nombre;costoUnitario");

            var productoIdTxt = parts[0].Trim();
            var nombre = parts[1].Trim();
            var costoTxt = parts[2].Trim().Replace(',', '.');

            int? productoId = null;
            if (!string.IsNullOrWhiteSpace(productoIdTxt))
            {
                if (!int.TryParse(productoIdTxt, out var parsedId))
                    throw new InvalidOperationException($"CSV invalido en linea {lineNumber}. ProductoId incorrecto.");
                productoId = parsedId;
            }

            if (!decimal.TryParse(costoTxt, NumberStyles.Any, CultureInfo.InvariantCulture, out var costo) || costo <= 0)
                throw new InvalidOperationException($"CSV invalido en linea {lineNumber}. Costo unitario incorrecto.");

            if (productoId == null && string.IsNullOrWhiteSpace(nombre))
                throw new InvalidOperationException($"CSV invalido en linea {lineNumber}. Informa productoId o nombre.");

            rows.Add(new CatalogoProveedorRow
            {
                ProductoId = productoId,
                Nombre = nombre,
                CostoUnitario = costo
            });
        }

        return rows;
    }

    private static async Task<List<CatalogoProveedorRow>> LeerCatalogoXlsxAsync(Stream stream)
    {
        var rows = new List<CatalogoProveedorRow>();
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.FirstOrDefault() ?? throw new InvalidOperationException("Excel sin hojas.");
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (var r = 2; r <= lastRow; r++)
        {
            var productoIdTxt = ws.Cell(r, 1).GetString().Trim();
            var nombre = ws.Cell(r, 2).GetString().Trim();
            var costoTxt = ws.Cell(r, 3).GetString().Trim().Replace(',', '.');

            if (string.IsNullOrWhiteSpace(productoIdTxt) && string.IsNullOrWhiteSpace(nombre) && string.IsNullOrWhiteSpace(costoTxt))
                continue;

            int? productoId = null;
            if (!string.IsNullOrWhiteSpace(productoIdTxt))
            {
                if (!int.TryParse(productoIdTxt, out var parsedId))
                    throw new InvalidOperationException($"Excel invalido en fila {r}. ProductoId incorrecto.");
                productoId = parsedId;
            }

            if (!decimal.TryParse(costoTxt, NumberStyles.Any, CultureInfo.InvariantCulture, out var costo) || costo <= 0)
                throw new InvalidOperationException($"Excel invalido en fila {r}. Costo unitario incorrecto.");

            if (productoId == null && string.IsNullOrWhiteSpace(nombre))
                throw new InvalidOperationException($"Excel invalido en fila {r}. Informa productoId o nombre.");

            rows.Add(new CatalogoProveedorRow
            {
                ProductoId = productoId,
                Nombre = nombre,
                CostoUnitario = costo
            });
        }

        await Task.CompletedTask;
        return rows;
    }

    private async Task<int> ResolverProductoIdDesdeCatalogoAsync(CatalogoProveedorRow row)
    {
        if (row.ProductoId.HasValue)
        {
            var existe = await _context.Productos.AnyAsync(p => p.Id == row.ProductoId.Value);
            if (!existe) throw new InvalidOperationException($"ProductoId inexistente: {row.ProductoId.Value}");
            return row.ProductoId.Value;
        }

        var nombre = row.Nombre.Trim();
        var existente = await _context.Productos.FirstOrDefaultAsync(p => p.Nombre == nombre);
        if (existente != null) return existente.Id;

        var nuevo = new Producto
        {
            Nombre = nombre,
            Descripcion = null,
            Precio = row.CostoUnitario,
            Stock = 0,
            ImagenUrl = null,
            FechaCreacion = DateTime.UtcNow
        };
        _context.Productos.Add(nuevo);
        await _context.SaveChangesAsync();
        return nuevo.Id;
    }

    private string SafeDecrypt(string value)
    {
        try
        {
            return _cryptoService.Decrypt(value);
        }
        catch
        {
            return value;
        }
    }

    private static string NormalizarCuit(string cuit)
    {
        var limpio = cuit.Trim();
        if (!Regex.IsMatch(limpio, @"^\d+$"))
            throw new InvalidOperationException("CUIT invalido. Debe contener solo numeros.");
        return limpio;
    }

    private sealed class CatalogoProveedorRow
    {
        public int? ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal CostoUnitario { get; set; }
    }
}
