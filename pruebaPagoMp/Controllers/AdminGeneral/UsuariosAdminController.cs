using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Dtos.Admin.Usuarios;
using pruebaPagoMp.Services.Admin.Usuarios;

namespace pruebaPagoMp.Controllers.AdminGeneral;

[ApiController]
[Route("api/admin-general/usuarios")]
[Authorize(Roles = "AdminGeneral")]
public class UsuariosAdminController : ControllerBase
{
    private readonly IUsuariosAdminService _service;

    public UsuariosAdminController(IUsuariosAdminService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        return Ok(await _service.ListarAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalle([FromRoute] int id)
    {
        var item = await _service.ObtenerDetalleAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearUsuarioInternoDto dto)
    {
        try
        {
            var id = await _service.CrearUsuarioInternoAsync(dto);
            return CreatedAtAction(nameof(Detalle), new { id }, new { id });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar([FromRoute] int id, [FromBody] ActualizarUsuarioInternoDto dto)
    {
        try
        {
            await _service.ActualizarUsuarioInternoAsync(id, dto);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}/roles")]
    public async Task<IActionResult> AsignarRoles([FromRoute] int id, [FromBody] AsignarRolesDto dto)
    {
        try
        {
            await _service.AsignarRolesAsync(id, dto);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}/activo")]
    public async Task<IActionResult> CambiarActivo([FromRoute] int id, [FromBody] CambiarActivoDto dto)
    {
        try
        {
            await _service.CambiarActivoAsync(id, dto);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
