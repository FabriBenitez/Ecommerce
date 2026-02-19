using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.Dtos.Ventas;
using pruebaPagoMp.Models;
using pruebaPagoMp.Models.Caja;
using pruebaPagoMp.Models.Caja.Enums;
using pruebaPagoMp.Models.Ventas;
using pruebaPagoMp.Services.Pagos;
using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Services.Ventas;

public class VentasService : IVentasService
{
    private readonly ApplicationDbContext _context;
    private readonly IMercadoPagoService _mercadoPagoService;

    public VentasService(ApplicationDbContext context, IMercadoPagoService mercadoPagoService)
    {
        _context = context;
        _mercadoPagoService = mercadoPagoService;
    }
    public async Task<int> RegistrarDevolucionPresencialAsync(int ventaId, DevolucionPresencialDto dto)
    {
        if (dto.Monto <= 0) throw new InvalidOperationException("Monto inválido.");

        await using var tx = await _context.Database.BeginTransactionAsync();

        var venta = await _context.Ventas.FirstOrDefaultAsync(v => v.Id == ventaId);
        if (venta == null) throw new InvalidOperationException("Venta inexistente.");
        if (venta.Canal != CanalVenta.Presencial) throw new InvalidOperationException("Solo aplica a ventas presenciales.");

        // 1) Nota de crédito (acumula por DNI)
        var nc = await _context.Set<NotaCredito>()
            .FirstOrDefaultAsync(x => x.ClienteDni == dto.ClienteDni);

        if (nc == null)
        {
            nc = new NotaCredito
            {
                ClienteDni = dto.ClienteDni,
                SaldoDisponible = dto.Monto,
                FechaCreacion = DateTime.UtcNow
            };
            _context.Add(nc);
        }
        else
        {
            nc.SaldoDisponible += dto.Monto;
        }

        // 2) Movimiento de caja EGRESO (nota crédito)
        var mov = new MovimientoCaja
        {
            Tipo = TipoMovimientoCaja.Egreso,
            Monto = dto.Monto,
            Concepto = string.IsNullOrWhiteSpace(dto.Motivo) ? "Devolución / Nota de crédito" : $"Devolución: {dto.Motivo}",
            VentaId = ventaId,
            MedioPago = MedioPago.NotaCredito
        };
        _context.Add(mov);

        // 3) (opcional) marcar venta cancelada si querés cancelación total
        // venta.EstadoVenta = EstadoVenta.Cancelada;

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return nc.Id;
    }


    public async Task<CheckoutVentaWebRespuestaDto> CheckoutVentaWebAsync(int usuarioId, CheckoutVentaWebDto dto)
    {
        
        // 1) Carrito activo del usuario (con items)
        var carrito = await _context.Carritos
            .Include(c => c.CarritoItems)
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

        if (carrito == null || carrito.CarritoItems.Count == 0)
            throw new InvalidOperationException("No hay carrito activo o está vacío.");

        // 2) Validar stock y “congelar” precios leyendo Producto actual
        var productoIds = carrito.CarritoItems.Select(i => i.ProductoId).Distinct().ToList();

        var productos = await _context.Productos
            .Where(p => productoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var item in carrito.CarritoItems)
        {
            if (!productos.TryGetValue(item.ProductoId, out var prod))
                throw new InvalidOperationException($"Producto inexistente: {item.ProductoId}");

            if (prod.Stock < item.Cantidad)
                throw new InvalidOperationException($"Stock insuficiente para {prod.Nombre}. Disponible: {prod.Stock}");
        }

        // 3) Crear Venta (Pendiente, Web) + Detalles (inmutable)
        var venta = new Venta
        {
            Fecha = DateTime.UtcNow,
            UsuarioId = usuarioId,
            ClienteDni = dto.ClienteDni?.Trim(),
            ClienteNombre = dto.NombreEntrega,
            EstadoVenta = EstadoVenta.Pendiente,   // ajustá si tu propiedad se llama distinto
            Canal = CanalVenta.Web,                // ajustá si tu propiedad se llama distinto
            Total = 0m
        };

        _context.Ventas.Add(venta);
        await _context.SaveChangesAsync(); // para obtener Venta.Id

        decimal total = 0m;

        foreach (var item in carrito.CarritoItems)
        {
            var prod = productos[item.ProductoId];

            // Precio “congelado” en la venta:
            var precioUnitario = prod.Precio;
            var subtotal = precioUnitario * item.Cantidad;

            total += subtotal;

            var detalle = new DetalleVenta
            {
                VentaId = venta.Id,
                ProductoId = prod.Id,
                Cantidad = item.Cantidad,
                PrecioUnitario = precioUnitario,
                Subtotal = subtotal
            };

            _context.DetalleVentas.Add(detalle);
        }
        venta.NombreEntrega = dto.NombreEntrega;
        venta.TelefonoEntrega = dto.TelefonoEntrega;
        venta.DireccionEntrega = dto.DireccionEntrega;
        venta.Ciudad = dto.Ciudad;
        venta.Provincia = dto.Provincia;
        venta.CodigoPostal = dto.CodigoPostal;

        // solo lo libre queda en Observaciones
        venta.Observaciones = dto.Observaciones;


        venta.Total = total;
        await _context.SaveChangesAsync();

        // 4) Crear preferencia MercadoPago usando external_reference = venta.Id
        var descripcion = $"Compra Web - Venta #{venta.Id}";
        (string preferenceId, String urlPago) = await _mercadoPagoService.CrearPreferenciaPagoAsync(venta.Id, total, descripcion);

        // 5) Guardar referencias MP en tu entidad Pago o en Venta (depende tu diseño)
        // ✅ Como tu fase 2 Bloque C pide Venta ↔ Pago, acá creamos un Pago “Pendiente”

        // ⚠️ Si tu tabla Pago exige PedidoId, NO guardes acá.
        // En ese caso: guardá preferenceId en Venta (agregando columna) o creá entidad PagoVenta.
        // Lo dejo comentado para evitar romperte compilación:
        // _context.Pagos.Add(pago);
        // await _context.SaveChangesAsync();

        // 6) (Opcional) vaciar carrito luego de crear venta pendiente
        _context.CarritoItems.RemoveRange(carrito.CarritoItems);
        await _context.SaveChangesAsync();

        return new CheckoutVentaWebRespuestaDto
        {
            VentaId = venta.Id,
            PreferenceId = preferenceId,
            UrlPago = urlPago,
            Total = total
        };
    }

