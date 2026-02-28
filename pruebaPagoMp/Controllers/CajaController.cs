using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Dtos.Caja;
using pruebaPagoMp.Models.Bitacora;
using pruebaPagoMp.Services.Caja;
using pruebaPagoMp.Services.Bitacora;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/caja")]
[Authorize(Roles = "AdminVentas,AdminGeneral")]
public class CajaController : ControllerBase
{
    private readonly ICajaService _service;
    private readonly IBitacoraService _bitacoraService;

    public CajaController(ICajaService service, IBitacoraService bitacoraService)
    {
        _service = service;
        _bitacoraService = bitacoraService;
    }

    [HttpPost("abrir")]
    public async Task<IActionResult> Abrir([FromBody] AbrirCajaDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        try
        {
            return Ok(await _service.AbrirAsync(dto));
        }
        catch (InvalidOperationException ex)
        {
            await _bitacoraService.RegistrarAsync(new BitacoraEntry
            {
                Accion = "CAJA_APERTURA",
                Detalle = ex.Message,
                Ip = ip,
                Resultado = "ERROR"
            });
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("cerrar")]
    public async Task<IActionResult> Cerrar([FromBody] CerrarCajaDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        try
        {
            return Ok(await _service.CerrarAsync(dto));
        }
        catch (InvalidOperationException ex)
        {
            await _bitacoraService.RegistrarAsync(new BitacoraEntry
            {
                Accion = "CAJA_CIERRE",
                Detalle = ex.Message,
                Ip = ip,
                Resultado = "ERROR"
            });
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

    [HttpGet("historial-diario")]
    public async Task<IActionResult> HistorialDiario([FromQuery] int dias = 30)
    {
        return Ok(await _service.ListarHistorialDiarioAsync(dias));
    }
}
