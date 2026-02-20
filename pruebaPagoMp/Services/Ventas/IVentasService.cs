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
}
