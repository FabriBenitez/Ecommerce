namespace pruebaPagoMp.Dtos.Ventas;

public class DevolucionPresencialDto
{
    public string ClienteDni { get; set; } = null!;
    public decimal Monto { get; set; }
    public string? Motivo { get; set; }
}
