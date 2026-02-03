using pruebaPagoMp.DTOs;

namespace pruebaPagoMp.Services
{
    public interface IProductoService
    {
        // Este método devolverá la lista de libros mapeada a DTO
        Task<IEnumerable<ProductoDto>> GetCatalogoAsync();
    }
}