using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using pruebaPagoMp.Dtos.Ventas;
using pruebaPagoMp.Services.Ventas;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/ventas")]
public class VentasController : ControllerBase
{
    private readonly IVentasService _ventasService;

    public VentasController(IVentasService ventasService)
    {
        _ventasService = ventasService;
    }

    [HttpPost("web/checkout")]
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
        // lo implementamos en service: ObtenerVentaPorIdAsync(id, usuarioId, esAdmin)
        return Ok(new {id}); // lo completamos después de crear el método
    }

    [HttpGet("mis-ventas")]
    public IActionResult MisVentas()
    {
        return StatusCode(501, "Pendiente de implementación: MisVentas()");
    }

}
