namespace pruebaPagoMp.Dtos.Ventas;

public class CheckoutVentaWebRespuestaDto
{
    public int VentaId { get; set; }
    public string PreferenceId { get; set; } = null!;
    public string UrlPago { get; set; } = null!;
    public decimal Total { get; set; }
}
