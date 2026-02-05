using pruebaPagoMp.Dtos.Carritos;

namespace pruebaPagoMp.Services.Carritos;

public interface ICarritoService
{
    Task<CarritoDto> ObtenerCarritoActualAsync(int usuarioId);
    Task<CarritoDto> AgregarCarritoItemAsync(int usuarioId, AgregarCarritoItemDto dto);
    Task<CarritoDto> ActualizarCantidadCarritoItemAsync(int usuarioId, int itemId, int cantidad);
    Task<CarritoDto> EliminarCarritoItemAsync(int usuarioId, int itemId);
}
