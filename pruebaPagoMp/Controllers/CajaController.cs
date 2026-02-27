using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Dtos.Caja;
using pruebaPagoMp.Services.Caja;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/caja")]
[Authorize(Roles = "AdminVentas,AdminGeneral")]
public class CajaController : ControllerBase
{
    private readonly ICajaService _service;

    public CajaController(ICajaService service)
    {
        _service = service;
    }

    [HttpPost("abrir")]
    public async Task<IActionResult> Abrir([FromBody] AbrirCajaDto dto)
    {
        try
        {
            return Ok(await _service.AbrirAsync(dto));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("cerrar")]
    public async Task<IActionResult> Cerrar([FromBody] CerrarCajaDto dto)
    {
        try
        {
            return Ok(await _service.CerrarAsync(dto));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        return Ok(await _service.ObtenerResumenAsync());
    }

    [HttpGet("movimientos")]
    public async Task<IActionResult> Movimientos([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        return Ok(await _service.ListarMovimientosAsync(desde, hasta));
    }
}
