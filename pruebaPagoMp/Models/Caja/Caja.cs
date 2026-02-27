namespace pruebaPagoMp.Models.Caja;

public class Caja
{
    public int Id { get; set; }
    public DateTime FechaApertura { get; set; } = DateTime.UtcNow;
    public DateTime? FechaCierre { get; set; }
    public decimal SaldoInicial { get; set; }
    public decimal? SaldoFinal { get; set; }
    public bool Abierta { get; set; } = true;
    public string? Observaciones { get; set; }
}
