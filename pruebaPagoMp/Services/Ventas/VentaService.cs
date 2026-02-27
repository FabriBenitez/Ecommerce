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
        if (string.IsNullOrWhiteSpace(dto.ClienteDni))
            throw new InvalidOperationException("Debe informar el DNI del cliente.");

        if (dto.Items == null || dto.Items.Count == 0)
            throw new InvalidOperationException("Debe incluir al menos un item a devolver.");

        if (dto.MontoDevolver < 0)
            throw new InvalidOperationException("Monto de devolución inválido.");

        // Si querés permitir generar NC con 0, sacá esta validación
        if (dto.GenerarNotaCredito && dto.MontoDevolver <= 0)
            throw new InvalidOperationException("Para generar nota de crédito, el monto debe ser mayor a cero.");

        await using var tx = await _context.Database.BeginTransactionAsync();

        var venta = await _context.Ventas
            .Include(v => v.Detalles)
            .FirstOrDefaultAsync(v => v.Id == ventaId);

        if (venta == null)
            throw new InvalidOperationException("Venta inexistente.");

        if (venta.Canal != CanalVenta.Presencial)
            throw new InvalidOperationException("Solo aplica a ventas presenciales.");

        if (venta.EstadoVenta == EstadoVenta.Cancelada)
            throw new InvalidOperationException("La venta ya está cancelada.");

        var dni = dto.ClienteDni.Trim();

        // ✅ Si la venta no tenía DNI (tu caso), lo guardamos ahora
        if (string.IsNullOrWhiteSpace(venta.ClienteDni))
            venta.ClienteDni = dni;
        else if (!string.Equals(venta.ClienteDni, dni, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El DNI informado no coincide con la venta.");

        // (Opcional) si querés guardar nombre si vino y no estaba
        if (string.IsNullOrWhiteSpace(venta.ClienteNombre) && !string.IsNullOrWhiteSpace(dto.ClienteNombre))
            venta.ClienteNombre = dto.ClienteNombre.Trim();

        var itemsDevueltos = dto.Items
            .GroupBy(i => i.ProductoId)
            .Select(g => new { ProductoId = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
            .ToList();

        if (itemsDevueltos.Any(i => i.Cantidad <= 0))
            throw new InvalidOperationException("Las cantidades a devolver deben ser mayores a cero.");

        var detalleCantidades = venta.Detalles
            .GroupBy(d => d.ProductoId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Cantidad));

        foreach (var item in itemsDevueltos)
        {
            if (!detalleCantidades.TryGetValue(item.ProductoId, out var cantidadVendida))
                throw new InvalidOperationException($"El producto {item.ProductoId} no pertenece a la venta.");

            if (item.Cantidad > cantidadVendida)
                throw new InvalidOperationException($"La cantidad devuelta de producto {item.ProductoId} supera la cantidad vendida.");
        }

        var productosIds = itemsDevueltos.Select(i => i.ProductoId).ToList();
        var productos = await _context.Productos
            .Where(p => productosIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        // ✅ Restock
        foreach (var item in itemsDevueltos)
        {
            if (!productos.TryGetValue(item.ProductoId, out var producto))
                throw new InvalidOperationException($"Producto inexistente: {item.ProductoId}");

            producto.Stock += item.Cantidad;
        }

        var devolucionTotal =
            detalleCantidades.Count == itemsDevueltos.Count &&
            detalleCantidades.All(dc => itemsDevueltos.Any(i => i.ProductoId == dc.Key && i.Cantidad == dc.Value));

        int notaCreditoId = 0;

        // ✅ Nota crédito
        if (dto.GenerarNotaCredito && dto.MontoDevolver > 0)
        {
            var notaCredito = await _context.NotasCredito
                .FirstOrDefaultAsync(x => x.ClienteDni == dni);

            if (notaCredito == null)
            {
                notaCredito = new NotaCredito
                {
                    ClienteDni = dni,
                    SaldoDisponible = dto.MontoDevolver,
                    FechaCreacion = DateTime.UtcNow
                };
                _context.NotasCredito.Add(notaCredito);
            }
            else
            {
                notaCredito.SaldoDisponible += dto.MontoDevolver;
            }

            await _context.SaveChangesAsync();
            notaCreditoId = notaCredito.Id;
        }

        // ✅ Caja (Egreso) SOLO si devolvés plata real (no nota crédito)
        if (!dto.GenerarNotaCredito && dto.MontoDevolver > 0)
        {
            var motivo = string.IsNullOrWhiteSpace(dto.Motivo)
                ? ""
                : $" - {dto.Motivo.Trim()}";

            _context.MovimientosCaja.Add(new MovimientoCaja
            {
                Tipo = TipoMovimientoCaja.Egreso,
                Monto = dto.MontoDevolver,
                Concepto = $"Devolución venta presencial #{ventaId}{motivo}",
                VentaId = ventaId,
                MedioPago = MedioPago.Efectivo
            });
        }

        // ✅ Estado / Observaciones
        if (devolucionTotal)
        {
            venta.EstadoVenta = EstadoVenta.Cancelada;
            venta.Observaciones = AppendObs(venta.Observaciones,
                $"Venta cancelada por devolución total ({DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC).");
        }
        else
        {
            venta.Observaciones = AppendObs(venta.Observaciones,
                $"Devolución parcial registrada ({DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC).");
        }

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return notaCreditoId;

        static string AppendObs(string? actual, string nuevo)
            => string.IsNullOrWhiteSpace(actual) ? nuevo : $"{actual} | {nuevo}";
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

    // 5) Validar pagos combinados: suma == total (con redondeo para evitar falso mismatch)
    var sumaPagos = decimal.Round(dto.Pagos.Sum(p => p.Monto), 2);
    var totalRedondeado = decimal.Round(total, 2);
    if (Math.Abs(sumaPagos - totalRedondeado) > 0.01m)
        throw new InvalidOperationException($"La suma de pagos ({sumaPagos}) no coincide con el total ({totalRedondeado}).");

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
            .Include(v => v.Pagos)
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
            EstadoRetiro = venta.EstadoRetiro,


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
            }).ToList(),
            Pagos = venta.Pagos.Select(p => new PagoItemDto
            {
                MedioPago = p.MedioPago,
                Monto = p.Monto,
                Referencia = p.Referencia
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
                Canal = v.Canal,
                EstadoRetiro = v.EstadoRetiro
            })
            .ToListAsync();
    }
    public async Task<List<HistorialPresencialItemDto>> ObtenerHistorialPresencialAsync(
        DateTime? desde,
        DateTime? hasta,
        string? dni,
        EstadoVenta? estado)
    {
        var q = _context.Ventas
            .AsNoTracking()
            .Where(v => v.Canal == CanalVenta.Presencial);

        if (desde.HasValue) q = q.Where(v => v.Fecha >= desde.Value);
        if (hasta.HasValue) q = q.Where(v => v.Fecha <= hasta.Value);
        if (!string.IsNullOrWhiteSpace(dni)) q = q.Where(v => v.ClienteDni == dni.Trim());
        if (estado.HasValue) q = q.Where(v => v.EstadoVenta == estado.Value);

        return await q
            .OrderByDescending(v => v.Fecha)
            .Select(v => new HistorialPresencialItemDto
            {
                Id = v.Id,
                Fecha = v.Fecha,
                ClienteDni = v.ClienteDni,
                ClienteNombre = v.ClienteNombre,
                Total = v.Total,
                EstadoVenta = v.EstadoVenta,
                Canal = v.Canal
            })
            .ToListAsync();
    }

}


