using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Dtos.Retiros;

public class RetirosListItemDto
{
    public int VentaId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public CanalVenta Canal { get; set; }
    public EstadoVenta EstadoVenta { get; set; }
    public EstadoRetiro EstadoRetiro { get; set; }

    public string? ClienteNombre { get; set; }
    public string? ClienteDni { get; set; }
}
