using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Dtos.Compras;
using pruebaPagoMp.Models.Bitacora;
using pruebaPagoMp.Services.Compras;
using pruebaPagoMp.Services.Bitacora;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/proveedores")]
[Authorize(Roles = "AdminCompras,AdminGeneral")]
public class ProveedoresController : ControllerBase
{
    private readonly IComprasService _comprasService;
    private readonly IBitacoraService _bitacoraService;

    public ProveedoresController(IComprasService comprasService, IBitacoraService bitacoraService)
    {
        _comprasService = comprasService;
        _bitacoraService = bitacoraService;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool? activos)
    {
        var data = await _comprasService.ListarProveedoresAsync(activos);
        return Ok(data);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] ProveedorCreateDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        try
        {
            var id = await _comprasService.CrearProveedorAsync(dto);
            return Ok(new { id });
        }
        catch (Exception ex)
        {
            await _bitacoraService.RegistrarAsync(new BitacoraEntry
            {
                Accion = "PROVEEDOR_CREAR",
                Detalle = ex.Message,
                Ip = ip,
                Resultado = "ERROR"
            });
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalle(int id)
    {
        var data = await _comprasService.ObtenerProveedorPorIdAsync(id);
        if (data == null) return NotFound();
        return Ok(data);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ProveedorCreateDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        try
        {
            await _comprasService.ActualizarProveedorAsync(id, dto);
            return NoContent();
        }
        catch (Exception ex)
        {
            await _bitacoraService.RegistrarAsync(new BitacoraEntry
            {
                Accion = "PROVEEDOR_ACTUALIZAR",
                Detalle = ex.Message,
                Ip = ip,
                Resultado = "ERROR"
            });
            return BadRequest(new { error = ex.Message });
        }
    }
}
