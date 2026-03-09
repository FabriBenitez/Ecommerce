using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.Dtos.Reportes;
using pruebaPagoMp.Models.Compras.Enums;
using pruebaPagoMp.Models.Caja.Enums;
using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Services.Reportes;

public class ReportesService : IReportesService
{
    private const string CompraProveedorConceptPrefix = "COMPRA_PROVEEDOR:";
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
        var movimientosOperativos = movimientos.Where(m => !EsMovimientoCompraInformativa(m.Concepto)).ToList();

        var totalCompras = await _context.FacturasProveedor
            .AsNoTracking()
            .Where(f => f.Fecha >= inicio && f.Fecha < fin)
            .SumAsync(f => (decimal?)f.Monto) ?? 0m;
        var totalNotasCredito = await _context.VentaPagos
            .AsNoTracking()
            .Where(vp => vp.Fecha >= inicio && vp.Fecha < fin)
            .Where(vp => vp.MedioPago == MedioPago.NotaCredito)
            .SumAsync(vp => (decimal?)vp.Monto) ?? 0m;
        var cantidadNotasCredito = await _context.VentaPagos
            .AsNoTracking()
            .Where(vp => vp.Fecha >= inicio && vp.Fecha < fin)
            .CountAsync(vp => vp.MedioPago == MedioPago.NotaCredito);

        var ingresos = movimientosOperativos.Where(m => m.Tipo == TipoMovimientoCaja.Ingreso).Sum(m => m.Monto);
        var egresos = movimientosOperativos.Where(m => m.Tipo == TipoMovimientoCaja.Egreso).Sum(m => m.Monto);

