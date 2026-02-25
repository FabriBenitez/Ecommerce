using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Dtos.Compras;
using pruebaPagoMp.Services.Compras;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/compras")]
[Authorize(Roles = "AdminCompras,AdminGeneral")]
public class ComprasController : ControllerBase
{
    private readonly IComprasService _comprasService;

    public ComprasController(IComprasService comprasService)
    {
        _comprasService = comprasService;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? proveedorId)
    {
        var data = await _comprasService.ListarComprasAsync(proveedorId);
        return Ok(data);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalle(int id)
    {
        var data = await _comprasService.ObtenerCompraDetalleAsync(id);
        if (data == null) return NotFound();
        return Ok(data);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearCompraDto dto)
    {
        try
        {
            var id = await _comprasService.CrearCompraAsync(dto);
            return Ok(new { id });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:int}/confirmar")]
    public async Task<IActionResult> Confirmar(int id)
    {
        try
        {
            await _comprasService.ConfirmarCompraAsync(id);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:int}/factura")]
    public async Task<IActionResult> RegistrarFactura(int id, [FromBody] RegistrarFacturaProveedorDto dto)
    {
        try
        {
            await _comprasService.RegistrarFacturaProveedorAsync(id, dto);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
