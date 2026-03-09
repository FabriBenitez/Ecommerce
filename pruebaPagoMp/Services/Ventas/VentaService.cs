using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.Dtos.Ventas;
using pruebaPagoMp.Models;
using pruebaPagoMp.Models.Caja;
using pruebaPagoMp.Models.Caja.Enums;
using pruebaPagoMp.Models.Bitacora;
using pruebaPagoMp.Models.Ventas;
using pruebaPagoMp.Services.Pagos;
using pruebaPagoMp.Models.Ventas.Enums;
using pruebaPagoMp.Security;
using pruebaPagoMp.Services.Bitacora;

namespace pruebaPagoMp.Services.Ventas;

public class VentasService : IVentasService
{
    private readonly ApplicationDbContext _context;
    private readonly IMercadoPagoService _mercadoPagoService;
    private readonly IDigitoVerificadorService _dvService;
    private readonly IBitacoraService _bitacoraService;

    public VentasService(
        ApplicationDbContext context,
        IMercadoPagoService mercadoPagoService,
        IDigitoVerificadorService dvService,
        IBitacoraService bitacoraService)
    {
        _context = context;
        _mercadoPagoService = mercadoPagoService;
        _dvService = dvService;
        _bitacoraService = bitacoraService;
    }
    public async Task<int> RegistrarDevolucionPresencialAsync(int ventaId, DevolucionPresencialDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ClienteDni))
            throw new InvalidOperationException("Debe informar el DNI del cliente.");

        if (dto.Items == null || dto.Items.Count == 0)
            throw new InvalidOperationException("Debe incluir al menos un item a devolver.");

        if (dto.MontoDevolver < 0)
            throw new InvalidOperationException("Monto de devoluciÃ³n invÃ¡lido.");

        // Si querÃ©s permitir generar NC con 0, sacÃ¡ esta validaciÃ³n
        if (dto.GenerarNotaCredito && dto.MontoDevolver <= 0)
            throw new InvalidOperationException("Para generar nota de crÃ©dito, el monto debe ser mayor a cero.");

        await using var tx = await _context.Database.BeginTransactionAsync();

        var venta = await _context.Ventas
            .Include(v => v.Detalles)
            .FirstOrDefaultAsync(v => v.Id == ventaId);

        if (venta == null)
            throw new InvalidOperationException("Venta inexistente.");

        if (venta.EstadoVenta == EstadoVenta.Cancelada)
            throw new InvalidOperationException("La venta ya estÃ¡ cancelada.");

        var dni = NormalizarDni(dto.ClienteDni);

        // âœ… Si la venta no tenÃ­a DNI (tu caso), lo guardamos ahora
        if (string.IsNullOrWhiteSpace(venta.ClienteDni))
            venta.ClienteDni = dni;
        else if (!string.Equals(NormalizarDni(venta.ClienteDni), dni, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El DNI informado no coincide con la venta.");

        // (Opcional) si querÃ©s guardar nombre si vino y no estaba
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

        // âœ… Restock
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

        // âœ… Nota crÃ©dito
        if (dto.GenerarNotaCredito && dto.MontoDevolver > 0)
        {
            var notasCredito = await _context.NotasCredito
                .Where(x => x.ClienteDni != null &&
                            x.ClienteDni.Replace(".", "").Replace("-", "").Replace(" ", "") == dni)
                .OrderBy(x => x.FechaCreacion)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var notaCredito = notasCredito.FirstOrDefault();

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

        // âœ… Caja (Egreso) SOLO si devolvÃ©s plata real (no nota crÃ©dito)
        if (!dto.GenerarNotaCredito && dto.MontoDevolver > 0)
        {
            var motivo = string.IsNullOrWhiteSpace(dto.Motivo)
                ? ""
                : $" - {dto.Motivo.Trim()}";

            _context.MovimientosCaja.Add(new MovimientoCaja
            {
                Tipo = TipoMovimientoCaja.Egreso,
                Monto = dto.MontoDevolver,
                Concepto = $"DevoluciÃ³n venta #{ventaId}{motivo}",
                VentaId = ventaId,
                MedioPago = MedioPago.Efectivo
            });
        }

        // âœ… Estado / Observaciones
        if (devolucionTotal)
        {
            venta.EstadoVenta = EstadoVenta.Cancelada;
            venta.Observaciones = AppendObs(venta.Observaciones,
                $"Venta cancelada por devoluciÃ³n total ({DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC).");
        }
        else
        {
            venta.Observaciones = AppendObs(venta.Observaciones,
                $"DevoluciÃ³n parcial registrada ({DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC).");
        }

        await _dvService.RecalcularEntidadAsync(venta);
        var movs = _context.ChangeTracker.Entries<MovimientoCaja>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .Select(e => e.Entity)
            .ToList();
        foreach (var mov in movs) await _dvService.RecalcularEntidadAsync(mov);

        await _context.SaveChangesAsync();
        await tx.CommitAsync();
        await _dvService.RecalcularDVVAsync("Ventas");
        await _dvService.RecalcularDVVAsync("MovimientosCaja");
        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            Accion = "VENTA_DEVOLUCION",
            Detalle = $"DevoluciÃ³n presencial en venta #{ventaId}.",
            Resultado = "OK"
        });

        return notaCreditoId;

        static string AppendObs(string? actual, string nuevo)
            => string.IsNullOrWhiteSpace(actual) ? nuevo : $"{actual} | {nuevo}";
    }


    public async Task<CheckoutVentaWebRespuestaDto> CheckoutVentaWebAsync(int usuarioId, CheckoutVentaWebDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ClienteDni))
            throw new InvalidOperationException("Debe ingresar su DNI.");

        if (string.IsNullOrWhiteSpace(dto.NombreEntrega))
            throw new InvalidOperationException("Debe ingresar su nombre completo.");

	    if (string.IsNullOrWhiteSpace(dto.TelefonoEntrega))
	        throw new InvalidOperationException("Debe ingresar su telefono.");

	    var clienteDni = NormalizarDniObligatorio(dto.ClienteDni);
	    var nombreEntrega = NormalizarTextoObligatorio(dto.NombreEntrega, "nombre", 200);
	    var telefonoEntrega = NormalizarTextoObligatorio(dto.TelefonoEntrega, "telefono", 50);

        // 1) Carrito activo del usuario (con items)
        var carrito = await _context.Carritos
            .Include(c => c.CarritoItems)
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

        if (carrito == null || carrito.CarritoItems.Count == 0)
            throw new InvalidOperationException("No hay carrito activo o estÃ¡ vacÃ­o.");

        // 2) Validar stock y â€œcongelarâ€ precios leyendo Producto actual
        var productoIds = carrito.CarritoItems.Select(i => i.ProductoId).Distinct().ToList();

        var productos = await _context.Productos
            .Where(p => productoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);
        var reservados = await ObtenerReservadoActivoPorProductoAsync(productoIds);

        foreach (var item in carrito.CarritoItems)
        {
            if (!productos.TryGetValue(item.ProductoId, out var prod))
                throw new InvalidOperationException($"Producto inexistente: {item.ProductoId}");

            var reservado = reservados.TryGetValue(item.ProductoId, out var cantRes) ? cantRes : 0;
            var disponible = prod.Stock - reservado;
            if (disponible < item.Cantidad)
                throw new InvalidOperationException($"Stock insuficiente para {prod.Nombre}. Disponible: {disponible}");
        }

        // 3) Crear Venta (Pendiente, Web) + Detalles (inmutable)
        var venta = new Venta
        {
            Fecha = DateTime.UtcNow,
            UsuarioId = usuarioId,
	            ClienteDni = clienteDni,
	            ClienteNombre = nombreEntrega,
            EstadoVenta = EstadoVenta.Pendiente,   // ajustÃ¡ si tu propiedad se llama distinto
            Canal = CanalVenta.Web,                // ajustÃ¡ si tu propiedad se llama distinto
            Total = 0m
        };
        await _dvService.RecalcularEntidadAsync(venta);

        _context.Ventas.Add(venta);
        await _context.SaveChangesAsync(); // para obtener Venta.Id

        decimal total = 0m;

        foreach (var item in carrito.CarritoItems)
        {
            var prod = productos[item.ProductoId];

            // Precio â€œcongeladoâ€ en la venta:
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
	        venta.NombreEntrega = nombreEntrega;
	        venta.TelefonoEntrega = telefonoEntrega;
        venta.DireccionEntrega = string.IsNullOrWhiteSpace(dto.DireccionEntrega) ? null : dto.DireccionEntrega.Trim();
        venta.Ciudad = string.IsNullOrWhiteSpace(dto.Ciudad) ? null : dto.Ciudad.Trim();
        venta.Provincia = string.IsNullOrWhiteSpace(dto.Provincia) ? null : dto.Provincia.Trim();
        venta.CodigoPostal = string.IsNullOrWhiteSpace(dto.CodigoPostal) ? null : dto.CodigoPostal.Trim();
        venta.Observaciones = dto.Observaciones;


        venta.Total = total;
        await _dvService.RecalcularEntidadAsync(venta);
        await _context.SaveChangesAsync();

        // 4) Crear preferencia MercadoPago usando external_reference = venta.Id
        var descripcion = $"Compra Web - Venta #{venta.Id}";
        (string preferenceId, String urlPago) = await _mercadoPagoService.CrearPreferenciaPagoAsync(venta.Id, total, descripcion);

        // 5) Guardar referencias MP en tu entidad Pago o en Venta (depende tu diseÃ±o)
        // âœ… Como tu fase 2 Bloque C pide Venta â†” Pago, acÃ¡ creamos un Pago â€œPendienteâ€

        // âš ï¸ Si tu tabla Pago exige PedidoId, NO guardes acÃ¡.
        // En ese caso: guardÃ¡ preferenceId en Venta (agregando columna) o creÃ¡ entidad PagoVenta.
        // Lo dejo comentado para evitar romperte compilaciÃ³n:
        // _context.Pagos.Add(pago);
        // await _context.SaveChangesAsync();

        // 6) (Opcional) vaciar carrito luego de crear venta pendiente
        _context.CarritoItems.RemoveRange(carrito.CarritoItems);
        await _context.SaveChangesAsync();
        await _dvService.RecalcularDVVAsync("Ventas");
        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            UsuarioId = usuarioId,
            Accion = "VENTA_WEB_CHECKOUT",
            Detalle = $"Checkout web venta #{venta.Id}.",
            Resultado = "OK"
        });

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

        var productoIds = venta.Detalles.Select(d => d.ProductoId).Distinct().ToList();
        var reservados = await ObtenerReservadoActivoPorProductoAsync(productoIds);

        foreach (var det in venta.Detalles)
        {
            var prod = await _context.Productos.FirstAsync(p => p.Id == det.ProductoId);
            var reservado = reservados.TryGetValue(det.ProductoId, out var cantRes) ? cantRes : 0;
            var disponible = prod.Stock - reservado;
            if (disponible < det.Cantidad)
                throw new InvalidOperationException($"Stock insuficiente al confirmar. Producto: {prod.Nombre}");

            prod.Stock -= det.Cantidad;
        }

        venta.EstadoVenta = EstadoVenta.Pagada;
        await _dvService.RecalcularEntidadAsync(venta);

        await _context.SaveChangesAsync();
        await tx.CommitAsync();
        await _dvService.RecalcularDVVAsync("Ventas");
        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            Accion = "VENTA_WEB_CONFIRMAR_PAGO",
            Detalle = $"Pago confirmado venta #{ventaId}.",
            Resultado = "OK"
        });

        return venta.Id;
    }



	public async Task<int> CrearVentaPresencialAsync(int adminUsuarioId, CrearVentaPresencialDto dto)
{
	    // 0) Validaciones bÃ¡sicas
	    if (dto.Items == null || dto.Items.Count == 0)
	        throw new InvalidOperationException("Debe incluir al menos un item.");

	    if (dto.Pagos == null || dto.Pagos.Count == 0)
	        throw new InvalidOperationException("Debe incluir al menos un pago.");

	    var dniClienteNormalizado = NormalizarDniObligatorio(dto.ClienteDni);
	    var clienteNombre = NormalizarTextoObligatorio(dto.ClienteNombre, "nombre", 200);
	    var clienteTelefono = NormalizarTextoObligatorio(dto.ClienteTelefono, "telefono", 50);

	    await using var tx = await _context.Database.BeginTransactionAsync();

    // 1) Traer productos
    var productoIds = dto.Items.Select(i => i.ProductoId).Distinct().ToList();
    var productos = await _context.Productos
        .Where(p => productoIds.Contains(p.Id))
        .ToDictionaryAsync(p => p.Id);
    var reservados = await ObtenerReservadoActivoPorProductoAsync(productoIds);

    // 2) Validar stock + cantidades
    foreach (var item in dto.Items)
    {
        if (item.Cantidad <= 0)
            throw new InvalidOperationException("Cantidad invÃ¡lida.");

        if (!productos.TryGetValue(item.ProductoId, out var prod))
            throw new InvalidOperationException($"Producto inexistente: {item.ProductoId}");

        var reservado = reservados.TryGetValue(item.ProductoId, out var cantRes) ? cantRes : 0;
        var disponible = prod.Stock - reservado;
        if (disponible < item.Cantidad)
            throw new InvalidOperationException($"Stock insuficiente para {prod.Nombre}. Disponible: {disponible}");
    }

    // 3) Crear venta (Presencial, Pagada)
    var venta = new Venta
    {
        Fecha = DateTime.UtcNow,
        UsuarioId = adminUsuarioId,         // quien registrÃ³ la venta (admin)
        EstadoVenta = EstadoVenta.Pagada,
        Canal = CanalVenta.Presencial,
	        Total = 0m,
	        Observaciones = dto.Observaciones,
	        ClienteDni = dniClienteNormalizado,
	        ClienteNombre = clienteNombre,
	        TelefonoEntrega = clienteTelefono,
	    };
    await _dvService.RecalcularEntidadAsync(venta);

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
    await _dvService.RecalcularEntidadAsync(venta);
    await _context.SaveChangesAsync();

    // 5) Validar pagos combinados: suma == total (con redondeo para evitar falso mismatch)
    var sumaPagos = decimal.Round(dto.Pagos.Sum(p => p.Monto), 2);
    var totalRedondeado = decimal.Round(total, 2);
    if (Math.Abs(sumaPagos - totalRedondeado) > 0.01m)
        throw new InvalidOperationException($"La suma de pagos ({sumaPagos}) no coincide con el total ({totalRedondeado}).");

    // 6) Nota de crÃ©dito (si aplica): validar saldo y descontar
    // Regla opciÃ³n 1: NotaCredito NO genera ingreso en caja (porque no entra plata nueva)
	    foreach (var p in dto.Pagos)
    {
        if (p.Monto <= 0)
            throw new InvalidOperationException("Monto de pago invÃ¡lido.");

        if (p.MedioPago == MedioPago.NotaCredito)
        {
            if (string.IsNullOrWhiteSpace(dniClienteNormalizado))
                throw new InvalidOperationException("Para usar Nota de CrÃ©dito se requiere DNI del cliente.");

            var notasCliente = await _context.NotasCredito
                .Where(x => x.ClienteDni != null &&
                            x.ClienteDni.Replace(".", "").Replace("-", "").Replace(" ", "") == dniClienteNormalizado)
                .OrderBy(x => x.FechaCreacion)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var saldoTotal = notasCliente.Sum(x => x.SaldoDisponible);
            if (notasCliente.Count == 0 || saldoTotal < p.Monto)
                throw new InvalidOperationException("Saldo de Nota de CrÃ©dito insuficiente.");

            var pendiente = p.Monto;
            foreach (var nc in notasCliente)
            {
                if (pendiente <= 0) break;
                var uso = Math.Min(nc.SaldoDisponible, pendiente);
                nc.SaldoDisponible -= uso;
                pendiente -= uso;
            }
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

        // Caja (opciÃ³n 1): ingreso solo cuando entra plata nueva
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

    var movimientosCaja = _context.ChangeTracker.Entries<MovimientoCaja>()
        .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
        .Select(e => e.Entity)
        .ToList();
    foreach (var mov in movimientosCaja) await _dvService.RecalcularEntidadAsync(mov);

    await _context.SaveChangesAsync();
    await tx.CommitAsync();
    await _dvService.RecalcularDVVAsync("Ventas");
    await _dvService.RecalcularDVVAsync("MovimientosCaja");
    await _bitacoraService.RegistrarAsync(new BitacoraEntry
    {
        UsuarioId = adminUsuarioId,
        Accion = "VENTA_PRESENCIAL_CREAR",
        Detalle = $"Venta presencial #{venta.Id}.",
        Resultado = "OK"
    });

    return venta.Id;
}

public async Task<int> CrearReservaPresencialAsync(int adminUsuarioId, CrearReservaPresencialDto dto)
{
	    if (dto.Items == null || dto.Items.Count == 0)
	        throw new InvalidOperationException("Debe incluir al menos un item.");
	    if (dto.PagosAnticipo == null || dto.PagosAnticipo.Count == 0)
	        throw new InvalidOperationException("Debe incluir al menos un pago de seña.");

	    var dniClienteNormalizado = NormalizarDniObligatorio(dto.ClienteDni);
	    var clienteNombre = NormalizarTextoObligatorio(dto.ClienteNombre, "nombre", 200);
	    var clienteTelefono = NormalizarTextoObligatorio(dto.ClienteTelefono, "telefono", 50);

	    await using var tx = await _context.Database.BeginTransactionAsync();

    var productoIds = dto.Items
        .Where(i => i.ProductoId.HasValue)
        .Select(i => i.ProductoId!.Value)
        .Distinct()
        .ToList();

    var productos = await _context.Productos
        .Where(p => productoIds.Contains(p.Id))
        .ToDictionaryAsync(p => p.Id);

    var reservados = await ObtenerReservadoActivoPorProductoAsync(productoIds);
    var itemsNormalizados = new List<(Producto producto, int cantidad, bool esAConseguir)>();

    foreach (var item in dto.Items)
    {
        if (item.Cantidad <= 0)
            throw new InvalidOperationException("Cantidad inválida.");

        Producto prod;
        if (item.ProductoId.HasValue)
        {
            if (!productos.TryGetValue(item.ProductoId.Value, out prod!))
                throw new InvalidOperationException($"Producto inexistente: {item.ProductoId.Value}");
        }
        else
        {
            if (!item.EsAConseguir)
                throw new InvalidOperationException("Los items sin producto deben registrarse como 'A conseguir'.");
            if (string.IsNullOrWhiteSpace(item.NombreManual))
                throw new InvalidOperationException("Debes informar el nombre del libro a conseguir.");
            if (!item.PrecioUnitarioManual.HasValue || item.PrecioUnitarioManual.Value <= 0m)
                throw new InvalidOperationException("Debes informar un precio pactado válido para el libro a conseguir.");

            prod = new Producto
            {
                Nombre = item.NombreManual.Trim(),
                Precio = decimal.Round(item.PrecioUnitarioManual.Value, 2),
                Stock = 0,
                Descripcion = "Item generado desde señas (a conseguir).",
                EsTemporalSenia = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Productos.Add(prod);
            await _context.SaveChangesAsync();
            productos[prod.Id] = prod;
        }

        if (!item.EsAConseguir)
        {
            var reservado = reservados.TryGetValue(prod.Id, out var cantRes) ? cantRes : 0;
            var disponible = prod.Stock - reservado;
            if (disponible < item.Cantidad)
                throw new InvalidOperationException($"Stock insuficiente para {prod.Nombre}. Disponible: {disponible}");
        }

        itemsNormalizados.Add((prod, item.Cantidad, item.EsAConseguir));
    }

    var total = itemsNormalizados.Sum(x => x.producto.Precio * x.cantidad);
    var sumaSenia = decimal.Round(dto.PagosAnticipo.Sum(p => p.Monto), 2);
    var totalRedondeado = decimal.Round(total, 2);

    if (sumaSenia <= 0m)
        throw new InvalidOperationException("La seña debe ser mayor a cero.");
    if (sumaSenia > totalRedondeado)
        throw new InvalidOperationException("La seña no puede superar el total.");

	    await ValidarYAplicarNotaCreditoAsync(dniClienteNormalizado, dto.PagosAnticipo);

    var reserva = new Reserva
    {
        FechaCreacion = DateTime.UtcNow,
	        FechaVencimiento = DateTime.UtcNow,
	        UsuarioAdminId = adminUsuarioId,
	        ClienteDni = dniClienteNormalizado,
	        ClienteNombre = clienteNombre,
	        ClienteTelefono = clienteTelefono,
        Observaciones = dto.Observaciones,
        Total = totalRedondeado,
        MontoSenia = sumaSenia,
        SaldoPendiente = decimal.Round(totalRedondeado - sumaSenia, 2),
        RequiereCompra = itemsNormalizados.Any(x => x.esAConseguir),
        Estado = EstadoReserva.Senada
    };

    _context.Reservas.Add(reserva);
    await _context.SaveChangesAsync();

    foreach (var item in itemsNormalizados)
    {
        var subtotal = item.producto.Precio * item.cantidad;
        _context.ReservaItems.Add(new ReservaItem
        {
            ReservaId = reserva.Id,
            ProductoId = item.producto.Id,
            Cantidad = item.cantidad,
            PrecioUnitario = item.producto.Precio,
            Subtotal = subtotal,
            EsAConseguir = item.esAConseguir
        });
    }

    foreach (var p in dto.PagosAnticipo)
    {
        if (p.Monto <= 0m)
            throw new InvalidOperationException("Monto de pago inválido.");

        _context.ReservaPagos.Add(new ReservaPago
        {
            ReservaId = reserva.Id,
            MedioPago = p.MedioPago,
            Monto = p.Monto,
            Referencia = p.Referencia
        });

        if (p.MedioPago != MedioPago.NotaCredito)
        {
            _context.MovimientosCaja.Add(new MovimientoCaja
            {
                Tipo = TipoMovimientoCaja.Ingreso,
                Monto = p.Monto,
                Concepto = $"Seña reserva #{reserva.Id}",
                MedioPago = p.MedioPago,
                Referencia = p.Referencia
            });
        }
    }

    if (reserva.RequiereCompra)
    {
        _context.PedidosSenia.Add(new PedidoSenia
        {
            ReservaId = reserva.Id,
            ClienteDni = reserva.ClienteDni,
            ClienteNombre = reserva.ClienteNombre,
            ClienteTelefono = reserva.ClienteTelefono,
            Estado = EstadoPedidoSenia.PendienteCompra,
            FechaCreacion = DateTime.UtcNow
        });
    }

    var movimientosCaja = _context.ChangeTracker.Entries<MovimientoCaja>()
        .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
        .Select(e => e.Entity)
        .ToList();
    foreach (var mov in movimientosCaja) await _dvService.RecalcularEntidadAsync(mov);

    await _context.SaveChangesAsync();
    await tx.CommitAsync();
    await _dvService.RecalcularDVVAsync("MovimientosCaja");
    await _bitacoraService.RegistrarAsync(new BitacoraEntry
    {
        UsuarioId = adminUsuarioId,
        Accion = "RESERVA_PRESENCIAL_CREAR",
        Detalle = $"Reserva #{reserva.Id} creada con seña.",
        Resultado = "OK"
    });

    return reserva.Id;
}

public async Task<int> CompletarPagoReservaAsync(int reservaId, CompletarReservaPagoDto dto)
{
    if (dto.Pagos == null || dto.Pagos.Count == 0)
        throw new InvalidOperationException("Debe incluir al menos un pago.");

    await using var tx = await _context.Database.BeginTransactionAsync();

    var reserva = await _context.Reservas
        .Include(r => r.Items)
        .Include(r => r.Pagos)
        .FirstOrDefaultAsync(r => r.Id == reservaId);

    if (reserva == null) throw new InvalidOperationException("Reserva inexistente.");
    if (reserva.Estado != EstadoReserva.Senada)
        throw new InvalidOperationException("La reserva no estÃ¡ en estado seÃ±ada.");

    var sumaPagos = decimal.Round(dto.Pagos.Sum(p => p.Monto), 2);
    var saldoPendiente = decimal.Round(reserva.SaldoPendiente, 2);
    if (Math.Abs(sumaPagos - saldoPendiente) > 0.01m)
        throw new InvalidOperationException($"La suma de pagos ({sumaPagos}) no coincide con el saldo pendiente ({saldoPendiente}).");

    await ValidarYAplicarNotaCreditoAsync(reserva.ClienteDni, dto.Pagos);

    var productoIds = reserva.Items.Select(i => i.ProductoId).Distinct().ToList();
    var productos = await _context.Productos.Where(p => productoIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

    foreach (var item in reserva.Items)
    {
        if (!productos.TryGetValue(item.ProductoId, out var prod))
            throw new InvalidOperationException($"Producto inexistente: {item.ProductoId}");
        if (prod.Stock < item.Cantidad)
            throw new InvalidOperationException($"Stock fÃ­sico insuficiente para {prod.Nombre}. Disponible: {prod.Stock}");
    }

    var venta = new Venta
    {
        Fecha = DateTime.UtcNow,
        UsuarioId = reserva.UsuarioAdminId,
        EstadoVenta = EstadoVenta.Pagada,
        Canal = CanalVenta.Presencial,
        ClienteDni = reserva.ClienteDni,
        ClienteNombre = reserva.ClienteNombre,
        Total = reserva.Total,
        Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones)
            ? $"Venta generada desde reserva #{reserva.Id}."
            : $"Venta generada desde reserva #{reserva.Id}. {dto.Observaciones.Trim()}"
    };
    await _dvService.RecalcularEntidadAsync(venta);
    _context.Ventas.Add(venta);
    await _context.SaveChangesAsync();

    foreach (var item in reserva.Items)
    {
        var prod = productos[item.ProductoId];
        _context.DetalleVentas.Add(new DetalleVenta
        {
            VentaId = venta.Id,
            ProductoId = item.ProductoId,
            Cantidad = item.Cantidad,
            PrecioUnitario = item.PrecioUnitario,
            Subtotal = item.Subtotal
        });
        prod.Stock -= item.Cantidad;
    }

    foreach (var p in reserva.Pagos)
    {
        _context.VentaPagos.Add(new VentaPago
        {
            VentaId = venta.Id,
            MedioPago = p.MedioPago,
            Monto = p.Monto,
            Referencia = p.Referencia
        });
    }

    foreach (var p in dto.Pagos)
    {
        _context.VentaPagos.Add(new VentaPago
        {
            VentaId = venta.Id,
            MedioPago = p.MedioPago,
            Monto = p.Monto,
            Referencia = p.Referencia
        });

        _context.ReservaPagos.Add(new ReservaPago
        {
            ReservaId = reserva.Id,
            MedioPago = p.MedioPago,
            Monto = p.Monto,
            Referencia = p.Referencia
        });

        if (p.MedioPago != MedioPago.NotaCredito)
        {
            _context.MovimientosCaja.Add(new MovimientoCaja
            {
                Tipo = TipoMovimientoCaja.Ingreso,
                Monto = p.Monto,
                Concepto = $"Pago saldo reserva #{reserva.Id}",
                VentaId = venta.Id,
                MedioPago = p.MedioPago,
                Referencia = p.Referencia
            });
        }
    }

    reserva.Estado = EstadoReserva.Pagada;
    reserva.SaldoPendiente = 0m;
    reserva.VentaId = venta.Id;

    await _dvService.RecalcularEntidadAsync(venta);
    var movimientosCaja = _context.ChangeTracker.Entries<MovimientoCaja>()
        .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
        .Select(e => e.Entity)
        .ToList();
    foreach (var mov in movimientosCaja) await _dvService.RecalcularEntidadAsync(mov);

    await _context.SaveChangesAsync();
    await tx.CommitAsync();
    await _dvService.RecalcularDVVAsync("Ventas");
    await _dvService.RecalcularDVVAsync("MovimientosCaja");
    await _bitacoraService.RegistrarAsync(new BitacoraEntry
    {
        Accion = "RESERVA_PRESENCIAL_COMPLETAR_PAGO",
        Detalle = $"Reserva #{reserva.Id} completada en venta #{venta.Id}.",
        Resultado = "OK"
    });

    return venta.Id;
}

public async Task CancelarReservaAsync(int reservaId, string? motivo)
{
    var reserva = await _context.Reservas.FirstOrDefaultAsync(r => r.Id == reservaId);
    if (reserva == null) throw new InvalidOperationException("Reserva inexistente.");
    if (reserva.Estado is EstadoReserva.Pagada or EstadoReserva.Cancelada)
        throw new InvalidOperationException("La reserva no se puede cancelar en su estado actual.");

    reserva.Estado = EstadoReserva.Cancelada;
    reserva.Observaciones = string.IsNullOrWhiteSpace(motivo)
        ? reserva.Observaciones
        : string.IsNullOrWhiteSpace(reserva.Observaciones)
            ? $"Cancelada: {motivo.Trim()}"
            : $"{reserva.Observaciones} | Cancelada: {motivo.Trim()}";

    await _context.SaveChangesAsync();
    await _bitacoraService.RegistrarAsync(new BitacoraEntry
    {
        Accion = "RESERVA_PRESENCIAL_CANCELAR",
        Detalle = $"Reserva #{reservaId} cancelada.",
        Resultado = "OK"
    });
}

public async Task<int> VencerReservasAsync(DateTime? fechaCorte = null)
{
    return 0;
}

public async Task<List<ReservaListItemDto>> ObtenerReservasAsync(
    EstadoReserva? estado,
    DateTime? desde,
    DateTime? hasta,
    string? dni)
{
    var q = _context.Reservas
        .AsNoTracking()
        .Include(r => r.Items)
        .AsQueryable();

    if (estado.HasValue) q = q.Where(r => r.Estado == estado.Value);
    if (desde.HasValue) q = q.Where(r => r.FechaCreacion >= desde.Value);
    if (hasta.HasValue) q = q.Where(r => r.FechaCreacion <= hasta.Value);
    if (!string.IsNullOrWhiteSpace(dni))
    {
        var dniNorm = NormalizarDni(dni);
        q = q.Where(r => r.ClienteDni != null && r.ClienteDni == dniNorm);
    }

    return await q
        .OrderByDescending(r => r.FechaCreacion)
        .Select(r => new ReservaListItemDto
        {
            Id = r.Id,
            FechaCreacion = r.FechaCreacion,
            FechaVencimiento = r.FechaVencimiento,
            ClienteDni = r.ClienteDni,
            ClienteNombre = r.ClienteNombre,
            ClienteTelefono = r.ClienteTelefono,
            Total = r.Total,
            MontoSenia = r.MontoSenia,
            SaldoPendiente = r.SaldoPendiente,
            RequiereCompra = r.RequiereCompra,
            Estado = r.Estado,
            CantidadItems = r.Items.Sum(i => i.Cantidad),
            VentaId = r.VentaId,
            PedidoSeniaId = _context.PedidosSenia
                .Where(ps => ps.ReservaId == r.Id)
                .Select(ps => (int?)ps.Id)
                .FirstOrDefault(),
            EstadoPedidoSenia = _context.PedidosSenia
                .Where(ps => ps.ReservaId == r.Id)
                .Select(ps => (int?)ps.Estado)
                .FirstOrDefault()
        })
        .ToListAsync();
}

public async Task<List<ReservaLibroResumenDto>> ObtenerResumenLibrosSeniaAsync(bool incluirCerradas = false)
{
    var query = _context.ReservaItems
        .AsNoTracking()
        .Include(ri => ri.Reserva)
        .Include(ri => ri.Producto)
        .AsQueryable();

    if (!incluirCerradas)
    {
        query = query.Where(ri =>
            ri.Reserva.Estado == EstadoReserva.PendienteAnticipo
            || ri.Reserva.Estado == EstadoReserva.Senada);
    }

    var data = await query
        .GroupBy(ri => ri.Producto != null ? ri.Producto.Nombre : "(sin nombre)")
        .Select(g => new ReservaLibroResumenDto
        {
            NombreLibro = g.Key,
            CantidadTotal = g.Sum(x => x.Cantidad),
            CantidadReservas = g.Select(x => x.ReservaId).Distinct().Count(),
            CantidadConStock = g.Where(x => !x.EsAConseguir).Sum(x => x.Cantidad),
            CantidadAConseguir = g.Where(x => x.EsAConseguir).Sum(x => x.Cantidad)
        })
        .OrderByDescending(x => x.CantidadTotal)
        .ThenBy(x => x.NombreLibro)
        .ToListAsync();

    return data;
}

public async Task<ReservaDetalleDto?> ObtenerReservaPorIdAsync(int reservaId)
{
    var reserva = await _context.Reservas
        .AsNoTracking()
        .Include(r => r.Items)
            .ThenInclude(i => i.Producto)
        .Include(r => r.Pagos)
        .FirstOrDefaultAsync(r => r.Id == reservaId);

    if (reserva == null) return null;

    return new ReservaDetalleDto
    {
        Id = reserva.Id,
        FechaCreacion = reserva.FechaCreacion,
        FechaVencimiento = reserva.FechaVencimiento,
        ClienteDni = reserva.ClienteDni,
        ClienteNombre = reserva.ClienteNombre,
        ClienteTelefono = reserva.ClienteTelefono,
        Total = reserva.Total,
        MontoSenia = reserva.MontoSenia,
        SaldoPendiente = reserva.SaldoPendiente,
        RequiereCompra = reserva.RequiereCompra,
        Estado = reserva.Estado,
        Observaciones = reserva.Observaciones,
        VentaId = reserva.VentaId,
        PedidoSeniaId = await _context.PedidosSenia
            .Where(ps => ps.ReservaId == reserva.Id)
            .Select(ps => (int?)ps.Id)
            .FirstOrDefaultAsync(),
        EstadoPedidoSenia = await _context.PedidosSenia
            .Where(ps => ps.ReservaId == reserva.Id)
            .Select(ps => (int?)ps.Estado)
            .FirstOrDefaultAsync(),
        Items = reserva.Items.Select(i => new ReservaDetalleItemDto
        {
            ProductoId = i.ProductoId,
            NombreProducto = i.Producto?.Nombre ?? "(sin nombre)",
            Cantidad = i.Cantidad,
            PrecioUnitario = i.PrecioUnitario,
            Subtotal = i.Subtotal,
            EsAConseguir = i.EsAConseguir
        }).ToList(),
        Pagos = reserva.Pagos.Select(p => new ReservaDetallePagoDto
        {
            MedioPago = p.MedioPago,
            Monto = p.Monto,
            Referencia = p.Referencia,
            Fecha = p.Fecha
        }).ToList()
    };
}

private static string NormalizarDni(string? dni)
{
	    if (string.IsNullOrWhiteSpace(dni))
	        return string.Empty;

    var trimmed = dni.Trim();
	    var soloDigitos = new string(trimmed.Where(char.IsDigit).ToArray());
	    return string.IsNullOrWhiteSpace(soloDigitos) ? trimmed : soloDigitos;
}

private static string NormalizarDniObligatorio(string? dni)
{
	    var normalizado = NormalizarDni(dni);
	    if (string.IsNullOrWhiteSpace(normalizado))
	        throw new InvalidOperationException("El DNI es obligatorio.");
	    if (normalizado.Length > 20)
	        throw new InvalidOperationException("El DNI no puede superar 20 caracteres.");
	    return normalizado;
}

private static string NormalizarTextoObligatorio(string? valor, string campo, int maxLength)
{
	    if (string.IsNullOrWhiteSpace(valor))
	        throw new InvalidOperationException($"El {campo} es obligatorio.");
	    var normalizado = valor.Trim();
	    if (normalizado.Length > maxLength)
	        throw new InvalidOperationException($"El {campo} no puede superar {maxLength} caracteres.");
	    return normalizado;
}

private async Task<Dictionary<int, int>> ObtenerReservadoActivoPorProductoAsync(
    IEnumerable<int> productoIds,
    int? excluirReservaId = null)
{
    var ids = productoIds.Distinct().ToList();
    if (ids.Count == 0) return new Dictionary<int, int>();

    var query = _context.ReservaItems
        .Where(ri => ids.Contains(ri.ProductoId)
            && !ri.EsAConseguir
            && ri.Reserva.Estado == EstadoReserva.Senada
            );

    if (excluirReservaId.HasValue)
        query = query.Where(ri => ri.ReservaId != excluirReservaId.Value);

    return await query
        .GroupBy(ri => ri.ProductoId)
        .Select(g => new { ProductoId = g.Key, Reservado = g.Sum(x => x.Cantidad) })
        .ToDictionaryAsync(x => x.ProductoId, x => x.Reservado);
}

private async Task ValidarYAplicarNotaCreditoAsync(string? dniClienteNormalizado, List<PagoItemDto> pagos)
{
    foreach (var p in pagos)
    {
        if (p.Monto <= 0m)
            throw new InvalidOperationException("Monto de pago invÃ¡lido.");

        if (p.MedioPago != MedioPago.NotaCredito) continue;

        if (string.IsNullOrWhiteSpace(dniClienteNormalizado))
            throw new InvalidOperationException("Para usar Nota de CrÃ©dito se requiere DNI del cliente.");

        var notasCliente = await _context.NotasCredito
            .Where(x => x.ClienteDni != null &&
                        x.ClienteDni.Replace(".", "").Replace("-", "").Replace(" ", "") == dniClienteNormalizado)
            .OrderBy(x => x.FechaCreacion)
            .ThenBy(x => x.Id)
            .ToListAsync();

        var saldoTotal = notasCliente.Sum(x => x.SaldoDisponible);
        if (notasCliente.Count == 0 || saldoTotal < p.Monto)
            throw new InvalidOperationException("Saldo de Nota de CrÃ©dito insuficiente.");

        var pendiente = p.Monto;
        foreach (var nc in notasCliente)
        {
            if (pendiente <= 0m) break;
            var uso = Math.Min(nc.SaldoDisponible, pendiente);
            nc.SaldoDisponible -= uso;
            pendiente -= uso;
        }
    }
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
            throw new UnauthorizedAccessException("No tenÃ©s permisos para ver esta venta.");

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
            .AsNoTracking();

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






