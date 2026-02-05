namespace pruebaPagoMp.Services.Pagos;

public interface IMercadoPagoService
{
    Task<MercadoPagoPagoInfo?> ObtenerPagoAsync(string paymentId);

    Task<(string preferenceId, string urlPago)> CrearPreferenciaPagoAsync(
        int ventaId,
        decimal total,
        string descripcion,
        string moneda = "ARS");
}

public class MercadoPagoPagoInfo
{
    public string? Status { get; set; }
    public string? ExternalReference { get; set; }
    public string? PaymentMethodId { get; set; }
}