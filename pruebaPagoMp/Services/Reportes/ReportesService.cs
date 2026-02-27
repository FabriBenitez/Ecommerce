using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.Dtos.Reportes;
using pruebaPagoMp.Models.Caja.Enums;

namespace pruebaPagoMp.Services.Reportes;

public class ReportesService : IReportesService
{
    private readonly ApplicationDbContext _context;

    public ReportesService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReporteVentasDto> ObtenerVentasAsync(DateTime desde, DateTime hasta)
    {
        if (hasta < desde) throw new InvalidOperationException("El rango de fechas es inválido.");

        var q = _context.Ventas
            .AsNoTracking()
            .Where(v => v.Fecha >= desde && v.Fecha <= hasta);

        return new ReporteVentasDto
        {
            Desde = desde,
            Hasta = hasta,
            CantidadVentas = await q.CountAsync(),
            TotalVendido = await q.SumAsync(v => (decimal?)v.Total) ?? 0m
        };
    }

    public async Task<List<ReporteStockDto>> ObtenerStockAsync()
    {
        return await _context.Productos
            .AsNoTracking()
            .Where(p => p.Stock <= 10)
            .OrderBy(p => p.Nombre)
            .Select(p => new ReporteStockDto
            {
                ProductoId = p.Id,
                Producto = p.Nombre,
                StockActual = p.Stock
            })
            .ToListAsync();
    }

    public async Task<ReporteCajaDto> ObtenerCajaAsync(DateTime fecha)
    {
        var inicio = fecha.Date;
        var fin = inicio.AddDays(1);

        var movimientos = await _context.MovimientosCaja
            .AsNoTracking()
            .Where(m => m.Fecha >= inicio && m.Fecha < fin)
            .ToListAsync();

        var ingresos = movimientos.Where(m => m.Tipo == TipoMovimientoCaja.Ingreso).Sum(m => m.Monto);
        var egresos = movimientos.Where(m => m.Tipo == TipoMovimientoCaja.Egreso).Sum(m => m.Monto);

        return new ReporteCajaDto
        {
            Fecha = inicio,
            Ingresos = ingresos,
            Egresos = egresos,
            SaldoNeto = ingresos - egresos
        };
    }
}
