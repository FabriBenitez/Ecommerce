using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using pruebaPagoMp.Data;
using pruebaPagoMp.Dtos.Ventas;
using pruebaPagoMp.Models.Bitacora;
using pruebaPagoMp.Models.Ventas.Enums;
using pruebaPagoMp.Services.Bitacora;
using pruebaPagoMp.Services.Ventas;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/ventas-presenciales")]
public class VentasPresencialesController : ControllerBase
{
    private readonly IVentasService _ventasService;
    private readonly ApplicationDbContext _context;
    private readonly IBitacoraService _bitacoraService;

    public VentasPresencialesController(IVentasService ventasService, ApplicationDbContext context, IBitacoraService bitacoraService)
    {
        _ventasService = ventasService;
        _context = context;
        _bitacoraService = bitacoraService;
    }

    [HttpPost]
    [Authorize(Roles = "AdminVentas")]
    public async Task<IActionResult> Crear([FromBody] CrearVentaPresencialDto dto)
    {
        var usuarioIdStr =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");

        if (!int.TryParse(usuarioIdStr, out var adminUsuarioId))
            return Unauthorized("Token invalido (sin usuarioId).");

        try
        {
            var ventaId = await _ventasService.CrearVentaPresencialAsync(adminUsuarioId, dto);
            return Ok(new { ventaId });
        }
        catch (InvalidOperationException ex)
        {
            await _bitacoraService.RegistrarAsync(new BitacoraEntry
            {
                UsuarioId = adminUsuarioId,
                Accion = "VENTA_PRESENCIAL_CREAR",
                Detalle = ex.Message,
                Ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Resultado = "ERROR"
            });
            return BadRequest(new { error = ex.Message });
        }
    }
    [HttpGet("historial")]
    [Authorize(Roles = "AdminVentas")]
    public async Task<IActionResult> Historial(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string? dni,
        [FromQuery] EstadoVenta? estado
    )
    {
        var data = await _ventasService.ObtenerHistorialPresencialAsync(desde, hasta, dni, estado);
        return Ok(data);
    }

    [HttpPost("{ventaId:int}/devolucion")]
    [Authorize(Roles = "AdminVentas")]
    public async Task<IActionResult> Devolucion([FromRoute] int ventaId, [FromBody] DevolucionPresencialDto dto)
    {
        try
        {
            var notaCreditoId = await _ventasService.RegistrarDevolucionPresencialAsync(ventaId, dto);
            return Ok(new { notaCreditoId });
        }
        catch (Exception ex)
        {
            await _bitacoraService.RegistrarAsync(new BitacoraEntry
            {
                Accion = "VENTA_DEVOLUCION",
                Detalle = ex.Message,
                Ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Resultado = "ERROR"
            });
            return BadRequest(new { error = ex.Message });
        }
    }
    [HttpGet("/api/notas-credito/{dni}")]
    [Authorize(Roles="AdminVentas")]
    public async Task<IActionResult> GetNotaCredito(string dni)
    {
        var nc = await _context.NotasCredito.FirstOrDefaultAsync(x => x.ClienteDni == dni);
        if (nc == null) return Ok(new { dni, saldoDisponible = 0m });
        return Ok(new { dni = nc.ClienteDni, saldoDisponible = nc.SaldoDisponible });
    }


}

