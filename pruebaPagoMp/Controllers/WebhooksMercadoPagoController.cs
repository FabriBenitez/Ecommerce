using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Services.Pagos;
using pruebaPagoMp.Services.Ventas;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/webhooks/mercadopago")]
public class WebhooksMercadoPagoController : ControllerBase
{
    private readonly IMercadoPagoService _mp;
    private readonly IVentasService _ventas;

    public WebhooksMercadoPagoController(IMercadoPagoService mp, IVentasService ventas)
    {
        _mp = mp;
        _ventas = ventas;
    }

    // MercadoPago suele mandar query: ?type=payment&data.id=123
    [HttpPost]
    public async Task<IActionResult> Recibir([FromQuery(Name = "data.id")] string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return Ok(); // idempotente: ignoramos

        var info = await _mp.ObtenerPagoAsync(paymentId);
        if (info?.ExternalReference == null)
            return Ok();

        if (!int.TryParse(info.ExternalReference, out var ventaId))
            return Ok();

        await _ventas.ConfirmarPagoVentaAsync(
            ventaId,
            referenciaExternaPago: paymentId,
            metodoPago: info.PaymentMethodId,
            estadoPago: info.Status ?? "unknown"
        );

        return Ok();
    }
}
