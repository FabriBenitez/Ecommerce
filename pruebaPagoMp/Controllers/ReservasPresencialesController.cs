using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using pruebaPagoMp.Dtos.Ventas;
using pruebaPagoMp.Models.Bitacora;
using pruebaPagoMp.Models.Ventas.Enums;
using pruebaPagoMp.Services.Bitacora;
using pruebaPagoMp.Services.Ventas;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/reservas-presenciales")]
[Authorize(Roles = "AdminVentas")]
public class ReservasPresencialesController : ControllerBase
{
    private readonly IVentasService _ventasService;
    private readonly IBitacoraService _bitacoraService;

    public ReservasPresencialesController(IVentasService ventasService, IBitacoraService bitacoraService)
    {
        _ventasService = ventasService;
        _bitacoraService = bitacoraService;
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearReservaPresencialDto dto)
    {
        if (!TryGetUsuarioId(out var adminUsuarioId))
            return Unauthorized("Token invalido (sin usuarioId).");

        try
        {
            var reservaId = await _ventasService.CrearReservaPresencialAsync(adminUsuarioId, dto);
            return Ok(new { reservaId });
        }
        catch (InvalidOperationException ex)
        {
            await RegistrarErrorAsync("RESERVA_PRESENCIAL_CREAR", ex.Message, adminUsuarioId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] EstadoReserva? estado,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string? dni)
    {
        var data = await _ventasService.ObtenerReservasAsync(estado, desde, hasta, dni);
        return Ok(data);
    }

    [HttpGet("libros-resumen")]
    public async Task<IActionResult> ResumenLibros([FromQuery] bool incluirCerradas = false)
    {
        var data = await _ventasService.ObtenerResumenLibrosSeniaAsync(incluirCerradas);
        return Ok(data);
    }

    [HttpGet("{reservaId:int}")]
    public async Task<IActionResult> Detalle([FromRoute] int reservaId)
    {
        var data = await _ventasService.ObtenerReservaPorIdAsync(reservaId);
        if (data == null) return NotFound(new { error = "Reserva inexistente." });
        return Ok(data);
    }

    [HttpPost("{reservaId:int}/completar-pago")]
    public async Task<IActionResult> CompletarPago([FromRoute] int reservaId, [FromBody] CompletarReservaPagoDto dto)
    {
        if (!TryGetUsuarioId(out var adminUsuarioId))
            return Unauthorized("Token invalido (sin usuarioId).");

        try
        {
            var ventaId = await _ventasService.CompletarPagoReservaAsync(reservaId, dto);
            return Ok(new { ventaId });
        }
        catch (InvalidOperationException ex)
        {
            await RegistrarErrorAsync("RESERVA_PRESENCIAL_COMPLETAR_PAGO", ex.Message, adminUsuarioId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{reservaId:int}/cancelar")]
    public async Task<IActionResult> Cancelar([FromRoute] int reservaId, [FromBody] CancelarReservaRequest req)
    {
        if (!TryGetUsuarioId(out var adminUsuarioId))
            return Unauthorized("Token invalido (sin usuarioId).");

        try
        {
            await _ventasService.CancelarReservaAsync(reservaId, req.Motivo);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            await RegistrarErrorAsync("RESERVA_PRESENCIAL_CANCELAR", ex.Message, adminUsuarioId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("vencer-expiradas")]
    public async Task<IActionResult> VencerExpiradas([FromQuery] DateTime? fechaCorte = null)
    {
        var count = await _ventasService.VencerReservasAsync(fechaCorte);
        return Ok(new { vencidas = count });
    }

    private bool TryGetUsuarioId(out int userId)
    {
        userId = 0;
        var usuarioIdStr =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");
        return int.TryParse(usuarioIdStr, out userId);
    }

    private Task RegistrarErrorAsync(string accion, string detalle, int? usuarioId)
    {
        return _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            UsuarioId = usuarioId,
            Accion = accion,
            Detalle = detalle,
            Ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
            Resultado = "ERROR"
        });
    }

    public class CancelarReservaRequest
    {
        public string? Motivo { get; set; }
    }
}
