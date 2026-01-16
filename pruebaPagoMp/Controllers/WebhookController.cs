using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Data;
using Microsoft.EntityFrameworkCore;

namespace pruebaPagoMp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WebhookController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("mercadopago")]
        public async Task<IActionResult> RecibirNotificacion([FromQuery] string type, [FromQuery] string id)
        {
            // Mercado Pago envía notificaciones de tipo "payment" o "plan", etc.
            if (type == "payment")
            {
                // Aquí es donde consultamos el estado del pago usando el ID que nos mandan
                // Por ahora, solo imprimiremos en consola para verificar que llega
                Console.WriteLine($"Notificación recibida: Pago ID {id}");
                
                // El próximo paso será buscar ese pago en MP y actualizar tu base de datos
            }

            return Ok(); // Siempre debemos responder 200 OK a Mercado Pago
        }
    }
}