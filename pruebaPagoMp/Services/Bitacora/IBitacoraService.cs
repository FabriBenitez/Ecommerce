using pruebaPagoMp.Models.Bitacora;

namespace pruebaPagoMp.Services.Bitacora;

public interface IBitacoraService
{
    Task RegistrarAsync(BitacoraEntry entry);
}
