namespace pruebaPagoMp.Dtos.Ventas;

public class CheckoutVentaWebDto
{
    public string ClienteDni { get; set; } = null!;
    public string NombreEntrega { get; set; } = null!;
    public string TelefonoEntrega { get; set; } = null!;
    public string DireccionEntrega { get; set; } = null!;
    public string Ciudad { get; set; } = null!;
    public string Provincia { get; set; } = null!;
    public string CodigoPostal { get; set; } = null!;
    public string? Observaciones { get; set; } = null;

}
