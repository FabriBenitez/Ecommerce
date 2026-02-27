namespace pruebaPagoMp.Dtos.Reportes;

public class ReporteCajaDto
{
    public DateTime Fecha { get; set; }
    public decimal Ingresos { get; set; }
    public decimal Egresos { get; set; }
    public decimal SaldoNeto { get; set; }
}
