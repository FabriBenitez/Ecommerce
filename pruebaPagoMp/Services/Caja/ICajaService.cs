using pruebaPagoMp.Dtos.Caja;

namespace pruebaPagoMp.Services.Caja;

public interface ICajaService
{
    Task<CajaResumenDto> AbrirAsync(AbrirCajaDto dto);
    Task<CajaResumenDto> CerrarAsync(CerrarCajaDto dto);
    Task<CajaResumenDto> ObtenerResumenAsync();
    Task<List<MovimientoCajaDto>> ListarMovimientosAsync(DateTime? desde, DateTime? hasta);
}
