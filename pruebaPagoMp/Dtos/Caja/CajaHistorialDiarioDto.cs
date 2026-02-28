namespace pruebaPagoMp.Dtos.Caja;

public class CajaHistorialDiarioDto
{
    public int CajaId { get; set; }
    public DateTime Fecha { get; set; }
    public DateTime FechaApertura { get; set; }
    public DateTime? FechaCierre { get; set; }
    public bool Abierta { get; set; }
    public decimal SaldoInicial { get; set; }
    public decimal Ingresos { get; set; }
    public decimal Egresos { get; set; }
    public decimal SaldoEsperado { get; set; }
    public decimal? SaldoFinal { get; set; }
    public decimal? DiferenciaCierre { get; set; }
    public int CantidadMovimientos { get; set; }
}
