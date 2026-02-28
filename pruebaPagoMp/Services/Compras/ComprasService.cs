using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.Dtos.Compras;
using pruebaPagoMp.Models.Bitacora;
using pruebaPagoMp.Models.Compras;
using pruebaPagoMp.Models.Compras.Enums;
using pruebaPagoMp.Security;
using pruebaPagoMp.Services.Bitacora;

namespace pruebaPagoMp.Services.Compras;

public class ComprasService : IComprasService
{
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

        var proveedor = new Proveedor
        {
            RazonSocial = dto.RazonSocial.Trim(),
            CUIT = _cryptoService.Encrypt(dto.CUIT.Trim()),
            CuitHash = SecurityHashing.Sha256Normalized(dto.CUIT),
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

        proveedor.RazonSocial = dto.RazonSocial.Trim();
        proveedor.CUIT = _cryptoService.Encrypt(dto.CUIT.Trim());
        proveedor.CuitHash = SecurityHashing.Sha256Normalized(dto.CUIT);
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

        await _context.SaveChangesAsync();
        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            Accion = "COMPRA_FACTURA_REGISTRAR",
            Detalle = $"Factura registrada para compra #{compraId}.",
            Resultado = "OK"
        });
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
}
