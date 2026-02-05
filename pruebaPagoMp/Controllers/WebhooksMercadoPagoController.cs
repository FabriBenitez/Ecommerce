using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using pruebaPagoMp.Services.Pagos;
using pruebaPagoMp.Services.Ventas;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/webhooks/mercadopago")]
public class WebhooksMercadoPagoController : ControllerBase
{
    private readonly IMercadoPagoService _mercadoPagoService;
    private readonly IVentasService _ventasService;

    public WebhooksMercadoPagoController(IMercadoPagoService mercadoPagoService, IVentasService ventasService)
    {
        _mercadoPagoService = mercadoPagoService;
        _ventasService = ventasService;
    }

    [HttpPost]
    public async Task<IActionResult> Recibir([FromBody] JsonElement payload)
    {
        // 1) Extraer paymentId del payload (esto depende del formato exacto que te llegue)
        // EJ: si viene payload.data.id (común en notificaciones)
        if (!payload.TryGetProperty("data", out var data) || !data.TryGetProperty("id", out var idNode))
            return Ok(); // devolvemos 200 para que MP no reintente, pero no hacemos nada

        var paymentId = idNode.GetString();
        if (string.IsNullOrWhiteSpace(paymentId))
            return Ok();

        // 2) Consultar a MercadoPago para validar estado real
        var pagoInfo = await _mercadoPagoService.ObtenerPagoAsync(paymentId);
        if (pagoInfo == null)
            return Ok();

        // 3) Tu "ventaId" debería venir como external_reference (lo seteás cuando creás la preferencia)
        // EJ: external_reference = venta.Id
        if (string.IsNullOrWhiteSpace(pagoInfo.ExternalReference))
            return Ok();

        if (!int.TryParse(pagoInfo.ExternalReference, out var ventaId))
            return Ok();

        // 4) Si el pago está approved, confirmás y listo.
        if (pagoInfo.Status == "approved")
        {
            await _ventasService.ConfirmarPagoVentaAsync(
                ventaId,
                paymentId,
                pagoInfo.PaymentMethodId ?? "mercadopago",
                pagoInfo.Status
            );
        }

        return Ok();
    }
}
