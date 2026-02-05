using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using pruebaPagoMp.Data;
using pruebaPagoMp.Models.Ventas;
using pruebaPagoMp.Services.Pagos;
using pruebaPagoMp.Models.Ventas.Enums;


namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/ventas")]
public class VentasController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMercadoPagoService _mercadoPagoService;

    public VentasController(ApplicationDbContext context, IMercadoPagoService mercadoPagoService)
    {
        _context = context;
        _mercadoPagoService = mercadoPagoService;
    }

    [HttpPost("web/checkout")]
    [Authorize]
    public async Task<IActionResult> CheckoutWeb()
    {
        // 1) Usuario logueado
        var usuarioIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                           ?? User.FindFirstValue("sub");

        if (!int.TryParse(usuarioIdStr, out var usuarioId))
            return Unauthorized("Usuario inválido");

        // 2) Carrito activo + items
        var carrito = await _context.Carritos
            .Include(c => c.CarritoItems)
                .ThenInclude(ci => ci.Producto)
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

        if (carrito == null || carrito.CarritoItems.Count == 0)
            return BadRequest("El carrito está vacío");

        // 3) Pre-chequeo de stock
        foreach (var item in carrito.CarritoItems)
        {
            if (item.Producto.Stock < item.Cantidad)
                return BadRequest($"Stock insuficiente para: {item.Producto.Nombre}");
        }

        // 4) Crear Venta Pendiente + Detalles (precio congelado)
        var venta = new Venta
        {
            Fecha = DateTime.UtcNow,
            UsuarioId = usuarioId,
            EstadoVenta = EstadoVenta.Pendiente,
            Canal = CanalVenta.Web,
            Total = 0m,
            Detalles = new List<DetalleVenta>()
        };

        foreach (var item in carrito.CarritoItems)
        {
            var precio = item.Producto.Precio; // congelado
            var subtotal = precio * item.Cantidad;

            venta.Detalles.Add(new DetalleVenta
            {
                ProductoId = item.ProductoId,
                Cantidad = item.Cantidad,
                PrecioUnitario = precio,
                Subtotal = subtotal
            });

            venta.Total += subtotal;
        }

        _context.Ventas.Add(venta);
        await _context.SaveChangesAsync(); // acá ya tenés venta.Id

        // 5) Crear preferencia en Mercado Pago (Paso 2)
        var (preferenceId, urlPago) = await _mercadoPagoService.CrearPreferenciaPagoAsync(
            venta.Id,
            venta.Total,
            $"Venta #{venta.Id} - Compra Web"
        );


        // 6) Respuesta al front
        return Ok(new
        {
            ventaId = venta.Id,
            urlPago = urlPago,
            preferenceId = preferenceId
        });
    }
}

