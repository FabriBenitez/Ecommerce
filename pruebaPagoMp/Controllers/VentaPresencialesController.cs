using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        var ventaId = await _ventasService.CrearVentaPresencialAsync(dto);
        return Ok(new { ventaId });
    }
}
