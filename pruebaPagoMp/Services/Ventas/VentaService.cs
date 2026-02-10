using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.Dtos.Ventas;
using pruebaPagoMp.Models;
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
        venta.Observaciones = $"Entrega: {dto.NombreEntrega} - Tel: {dto.TelefonoEntrega} - Dir: {dto.DireccionEntrega}, {dto.Ciudad}, {dto.Provincia} ({dto.CodigoPostal})" + (string.IsNullOrWhiteSpace(dto.Observaciones) ? "" : $" | Obs: {dto.Observaciones}");

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



    public async Task<int> CrearVentaPresencialAsync(CrearVentaPresencialDto dto)
    {
        // 1) Validar items
        if (dto.Items == null || dto.Items.Count == 0)
            throw new InvalidOperationException("Debe incluir al menos un item.");

        var productoIds = dto.Items.Select(i => i.ProductoId).Distinct().ToList();
        var productos = await _context.Productos
            .Where(p => productoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var item in dto.Items)
        {
            if (!productos.TryGetValue(item.ProductoId, out var prod))
                throw new InvalidOperationException($"Producto inexistente: {item.ProductoId}");

            if (item.Cantidad <= 0)
                throw new InvalidOperationException("Cantidad inválida.");

            if (prod.Stock < item.Cantidad)
                throw new InvalidOperationException($"Stock insuficiente para {prod.Nombre}. Disponible: {prod.Stock}");
        }

        // 2) Crear venta pagada directamente
        var venta = new Venta
        {
            Fecha = DateTime.UtcNow,
            UsuarioId = dto.UsuarioId,
            EstadoVenta = EstadoVenta.Pagada,
            Canal = CanalVenta.Presencial,
            Total = 0m
        };

        _context.Ventas.Add(venta);
        await _context.SaveChangesAsync();

        decimal total = 0m;

        foreach (var item in dto.Items)
        {
            var prod = productos[item.ProductoId];
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

            // 3) Descontar stock
            prod.Stock -= item.Cantidad;
        }

        // 4) Crear preferencia MercadoPago usando external_reference = venta.Id

        await _context.SaveChangesAsync();

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
        return await _context.Ventas
            .AsNoTracking()
            .Where(v => v.UsuarioId == usuarioId)
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

}