        return new ReporteCajaDto
        {
            Fecha = inicio,
            Ingresos = ingresos,
            Egresos = egresos,
            TotalCompras = totalCompras,
            TotalNotasCredito = totalNotasCredito,
            CantidadNotasCredito = cantidadNotasCredito,
            SaldoNeto = ingresos - egresos
        };
    }

    public async Task<ReporteEjecutivoDto> ObtenerEjecutivoAsync(DateTime desde, DateTime hasta)
    {
        if (hasta < desde) throw new InvalidOperationException("El rango de fechas es inválido.");

        var inicio = desde;
        var fin = hasta;
        var duracion = fin - inicio;

        var inicioAnterior = inicio - duracion;
        var finAnterior = inicio.AddTicks(-1);

        var ventasPeriodo = _context.Ventas
            .AsNoTracking()
            .Where(v => v.Fecha >= inicio && v.Fecha <= fin)
            .Where(v => v.EstadoVenta == EstadoVenta.Pagada);

        var totalVendido = await ventasPeriodo.SumAsync(v => (decimal?)v.Total) ?? 0m;
        var cantidadVentas = await ventasPeriodo.CountAsync();
        var ticketPromedio = cantidadVentas > 0 ? totalVendido / cantidadVentas : 0m;

        var totalVendidoAnterior = await _context.Ventas
            .AsNoTracking()
            .Where(v => v.Fecha >= inicioAnterior && v.Fecha <= finAnterior)
            .Where(v => v.EstadoVenta == EstadoVenta.Pagada)
            .SumAsync(v => (decimal?)v.Total) ?? 0m;

        var crecimiento = 0m;
        if (totalVendidoAnterior == 0m)
        {
            crecimiento = totalVendido > 0m ? 100m : 0m;
        }
        else
        {
            crecimiento = ((totalVendido - totalVendidoAnterior) / totalVendidoAnterior) * 100m;
        }

        var comprasPeriodo = _context.Compras
            .AsNoTracking()
            .Where(c => c.Fecha >= inicio && c.Fecha <= fin)
            .Where(c => c.EstadoCompra != EstadoCompra.Cancelada);

        var totalComprado = await comprasPeriodo.SumAsync(c => (decimal?)c.Total) ?? 0m;

        var ventasWeb = await ventasPeriodo
            .Where(v => v.Canal == CanalVenta.Web)
            .SumAsync(v => (decimal?)v.Total) ?? 0m;

        var ventasPresencial = await ventasPeriodo
            .Where(v => v.Canal == CanalVenta.Presencial)
            .SumAsync(v => (decimal?)v.Total) ?? 0m;

        var totalCanales = ventasWeb + ventasPresencial;
        var porcentajeWeb = totalCanales > 0m ? (ventasWeb / totalCanales) * 100m : 0m;
        var porcentajePresencial = totalCanales > 0m ? (ventasPresencial / totalCanales) * 100m : 0m;

        var movimientosPeriodo = await _context.MovimientosCaja
            .AsNoTracking()
            .Where(m => m.Fecha >= inicio && m.Fecha <= fin)
            .ToListAsync();
        var movimientosOperativos = movimientosPeriodo.Where(m => !EsMovimientoCompraInformativa(m.Concepto)).ToList();

        var ingresos = movimientosOperativos.Where(m => m.Tipo == TipoMovimientoCaja.Ingreso).Sum(m => m.Monto);
        var egresos = movimientosOperativos.Where(m => m.Tipo == TipoMovimientoCaja.Egreso).Sum(m => m.Monto);
        var totalCompras = await _context.FacturasProveedor
            .AsNoTracking()
            .Where(f => f.Fecha >= inicio && f.Fecha <= fin)
            .SumAsync(f => (decimal?)f.Monto) ?? 0m;
        var totalNotasCredito = await _context.VentaPagos
            .AsNoTracking()
            .Where(vp => vp.Fecha >= inicio && vp.Fecha <= fin)
            .Where(vp => vp.MedioPago == MedioPago.NotaCredito)
            .SumAsync(vp => (decimal?)vp.Monto) ?? 0m;
        var cantidadNotasCredito = await _context.VentaPagos
            .AsNoTracking()
            .Where(vp => vp.Fecha >= inicio && vp.Fecha <= fin)
            .CountAsync(vp => vp.MedioPago == MedioPago.NotaCredito);

        var medioPredominante = movimientosPeriodo
            .Where(m => m.Tipo == TipoMovimientoCaja.Ingreso)
            .GroupBy(m => m.MedioPago)
            .Select(g => new { Medio = g.Key, Total = g.Sum(x => x.Monto) })
            .OrderByDescending(x => x.Total)
            .Select(x => MedioPagoLabel(x.Medio))
            .FirstOrDefault() ?? "Sin datos";

        var stockMinimo = await _context.ConfiguracionesSistema
            .AsNoTracking()
            .Where(c => c.Id == 1)
            .Select(c => (int?)c.StockMinimoAlerta)
            .FirstOrDefaultAsync() ?? 10;

        var inventarioBase = await _context.Productos
            .AsNoTracking()
            .Select(p => new { p.Id, p.Nombre, p.Stock, p.Precio, p.FechaCreacion })
            .ToListAsync();

        var valorTotalInventario = inventarioBase.Sum(p => p.Precio * p.Stock);
        var productosStockBajo = inventarioBase.Count(p => p.Stock <= stockMinimo);

        var topProductos = await _context.DetalleVentas
            .AsNoTracking()
            .Where(d => d.Venta.Fecha >= inicio && d.Venta.Fecha <= fin)
            .Where(d => d.Venta.EstadoVenta == EstadoVenta.Pagada)
            .GroupBy(d => new { d.ProductoId, d.Producto.Nombre })
            .Select(g => new TopProductoVendidoDto
            {
                ProductoId = g.Key.ProductoId,
                Producto = g.Key.Nombre,
                CantidadVendida = g.Sum(x => x.Cantidad),
                TotalVendido = g.Sum(x => x.Subtotal)
            })
            .OrderByDescending(x => x.CantidadVendida)
            .ThenByDescending(x => x.TotalVendido)
            .Take(6)
            .ToListAsync();

        var limiteSinRotacion = fin.AddDays(-60);

        var ultimasVentasPorProducto = await _context.DetalleVentas
            .AsNoTracking()
            .Where(d => d.Venta.EstadoVenta == EstadoVenta.Pagada)
            .Where(d => d.Venta.Fecha <= fin)
            .GroupBy(d => d.ProductoId)
            .Select(g => new { ProductoId = g.Key, UltimaVenta = g.Max(x => x.Venta.Fecha) })
            .ToDictionaryAsync(x => x.ProductoId, x => x.UltimaVenta);

        var sinRotacionBase = inventarioBase
            .Where(p =>
            {
                if (!ultimasVentasPorProducto.TryGetValue(p.Id, out var ultimaVenta)) return true;
                return ultimaVenta < limiteSinRotacion;
            })
            .Select(p =>
            {
                var baseFecha = p.FechaCreacion;
                if (ultimasVentasPorProducto.TryGetValue(p.Id, out var ultimaVenta)) baseFecha = ultimaVenta;
                var dias = (int)Math.Max(0, (fin.Date - baseFecha.Date).TotalDays);

                return new ProductoSinRotacionDto
                {
                    ProductoId = p.Id,
                    Producto = p.Nombre,
                    StockActual = p.Stock,
                    DiasSinVenta = dias
                };
            });

        var sinRotacionCount = sinRotacionBase.Count();

        var sinRotacion = sinRotacionBase
            .OrderByDescending(p => p.DiasSinVenta)
            .ThenByDescending(p => p.StockActual)
            .Take(8)
            .ToList();

        var rankingProveedores = await _context.Compras
            .AsNoTracking()
            .Where(c => c.Fecha >= inicio && c.Fecha <= fin)
            .Where(c => c.EstadoCompra != EstadoCompra.Cancelada)
            .GroupBy(c => new { c.ProveedorId, c.Proveedor.RazonSocial })
            .Select(g => new ProveedorResumenDto
            {
                ProveedorId = g.Key.ProveedorId,
                Proveedor = g.Key.RazonSocial,
                TotalInvertido = g.Sum(x => x.Total),
                FrecuenciaCompras = g.Count()
            })
            .OrderByDescending(x => x.TotalInvertido)
            .ThenByDescending(x => x.FrecuenciaCompras)
            .Take(8)
            .ToListAsync();

        var totalInvertido = rankingProveedores.Sum(x => x.TotalInvertido);
        var frecuenciaCompras = rankingProveedores.Sum(x => x.FrecuenciaCompras);

        return new ReporteEjecutivoDto
        {
            Desde = inicio,
            Hasta = fin,
            GeneradoEn = DateTime.UtcNow,
            Rentabilidad = new RentabilidadResumenDto
            {
                TotalVendido = totalVendido,
                TotalComprado = totalComprado,
                MargenBrutoEstimado = totalVendido - totalComprado,
                TicketPromedio = ticketPromedio,
                CantidadVentas = cantidadVentas,
                CrecimientoPorcentualVsPeriodoAnterior = crecimiento
            },
            Canal = new CanalResumenDto
            {
                VentasWeb = ventasWeb,
                VentasPresencial = ventasPresencial,
                PorcentajeWeb = porcentajeWeb,
                PorcentajePresencial = porcentajePresencial
            },
            Caja = new CajaResumenDto
            {
                Ingresos = ingresos,
                Egresos = egresos,
                TotalCompras = totalCompras,
                TotalNotasCredito = totalNotasCredito,
                CantidadNotasCredito = cantidadNotasCredito,
                SaldoNeto = ingresos - egresos,
                MedioPagoPredominante = medioPredominante
            },
            Inventario = new InventarioResumenDto
            {
                StockMinimoConfigurado = stockMinimo,
                ValorTotalInventario = valorTotalInventario,
                ProductosStockBajo = productosStockBajo,
                ProductosSinRotacion60Dias = sinRotacionCount,
                TopProductosVendidos = topProductos,
                ProductosSinRotacionDetalle = sinRotacion
            },
            Proveedores = new ProveedoresResumenDto
            {
                TotalInvertido = totalInvertido,
                FrecuenciaCompras = frecuenciaCompras,
                ProveedorMayorVolumen = rankingProveedores.FirstOrDefault(),
                Ranking = rankingProveedores
            }
        };
    }

    private static string MedioPagoLabel(MedioPago medioPago)
    {
        return medioPago switch
        {
            MedioPago.Efectivo => "Efectivo",
            MedioPago.Debito => "Debito",
            MedioPago.Credito => "Credito",
            MedioPago.Transferencia => "Transferencia",
            MedioPago.NotaCredito => "Nota de credito",
            _ => $"Medio {(int)medioPago}"
        };
    }

    private static bool EsMovimientoCompraInformativa(string? concepto)
    {
        if (string.IsNullOrWhiteSpace(concepto)) return false;
        return concepto.StartsWith(CompraProveedorConceptPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
