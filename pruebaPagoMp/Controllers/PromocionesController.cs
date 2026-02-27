using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Dtos.Promociones;
using pruebaPagoMp.Services.Promociones;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/promociones")]
[Authorize(Roles = "AdminGeneral")]
public class PromocionesController : ControllerBase
{
    private readonly IPromocionService _service;

    public PromocionesController(IPromocionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool? activas)
    {
        return Ok(await _service.ListarAsync(activas));
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] PromocionCreateDto dto)
    {
        try
        {
            var id = await _service.CrearAsync(dto);
            return CreatedAtAction(nameof(Listar), new { id }, new { id });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
