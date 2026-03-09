using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.Dtos.Compras;
using pruebaPagoMp.Models.Bitacora;
using pruebaPagoMp.Services.Bitacora;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/pedidos-senia")]
[Authorize(Roles = "AdminCompras,AdminGeneral")]
public class PedidosSeniaController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IBitacoraService _bitacoraService;

    public PedidosSeniaController(ApplicationDbContext context, IBitacoraService bitacoraService)
    {
        _context = context;
        _bitacoraService = bitacoraService;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var items = await _context.PedidosSenia
            .AsNoTracking()
            .Include(x => x.Reserva)
                .ThenInclude(r => r.Items)
                    .ThenInclude(i => i.Producto)
            .OrderByDescending(x => x.FechaCreacion)
            .ToListAsync();

        var data = items.Select(x => new PedidoSeniaListDto
            {
                Id = x.Id,
                ReservaId = x.ReservaId,
                FechaCreacion = x.FechaCreacion,
                Estado = x.Estado,
                EstadoReserva = x.Reserva.Estado,
                ClienteDni = x.ClienteDni,
                ClienteNombre = x.ClienteNombre,
                ClienteTelefono = x.ClienteTelefono,
                Total = x.Reserva.Total,
                MontoSenia = x.Reserva.MontoSenia,
                SaldoPendiente = x.Reserva.SaldoPendiente,
                CantidadItems = x.Reserva.Items.Sum(i => i.Cantidad),
                ResumenItems = string.Join(" | ", x.Reserva.Items.Select(i => $"{(i.Producto?.Nombre ?? "Item")} x{i.Cantidad}"))
            })
            .ToList();

        return Ok(data);
    }

    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> ActualizarEstado([FromRoute] int id, [FromBody] ActualizarEstadoPedidoSeniaDto dto)
    {
        var item = await _context.PedidosSenia.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound(new { error = "Pedido de seña inexistente." });

        item.Estado = dto.Estado;
        item.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            Accion = "PEDIDO_SENIA_ESTADO",
            Detalle = $"PedidoSeña #{item.Id} -> {item.Estado}",
            Resultado = "OK",
            Ip = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        return NoContent();
    }
}
