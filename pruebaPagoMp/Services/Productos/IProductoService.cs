using pruebaPagoMp.DTOs;

namespace pruebaPagoMp.Services
{
    public interface IProductoService
    {
        Task<IEnumerable<ProductoDto>> GetCatalogoAsync();
        Task<int> CrearAsync(CrearProductoDto dto);
        Task<int> ImportarArchivoAsync(Stream stream, string fileName);
    }
}
