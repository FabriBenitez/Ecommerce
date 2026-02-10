using System.Net.Http.Headers;
using System.Text;
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
        var token = _config["MercadoPago:AccessToken"];
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Falta MercadoPago:AccessToken en appsettings.json");

        var publicBaseUrl = _config["MercadoPago:PublicBaseUrl"] ?? _config["PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(publicBaseUrl))
            throw new InvalidOperationException("Falta PublicBaseUrl o MercadoPago:PublicBaseUrl en appsettings.json");

        publicBaseUrl = publicBaseUrl.TrimEnd('/');

        var requestBody = new
        {
            items = new[]
            {
                new
                {
                    title = descripcion,
                    quantity = 1,
                    unit_price = decimal.Round(total, 2),
                    currency_id = moneda
                }
            },
            external_reference = ventaId.ToString(),
            notification_url = $"{publicBaseUrl}/api/webhooks/mercadopago",
            back_urls = new
            {
                success = publicBaseUrl,
                failure = publicBaseUrl,
                pending = publicBaseUrl
            },
            auto_return = "approved"
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.mercadopago.com/checkout/preferences")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await _http.SendAsync(req);
        var responseJson = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Mercado Pago devolvio error al crear preferencia. Status={(int)resp.StatusCode} Body={responseJson}");
        }

        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        var preferenceId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        var initPoint = root.TryGetProperty("init_point", out var initProp) ? initProp.GetString() : null;

        if (string.IsNullOrWhiteSpace(preferenceId) || string.IsNullOrWhiteSpace(initPoint))
            throw new InvalidOperationException("Respuesta de Mercado Pago invalida: no contiene id/init_point.");

        return (preferenceId, initPoint);
    }
}
