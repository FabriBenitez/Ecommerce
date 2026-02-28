using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Security;

namespace pruebaPagoMp.Controllers.AdminGeneral;

[ApiController]
[Route("api/seguridad")]
[Authorize(Roles = "AdminGeneral")]
public class SeguridadController : ControllerBase
{
    private readonly IDigitoVerificadorService _dvService;

    public SeguridadController(IDigitoVerificadorService dvService)
    {
        _dvService = dvService;
    }

    [HttpGet("verificar")]
    public async Task<IActionResult> Verificar()
    {
        var usuarios = await _dvService.VerificarTablaAsync("Usuarios");
        var proveedores = await _dvService.VerificarTablaAsync("Proveedores");
        var compras = await _dvService.VerificarTablaAsync("Compras");
        var ventas = await _dvService.VerificarTablaAsync("Ventas");
        var movimientosCaja = await _dvService.VerificarTablaAsync("MovimientosCaja");

        return Ok(new
        {
            ok = usuarios && proveedores && compras && ventas && movimientosCaja,
            detalle = new
            {
                usuarios,
                proveedores,
                compras,
                ventas,
                movimientosCaja
            }
        });
    }

    [HttpPost("recalcular")]
    public async Task<IActionResult> Recalcular()
    {
        await _dvService.RecalcularDVVAsync("Usuarios");
        await _dvService.RecalcularDVVAsync("Proveedores");
        await _dvService.RecalcularDVVAsync("Compras");
        await _dvService.RecalcularDVVAsync("Ventas");
        await _dvService.RecalcularDVVAsync("MovimientosCaja");

        return Ok(new { ok = true });
    }
}
