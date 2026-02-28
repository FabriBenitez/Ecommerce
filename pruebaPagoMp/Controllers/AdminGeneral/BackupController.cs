using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.Security;

namespace pruebaPagoMp.Controllers.AdminGeneral;

[ApiController]
[Route("api/backup")]
[Authorize(Roles = "AdminGeneral")]
public class BackupController : ControllerBase
{
    private readonly IBackupRestoreService _backupRestoreService;
    private readonly ApplicationDbContext _context;

    public BackupController(IBackupRestoreService backupRestoreService, ApplicationDbContext context)
    {
        _backupRestoreService = backupRestoreService;
        _context = context;
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var payload = await _backupRestoreService.ExportAsync();
        return Ok(payload);
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] BackupPayload payload)
    {
        if (payload == null) return BadRequest("Payload vacío.");
        if (string.IsNullOrWhiteSpace(payload.Version)) return BadRequest("Backup sin versión.");

        var cajaAbierta = await _context.Cajas.AsNoTracking().AnyAsync(c => c.Abierta);
        if (cajaAbierta) return BadRequest("No se puede importar con caja abierta.");

        await _backupRestoreService.ImportAsync(payload);
        return Ok(new { ok = true });
    }
}
