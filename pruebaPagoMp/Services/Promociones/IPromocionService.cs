using pruebaPagoMp.Dtos.Promociones;

namespace pruebaPagoMp.Services.Promociones;

public interface IPromocionService
{
    Task<int> CrearAsync(PromocionCreateDto dto);
    Task<List<PromocionListDto>> ListarAsync(bool? activas);
}
