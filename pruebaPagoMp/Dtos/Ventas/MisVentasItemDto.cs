using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Dtos.Ventas;

public class MisVentasItemDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public EstadoVenta EstadoVenta { get; set; }
    public CanalVenta Canal { get; set; }
    public EstadoRetiro EstadoRetiro { get; set; }
}
