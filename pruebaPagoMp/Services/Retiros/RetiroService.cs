using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.Dtos.Retiros;
using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Services.Retiros;

public class RetirosService : IRetirosService
{
    private readonly ApplicationDbContext _context;

    public RetirosService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RetirosListItemDto>> ListarAsync(EstadoRetiro? estado)
    {
        var q = _context.Ventas
            .AsNoTracking()
            .Where(v => v.Canal == CanalVenta.Web)
            .Where(v => v.EstadoVenta == EstadoVenta.Pagada);

        if (estado.HasValue)
            q = q.Where(v => v.EstadoRetiro == estado.Value);

        return await q
            .OrderByDescending(v => v.Fecha)
            .Select(v => new RetirosListItemDto
            {
                VentaId = v.Id,
                Fecha = v.Fecha,
                Total = v.Total,
                Canal = v.Canal,
                EstadoVenta = v.EstadoVenta,
                EstadoRetiro = v.EstadoRetiro,
                ClienteNombre = v.ClienteNombre,
                ClienteDni = v.ClienteDni
            })
            .ToListAsync();
    }

    public async Task<bool> CambiarEstadoAsync(int ventaId, EstadoRetiro nuevoEstado)
    {
        var venta = await _context.Ventas.FirstOrDefaultAsync(v => v.Id == ventaId);
        if (venta == null) return false;

        // Reglas: solo ventas web pagadas
        if (venta.Canal != CanalVenta.Web)
            throw new InvalidOperationException("Solo aplica a ventas Web.");

        if (venta.EstadoVenta != EstadoVenta.Pagada)
            throw new InvalidOperationException("Solo se puede gestionar retiro si la venta está Pagada.");

        // Validar transición (flujo secuencial)
        if (!EsTransicionValida(venta.EstadoRetiro, nuevoEstado))
            throw new InvalidOperationException($"Transición inválida: {venta.EstadoRetiro} -> {nuevoEstado}");

        venta.EstadoRetiro = nuevoEstado;

        // Log simple en Observaciones (opcional)
        var msg = $"Retiro: {nuevoEstado} ({DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC)";
        venta.Observaciones = string.IsNullOrWhiteSpace(venta.Observaciones)
            ? msg
            : $"{venta.Observaciones} | {msg}";

        await _context.SaveChangesAsync();
        return true;
    }

    private static bool EsTransicionValida(EstadoRetiro actual, EstadoRetiro nuevo)
    {
        // Secuencial: Pendiente -> Preparando -> ListoParaRetirar -> Entregado
        // Permitimos "mismo estado" por idempotencia
        if (actual == nuevo) return true;

        return (actual, nuevo) switch
        {
            (EstadoRetiro.Pendiente, EstadoRetiro.Preparando) => true,
            (EstadoRetiro.Preparando, EstadoRetiro.ListoParaRetirar) => true,
            (EstadoRetiro.ListoParaRetirar, EstadoRetiro.Entregado) => true,
            _ => false
        };
    }
}
