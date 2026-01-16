using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Services;
using pruebaPagoMp.Data;
using pruebaPagoMp.Models;
using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.DTOs;

namespace pruebaPagoMp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PedidosController : ControllerBase
    {
        private readonly IPedidoService _pedidoService;
        private readonly ApplicationDbContext _context;
        public PedidosController(IPedidoService pedidoService, ApplicationDbContext context)
        {
            _pedidoService = pedidoService;
            _context = context;
        }

        [HttpPost("crear-pago")]
        public async Task<IActionResult> CrearPago([FromBody] List<PedidoItemDto> items, [FromQuery] int? usuarioId)
        {
            try
            {
                var resultado = await _pedidoService.CrearPedidoYPreferenciaAsync(items, usuarioId);
                return Ok(new { initPoint = resultado.UrlPago });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPedido(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.PedidoItems)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            return Ok(pedido);
        }
    }
}