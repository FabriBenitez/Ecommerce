using Microsoft.Extensions.Configuration;
using pruebaPagoMp.Services.Pagos;
using MercadoPago.Config;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;

namespace pruebaPagoMp.Services.Pagos
{
    public class MercadoPagoService : IMercadoPagoService
    {
        private readonly IConfiguration _config;

        public MercadoPagoService(IConfiguration config)
        {
            _config = config;

            // Config global del SDK
            MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
        }

        public async Task<(string preferenceId, string urlPago)> CrearPreferenciaPagoAsync(
            int ventaId,
            decimal total,
            string descripcion,
            string moneda = "ARS")
        {
            // URL pública del webhook (debe ser accesible desde internet)
            var publicBaseUrl = _config["MercadoPago:PublicBaseUrl"]?.TrimEnd('/');
            var webhookPath = _config["MercadoPago:WebhookPath"] ?? "/api/webhooks/mercadopago";
            var notificationUrl = $"{publicBaseUrl}{webhookPath}";

            var request = new PreferenceRequest
            {
                ExternalReference = ventaId.ToString(), // ✅ CLAVE: vincula pago ↔ venta
                NotificationUrl = notificationUrl,
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = descripcion,
                        Quantity = 1,
                        CurrencyId = moneda,
                        UnitPrice = total
                    }
                }
            };

            var client = new PreferenceClient();
            Preference preference = await client.CreateAsync(request);

            // ✅ init_point es la URL real de pago
            return (preference.Id, preference.InitPoint);
        }

        public async Task<MercadoPagoPagoInfo?> ObtenerPagoAsync(string paymentId)
        {
            var client = new PaymentClient();
            var payment = await client.GetAsync(long.Parse(paymentId));

            return new MercadoPagoPagoInfo
            {
                Status = payment.Status,
                ExternalReference = payment.ExternalReference,
                PaymentMethodId = payment.PaymentMethodId
            };
        }
    }
}
