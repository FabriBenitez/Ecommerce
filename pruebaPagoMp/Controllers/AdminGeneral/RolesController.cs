using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;

namespace pruebaPagoMp.Controllers.AdminGeneral;

[ApiController]
[Route("api/admin-general/roles")]
[Authorize(Roles = "AdminGeneral")]
public class RolesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RolesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Nombre)
            .Select(r => new { r.Id, r.Nombre, r.Descripcion })
            .ToListAsync();

        return Ok(roles);
    }
}
