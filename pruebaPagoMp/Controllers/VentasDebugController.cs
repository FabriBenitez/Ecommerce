using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Services.Ventas;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/debug/ventas")]
public class VentasDebugController : ControllerBase
{
    private readonly IVentasService _ventas;

    public VentasDebugController(IVentasService ventas)
    {
        _ventas = ventas;
    }

    [HttpPost("{ventaId:int}/aprobar")]
    [AllowAnonymous] // solo dev; después lo borrás
    public async Task<IActionResult> Aprobar(int ventaId)
    {
        await _ventas.ConfirmarPagoVentaAsync(ventaId, "TEST_PAYMENT_ID", "test", "approved");
        return Ok(new { ventaId, status = "approved" });
    }
}
