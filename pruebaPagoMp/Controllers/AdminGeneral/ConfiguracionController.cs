using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.Models.Administracion;

namespace pruebaPagoMp.Controllers.AdminGeneral;

[ApiController]
[Route("api/configuracion")]
public class ConfiguracionController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ConfiguracionController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("stock-minimo")]
    [Authorize(Roles = "AdminCompras,AdminGeneral")]
    public async Task<IActionResult> GetStockMinimo()
    {
        var cfg = await EnsureConfigAsync();
        return Ok(new { stockMinimo = cfg.StockMinimoAlerta });
    }

    [HttpPut("stock-minimo")]
    [Authorize(Roles = "AdminGeneral")]
    public async Task<IActionResult> SetStockMinimo([FromBody] StockMinimoDto dto)
    {
        if (dto.StockMinimo < 1 || dto.StockMinimo > 100000)
            return BadRequest(new { error = "Stock minimo invalido." });

        var cfg = await EnsureConfigAsync();
        cfg.StockMinimoAlerta = dto.StockMinimo;
        await _context.SaveChangesAsync();
        return Ok(new { stockMinimo = cfg.StockMinimoAlerta });
    }

    private async Task<ConfiguracionSistema> EnsureConfigAsync()
    {
        var cfg = await _context.ConfiguracionesSistema.FirstOrDefaultAsync();
        if (cfg != null) return cfg;
        cfg = new ConfiguracionSistema { StockMinimoAlerta = 10 };
        _context.ConfiguracionesSistema.Add(cfg);
        await _context.SaveChangesAsync();
        return cfg;
    }

    public class StockMinimoDto
    {
        public int StockMinimo { get; set; }
    }
}
