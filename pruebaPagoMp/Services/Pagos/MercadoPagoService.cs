using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace pruebaPagoMp.Services.Pagos;

public class MercadoPagoService : IMercadoPagoService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    public MercadoPagoService(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _http = httpClientFactory.CreateClient();
    }

    public async Task<MercadoPagoPagoInfo?> ObtenerPagoAsync(string paymentId)
    {
        var token = _config["MercadoPago:AccessToken"];
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Falta MercadoPago:AccessToken en appsettings.json");

        var url = $"https://api.mercadopago.com/v1/payments/{paymentId}";

        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new MercadoPagoPagoInfo
        {
            Status = root.TryGetProperty("status", out var status) ? status.GetString() : null,
            ExternalReference = root.TryGetProperty("external_reference", out var ext) ? ext.GetString() : null,
            PaymentMethodId = root.TryGetProperty("payment_method_id", out var pm) ? pm.GetString() : null
        };
    }

    public async Task<(string preferenceId, string urlPago)> CrearPreferenciaPagoAsync(
        int ventaId, decimal total, string descripcion, string moneda = "ARS"
    )
    {
        var publicBaseUrl = _config["PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(publicBaseUrl))
            throw new InvalidOperationException("Falta PublicBaseUrl en appsettings.Development.json");

        var notificationUrl = $"{publicBaseUrl}/api/webhooks/mercadopago";
        Console.WriteLine($"NotificationUrl = {notificationUrl}");

        // Mock por ahora
        await Task.CompletedTask;

        var preferenceId = $"pref_{ventaId}_{Guid.NewGuid():N}";

        var urlPago = $"https://www.mercadopago.com.ar/checkout/v1/redirect?pref_id={preferenceId}";

        return (preferenceId, urlPago);
    }


    /*public async Task<(string preferenceId, string urlPago)> CrearPreferenciaPagoAsync(
        int ventaId,
        decimal total,
        string descripcion,
        string moneda = "ARS"
    )
    {
        // ⚠️ por ahora lo dejamos como mock si no querés integrar preferencia todavía
        await Task.CompletedTask;

        var preferenceId = $"pref_{ventaId}_{Guid.NewGuid():N}";

        var urlPago = $"https://www.mercadopago.com.ar/checkout/v1/redirect?pref_id={preferenceId}";

        var publicBaseUrl = _config["PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(publicBaseUrl))
            throw new InvalidOperationException("Falta PublicBaseUrl en appsettings.Development.json");

        Console.WriteLine($"PublicBaseUrl = {publicBaseUrl}");

        var notificationUrl = $"{publicBaseUrl}/api/webhooks/mercadopago";


        return (preferenceId, urlPago);
    }*/
}
