using pruebaPagoMp.Dtos.Retiros;
using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Services.Retiros;

public interface IRetirosService
{
    Task<List<RetirosListItemDto>> ListarAsync(EstadoRetiro? estado);
    Task<bool> CambiarEstadoAsync(int ventaId, EstadoRetiro nuevoEstado);
}
