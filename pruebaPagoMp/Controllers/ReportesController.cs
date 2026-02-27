using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Services.Reportes;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/reportes")]
[Authorize(Roles = "AdminGeneral")]
public class ReportesController : ControllerBase
{
    private readonly IReportesService _service;

    public ReportesController(IReportesService service)
    {
        _service = service;
    }

    [HttpGet("ventas")]
    public async Task<IActionResult> Ventas([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var fechaHasta = hasta ?? DateTime.UtcNow;
        var fechaDesde = desde ?? fechaHasta.AddDays(-7);
        return Ok(await _service.ObtenerVentasAsync(fechaDesde, fechaHasta));
    }

    [HttpGet("stock")]
    public async Task<IActionResult> Stock()
    {
        return Ok(await _service.ObtenerStockAsync());
    }

    [HttpGet("caja")]
    public async Task<IActionResult> Caja([FromQuery] DateTime? fecha)
    {
        return Ok(await _service.ObtenerCajaAsync(fecha ?? DateTime.UtcNow));
    }
}
