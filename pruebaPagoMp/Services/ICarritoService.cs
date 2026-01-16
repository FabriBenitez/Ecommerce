using pruebaPagoMp.DTOs;

namespace pruebaPagoMp.Services
{
    public interface ICarritoService
    {
        Task<bool> AgregarProductoAsync(AgregarItemDto itemDto);
        // Más adelante agregaremos: ObtenerCarrito, EliminarItem, etc.
    }
}