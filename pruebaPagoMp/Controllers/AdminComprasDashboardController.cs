using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.Models.Compras.Enums;
using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/admin-compras/dashboard")]
[Authorize(Roles = "AdminCompras,AdminGeneral")]
public class AdminComprasDashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminComprasDashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? stockMinimo = null)
    {
        if (!stockMinimo.HasValue)
        {
            stockMinimo = await _context.ConfiguracionesSistema
                .AsNoTracking()
                .Where(x => x.Id == 1)
                .Select(x => (int?)x.StockMinimoAlerta)
                .FirstOrDefaultAsync() ?? 10;
        }

        var stockBajoCount = await _context.Productos
            .AsNoTracking()
            .CountAsync(p =>
                !p.EsTemporalSenia &&
                p.Stock - (
                    _context.ReservaItems
                        .Where(ri =>
                            ri.ProductoId == p.Id
                            && !ri.EsAConseguir
                            && ri.Reserva.Estado == EstadoReserva.Senada)
                        .Sum(ri => (int?)ri.Cantidad) ?? 0
                ) < stockMinimo.Value);

        var comprasPendientesCount = await _context.Compras
            .AsNoTracking()
            .CountAsync(c => c.EstadoCompra == EstadoCompra.Pendiente);

        var ultimasConfirmadas = await _context.Compras
            .AsNoTracking()
            .Include(c => c.Proveedor)
            .Where(c => c.EstadoCompra == EstadoCompra.Confirmada)
            .OrderByDescending(c => c.Fecha)
            .Take(5)
            .Select(c => new
            {
                id = c.Id,
                fecha = c.Fecha,
                proveedor = c.Proveedor.RazonSocial,
                total = c.Total,
                estadoCompra = c.EstadoCompra
            })
            .ToListAsync();

        return Ok(new
        {
            stockMinimo = stockMinimo.Value,
            stockBajoCount,
            comprasPendientesCount,
            ultimasConfirmadas
        });
    }
}
