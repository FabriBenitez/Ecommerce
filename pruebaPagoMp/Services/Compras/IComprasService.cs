using pruebaPagoMp.Dtos.Compras;

namespace pruebaPagoMp.Services.Compras;

public interface IComprasService
{
    // Proveedores
    Task<int> CrearProveedorAsync(ProveedorCreateDto dto);
    Task<List<ProveedorListDto>> ListarProveedoresAsync(bool? activos);
    Task<ProveedorListDto?> ObtenerProveedorPorIdAsync(int id);
    Task ActualizarProveedorAsync(int id, ProveedorCreateDto dto);

    // Compras
    Task<int> CrearCompraAsync(CrearCompraDto dto);
    Task ConfirmarCompraAsync(int compraId);
    Task<List<CompraListDto>> ListarComprasAsync(int? proveedorId);
    Task<object?> ObtenerCompraDetalleAsync(int compraId); // simple (MVP)
    Task RegistrarFacturaProveedorAsync(int compraId, RegistrarFacturaProveedorDto dto);
}