    public async Task<int> ConfirmarPagoVentaAsync(int ventaId, string? referenciaExternaPago, string? metodoPago, string estadoPago)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();

        var venta = await _context.Ventas
            .Include(v => v.Detalles)
            .FirstOrDefaultAsync(v => v.Id == ventaId);

        if (venta == null) throw new InvalidOperationException("Venta inexistente.");

        if (venta.EstadoVenta == EstadoVenta.Pagada)
            return venta.Id; // idempotente

        if (!string.Equals(estadoPago, "approved", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Pago no aprobado: {estadoPago}");

        foreach (var det in venta.Detalles)
        {
            var prod = await _context.Productos.FirstAsync(p => p.Id == det.ProductoId);
            if (prod.Stock < det.Cantidad)
                throw new InvalidOperationException($"Stock insuficiente al confirmar. Producto: {prod.Nombre}");

            prod.Stock -= det.Cantidad;
        }

        venta.EstadoVenta = EstadoVenta.Pagada;

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return venta.Id;
    }



    public async Task<int> CrearVentaPresencialAsync(int adminUsuarioId, CrearVentaPresencialDto dto)
{
    // 0) Validaciones básicas
    if (dto.Items == null || dto.Items.Count == 0)
        throw new InvalidOperationException("Debe incluir al menos un item.");

    if (dto.Pagos == null || dto.Pagos.Count == 0)
        throw new InvalidOperationException("Debe incluir al menos un pago.");

    await using var tx = await _context.Database.BeginTransactionAsync();

    // 1) Traer productos
    var productoIds = dto.Items.Select(i => i.ProductoId).Distinct().ToList();
    var productos = await _context.Productos
        .Where(p => productoIds.Contains(p.Id))
        .ToDictionaryAsync(p => p.Id);

    // 2) Validar stock + cantidades
    foreach (var item in dto.Items)
    {
        if (item.Cantidad <= 0)
            throw new InvalidOperationException("Cantidad inválida.");

        if (!productos.TryGetValue(item.ProductoId, out var prod))
            throw new InvalidOperationException($"Producto inexistente: {item.ProductoId}");

        if (prod.Stock < item.Cantidad)
            throw new InvalidOperationException($"Stock insuficiente para {prod.Nombre}. Disponible: {prod.Stock}");
    }

    // 3) Crear venta (Presencial, Pagada)
    var venta = new Venta
    {
        Fecha = DateTime.UtcNow,
        UsuarioId = adminUsuarioId,         // quien registró la venta (admin)
        EstadoVenta = EstadoVenta.Pagada,
        Canal = CanalVenta.Presencial,
        Total = 0m,
        Observaciones = dto.Observaciones,
        // si querés guardar dni/nombre del cliente, sumalos a Venta como campos opcionales:
        ClienteDni = dto.ClienteDni?.Trim(),
        ClienteNombre = dto.ClienteNombre?.Trim(),
    };

    _context.Ventas.Add(venta);
    await _context.SaveChangesAsync(); // obtener venta.Id

    // 4) Detalles + descuento stock + total
    decimal total = 0m;

    foreach (var item in dto.Items)
    {
        var prod = productos[item.ProductoId];

        var precioUnitario = prod.Precio;
        var subtotal = precioUnitario * item.Cantidad;

        total += subtotal;

        _context.DetalleVentas.Add(new DetalleVenta
        {
            VentaId = venta.Id,
            ProductoId = prod.Id,
            Cantidad = item.Cantidad,
            PrecioUnitario = precioUnitario,
            Subtotal = subtotal
        });

        prod.Stock -= item.Cantidad;
    }

    venta.Total = total;
    await _context.SaveChangesAsync();

    // 5) Validar pagos combinados: suma == total
    var sumaPagos = dto.Pagos.Sum(p => p.Monto);
    if (sumaPagos != total)
        throw new InvalidOperationException($"La suma de pagos ({sumaPagos}) no coincide con el total ({total}).");

    // 6) Nota de crédito (si aplica): validar saldo y descontar
    // Regla opción 1: NotaCredito NO genera ingreso en caja (porque no entra plata nueva)
    foreach (var p in dto.Pagos)
    {
        if (p.Monto <= 0)
            throw new InvalidOperationException("Monto de pago inválido.");

        if (p.MedioPago == MedioPago.NotaCredito)
        {
            if (string.IsNullOrWhiteSpace(dto.ClienteDni))
                throw new InvalidOperationException("Para usar Nota de Crédito se requiere DNI del cliente.");

            var nc = await _context.NotasCredito
                .FirstOrDefaultAsync(x => x.ClienteDni == dto.ClienteDni);

            if (nc == null || nc.SaldoDisponible < p.Monto)
                throw new InvalidOperationException("Saldo de Nota de Crédito insuficiente.");

            nc.SaldoDisponible -= p.Monto;
        }
    }

    await _context.SaveChangesAsync();

    // 7) Guardar VentaPago + MovimientoCaja (solo para medios que son ingreso)
    foreach (var p in dto.Pagos)
    {
        _context.VentaPagos.Add(new VentaPago
        {
            VentaId = venta.Id,
            MedioPago = p.MedioPago,
            Monto = p.Monto,
            Referencia = p.Referencia
        });

        // Caja (opción 1): ingreso solo cuando entra plata nueva
        if (p.MedioPago != MedioPago.NotaCredito)
        {
            _context.MovimientosCaja.Add(new MovimientoCaja
            {
                Tipo = TipoMovimientoCaja.Ingreso,
                Monto = p.Monto,
                Concepto = $"Venta Presencial #{venta.Id}",
                VentaId = venta.Id,
                MedioPago = p.MedioPago,
                Referencia = p.Referencia
            });
        }
    }

    await _context.SaveChangesAsync();
    await tx.CommitAsync();

    return venta.Id;
}

    public async Task<VentaDto?> ObtenerVentaPorIdAsync(int ventaId, int usuarioId, bool esAdminVentas)
    {
        var venta = await _context.Ventas
            .AsNoTracking()
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(v => v.Id == ventaId);

        if (venta == null) return null;

        if (!esAdminVentas && venta.UsuarioId != usuarioId)
            throw new UnauthorizedAccessException("No tenés permisos para ver esta venta.");

        return new VentaDto
        {
            Id = venta.Id,
            Fecha = venta.Fecha,
            Total = venta.Total,
            EstadoVenta = venta.EstadoVenta,
            Canal = venta.Canal,

            ClienteDni = venta.ClienteDni,
            ClienteNombre = venta.ClienteNombre,

            NombreEntrega = venta.NombreEntrega,
            TelefonoEntrega = venta.TelefonoEntrega,
            DireccionEntrega = venta.DireccionEntrega,
            Ciudad = venta.Ciudad,
            Provincia = venta.Provincia,
            CodigoPostal = venta.CodigoPostal,
            Observaciones = venta.Observaciones,

            Detalles = venta.Detalles.Select(d => new VentaDetalleDto
            {
                ProductoId = d.ProductoId,
                NombreProducto = d.Producto?.Nombre ?? "(sin nombre)",
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Subtotal = d.Subtotal
            }).ToList()
        };

    }

    public async Task<List<MisVentasItemDto>> ObtenerMisVentasAsync(int usuarioId)
    {
        var dni = await _context.Usuarios
            .Where(u => u.Id == usuarioId)
            .Select(u => u.Dni)
            .FirstOrDefaultAsync();

        var q = _context.Ventas.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(dni))
        {
            q = q.Where(v => v.UsuarioId == usuarioId || v.ClienteDni == dni);
        }
        else
        {
            q = q.Where(v => v.UsuarioId == usuarioId);
        }

        return await q
            .OrderByDescending(v => v.Fecha)
            .Select(v => new MisVentasItemDto
            {
                Id = v.Id,
                Fecha = v.Fecha,
                Total = v.Total,
                EstadoVenta = v.EstadoVenta,
                Canal = v.Canal
            })
            .ToListAsync();
    }
    public async Task<List<FacturaPresencialItemDto>> ObtenerHistorialPresencialAsync()
    {
        return await _context.Ventas
            .AsNoTracking()
            .Where(v => v.Canal == CanalVenta.Presencial)
            .OrderByDescending(v => v.Fecha)
            .Select(v => new FacturaPresencialItemDto
            {
                VentaId = v.Id,
                Fecha = v.Fecha,
                ClienteDni = v.ClienteDni,         // si existe en tu entidad Venta
                ClienteNombre = v.ClienteNombre,   // si existe en tu entidad Venta
                Total = v.Total,
                EstadoVenta = v.EstadoVenta,
                Canal = v.Canal
            })
            .ToListAsync();
    }

}
