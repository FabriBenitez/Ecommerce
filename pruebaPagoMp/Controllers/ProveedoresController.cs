using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Dtos.Compras;
using pruebaPagoMp.Services.Compras;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/proveedores")]
[Authorize(Roles = "AdminCompras,AdminGeneral")]
public class ProveedoresController : ControllerBase
{
    private readonly IComprasService _comprasService;

    public ProveedoresController(IComprasService comprasService)
    {
        _comprasService = comprasService;
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
        var id = await _comprasService.CrearProveedorAsync(dto);
        return Ok(new { id });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalle(int id)
    {
        var data = await _comprasService.ObtenerProveedorPorIdAsync(id);
        if (data == null) return NotFound();
        return Ok(data);
    }
}