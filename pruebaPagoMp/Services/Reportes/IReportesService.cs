using pruebaPagoMp.Dtos.Reportes;

namespace pruebaPagoMp.Services.Reportes;

public interface IReportesService
{
    Task<ReporteVentasDto> ObtenerVentasAsync(DateTime desde, DateTime hasta);
    Task<List<ReporteStockDto>> ObtenerStockAsync();
    Task<ReporteCajaDto> ObtenerCajaAsync(DateTime fecha);
}
