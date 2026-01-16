using pruebaPagoMp.DTOs;

namespace pruebaPagoMp.Services
{
    public interface IPedidoService
    {
        // Ahora recibe la lista de items y el usuarioId (opcional)
        Task<PagoRespuestaDto> CrearPedidoYPreferenciaAsync(List<PedidoItemDto> items, int? usuarioId);
    }
}