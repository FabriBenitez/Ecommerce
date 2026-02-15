using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using pruebaPagoMp.Dtos.Ventas;
using pruebaPagoMp.Services.Ventas;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/ventas")]
[Authorize]
public class VentasController : ControllerBase
{
    private readonly IVentasService _ventasService;

    public VentasController(IVentasService ventasService)
    {
        _ventasService = ventasService;
    }

    [HttpPost("web/checkout")]
    [Authorize]
    public async Task<ActionResult<CheckoutVentaWebRespuestaDto>> CheckoutWeb([FromBody] CheckoutVentaWebDto dto)
    {
        var usuarioIdStr =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");

        if (!int.TryParse(usuarioIdStr, out var usuarioId))
            return Unauthorized("Token inválido (sin usuarioId).");

        var resp = await _ventasService.CheckoutVentaWebAsync(usuarioId, dto);
        return Ok(resp);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerVenta(int id)
    {
        var usuarioIdStr =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");

        if (!int.TryParse(usuarioIdStr, out var usuarioId))
            return Unauthorized("Token inválido (sin usuarioId).");

        var esAdminVentas = User.IsInRole("AdminVentas");

        try
        {
            var venta = await _ventasService.ObtenerVentaPorIdAsync(id, usuarioId, esAdminVentas);
            if (venta == null) return NotFound();
            return Ok(venta);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
    }

    [HttpGet("mis-ventas")]
    public async Task<IActionResult> MisVentas()
    {
        var usuarioIdStr =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");

        if (!int.TryParse(usuarioIdStr, out var usuarioId))
            return Unauthorized("Token inválido (sin usuarioId).");

        var ventas = await _ventasService.ObtenerMisVentasAsync(usuarioId);
        return Ok(ventas);
    }

}
