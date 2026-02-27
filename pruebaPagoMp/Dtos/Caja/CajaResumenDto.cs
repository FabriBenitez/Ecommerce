namespace pruebaPagoMp.Dtos.Caja;

public class CajaResumenDto
{
    public int? CajaId { get; set; }
    public bool Abierta { get; set; }
    public decimal SaldoInicial { get; set; }
    public decimal Ingresos { get; set; }
    public decimal Egresos { get; set; }
    public decimal SaldoActual { get; set; }
    public DateTime? FechaApertura { get; set; }
    public DateTime? FechaCierre { get; set; }
}
