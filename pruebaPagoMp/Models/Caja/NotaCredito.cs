namespace pruebaPagoMp.Models.Caja;

public class NotaCredito
{
    public int Id { get; set; }
    public string ClienteDni { get; set; } = null!;
    public decimal SaldoDisponible { get; set; }
    public DateTime FechaCreacion { get; set; }
}
