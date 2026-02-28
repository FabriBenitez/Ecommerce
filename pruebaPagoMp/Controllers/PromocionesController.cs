using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Dtos.Promociones;
using pruebaPagoMp.Services.Promociones;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/promociones")]
[Authorize(Roles = "AdminGeneral,AdminCompras")]
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

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desactivar([FromRoute] int id)
    {
        try
        {
            await _service.DesactivarAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("by-producto/{productoId:int}")]
    public async Task<IActionResult> DesactivarPorProducto([FromRoute] int productoId)
    {
        var count = await _service.DesactivarPorProductoAsync(productoId);
        return Ok(new { desactivadas = count });
    }

    [HttpDelete("by-genero")]
    public async Task<IActionResult> DesactivarPorGenero([FromQuery] string genero)
    {
        try
        {
            var count = await _service.DesactivarPorGeneroAsync(genero);
            return Ok(new { desactivadas = count });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
