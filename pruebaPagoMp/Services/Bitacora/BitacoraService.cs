using pruebaPagoMp.Data;
using pruebaPagoMp.Models.Bitacora;

namespace pruebaPagoMp.Services.Bitacora;

public class BitacoraService : IBitacoraService
{
    private readonly ApplicationDbContext _context;

    public BitacoraService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task RegistrarAsync(BitacoraEntry entry)
    {
        _context.BitacoraEntries.Add(entry);
        await _context.SaveChangesAsync();
    }
}
