using pruebaPagoMp.Dtos.Ventas;
using pruebaPagoMp.Models.Ventas;
using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Services.Ventas;

public interface IVentasService
{
    Task<CheckoutVentaWebRespuestaDto> CheckoutVentaWebAsync(int usuarioId, CheckoutVentaWebDto dto);
    Task<int> ConfirmarPagoVentaAsync(int ventaId, string? referenciaExternaPago, string? metodoPago, string estadoPago);
    Task<int> CrearVentaPresencialAsync(int adminUsuarioId, CrearVentaPresencialDto dto);

    Task<VentaDto?> ObtenerVentaPorIdAsync(int ventaId, int usuarioId, bool esAdminVentas);
    Task<List<MisVentasItemDto>> ObtenerMisVentasAsync(int usuarioId);
    Task<List<HistorialPresencialItemDto>> ObtenerHistorialPresencialAsync(
        DateTime? desde,
        DateTime? hasta,
        string? dni,
        EstadoVenta? estado);

    Task<int> RegistrarDevolucionPresencialAsync(int ventaId, DevolucionPresencialDto dto);
    Task<int> CrearReservaPresencialAsync(int adminUsuarioId, CrearReservaPresencialDto dto);
    Task<int> CompletarPagoReservaAsync(int reservaId, CompletarReservaPagoDto dto);
    Task CancelarReservaAsync(int reservaId, string? motivo);
    Task<int> VencerReservasAsync(DateTime? fechaCorte = null);
    Task<List<ReservaListItemDto>> ObtenerReservasAsync(EstadoReserva? estado, DateTime? desde, DateTime? hasta, string? dni);
    Task<List<ReservaLibroResumenDto>> ObtenerResumenLibrosSeniaAsync(bool incluirCerradas = false);
    Task<ReservaDetalleDto?> ObtenerReservaPorIdAsync(int reservaId);
}
