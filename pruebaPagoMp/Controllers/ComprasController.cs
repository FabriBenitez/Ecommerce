using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Dtos.Compras;
using pruebaPagoMp.Models.Bitacora;
using pruebaPagoMp.Services.Compras;
using pruebaPagoMp.Services.Bitacora;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/compras")]
[Authorize(Roles = "AdminCompras,AdminGeneral")]
public class ComprasController : ControllerBase
{
    private readonly IComprasService _comprasService;
    private readonly IBitacoraService _bitacoraService;

    public ComprasController(IComprasService comprasService, IBitacoraService bitacoraService)
    {
        _comprasService = comprasService;
        _bitacoraService = bitacoraService;
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
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        try
        {
            var id = await _comprasService.CrearCompraAsync(dto);
            return Ok(new { id });
        }
        catch (InvalidOperationException ex)
        {
            await _bitacoraService.RegistrarAsync(new BitacoraEntry
            {
                Accion = "COMPRA_CREAR",
                Detalle = ex.Message,
                Ip = ip,
                Resultado = "ERROR"
            });
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:int}/confirmar")]
    public async Task<IActionResult> Confirmar(int id)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        try
        {
            await _comprasService.ConfirmarCompraAsync(id);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            await _bitacoraService.RegistrarAsync(new BitacoraEntry
            {
                Accion = "COMPRA_CONFIRMAR",
                Detalle = ex.Message,
                Ip = ip,
                Resultado = "ERROR"
            });
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:int}/factura")]
    public async Task<IActionResult> RegistrarFactura(int id, [FromBody] RegistrarFacturaProveedorDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        try
        {
            await _comprasService.RegistrarFacturaProveedorAsync(id, dto);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            await _bitacoraService.RegistrarAsync(new BitacoraEntry
            {
                Accion = "COMPRA_FACTURA_REGISTRAR",
                Detalle = ex.Message,
                Ip = ip,
                Resultado = "ERROR"
            });
            return BadRequest(new { error = ex.Message });
        }
    }
}
