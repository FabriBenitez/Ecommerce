using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Models.Ventas;

public class HistorialPresencialItemDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string? ClienteDni { get; set; }
    public string? ClienteNombre { get; set; }
    public decimal Total { get; set; }
    public EstadoVenta EstadoVenta { get; set; }
    public CanalVenta Canal { get; set; }
}
