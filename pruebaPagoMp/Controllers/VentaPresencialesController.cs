using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using pruebaPagoMp.Dtos.Ventas;
using pruebaPagoMp.Services.Ventas;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/ventas-presenciales")]
public class VentasPresencialesController : ControllerBase
{
    private readonly IVentasService _ventasService;

    public VentasPresencialesController(IVentasService ventasService)
    {
        _ventasService = ventasService;
    }

    [HttpPost]
    [Authorize(Roles = "AdminVentas")]
    public async Task<IActionResult> Crear([FromBody] CrearVentaPresencialDto dto)
    {
        var usuarioIdStr =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");

        if (!int.TryParse(usuarioIdStr, out var adminUsuarioId))
            return Unauthorized("Token invÃ¡lido (sin usuarioId).");

        var ventaId = await _ventasService.CrearVentaPresencialAsync(adminUsuarioId, dto);
        return Ok(new { ventaId });
    }
    [HttpGet("historial")]
    [Authorize(Roles = "AdminVentas")]
    public async Task<IActionResult> Historial()
    {
        var data = await _ventasService.ObtenerHistorialPresencialAsync();
        return Ok(data);
    }
    [HttpPost("{ventaId:int}/devolucion")]
    [Authorize(Roles = "AdminVentas")]
    public async Task<IActionResult> Devolucion([FromRoute] int ventaId, [FromBody] DevolucionPresencialDto dto)
    {
        var notaCreditoId = await _ventasService.RegistrarDevolucionPresencialAsync(ventaId, dto);
        return Ok(new { notaCreditoId });
    }

}
