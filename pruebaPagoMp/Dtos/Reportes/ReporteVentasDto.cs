namespace pruebaPagoMp.Dtos.Reportes;

public class ReporteVentasDto
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public int CantidadVentas { get; set; }
    public decimal TotalVendido { get; set; }
}
