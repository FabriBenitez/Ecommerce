using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Dtos.Retiros;
using pruebaPagoMp.Models.Ventas.Enums;
using pruebaPagoMp.Services.Retiros;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/retiros")]
[Authorize(Roles = "AdminVentas")]
public class RetirosController : ControllerBase
{
    private readonly IRetirosService _retiros;

    public RetirosController(IRetirosService retiros)
    {
        _retiros = retiros;
    }

    // GET /api/retiros?estado=Pendiente
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] EstadoRetiro? estado)
    {
        var data = await _retiros.ListarAsync(estado);
        return Ok(data);
    }

    // PUT /api/retiros/123/estado  body: { "estadoRetiro": 2 }
    [HttpPut("{ventaId:int}/estado")]
    public async Task<IActionResult> CambiarEstado([FromRoute] int ventaId, [FromBody] CambiarEstadoRetiroDto dto)
    {
        var ok = await _retiros.CambiarEstadoAsync(ventaId, dto.EstadoRetiro);
        if (!ok) return NotFound();
        return NoContent();
    }
}
