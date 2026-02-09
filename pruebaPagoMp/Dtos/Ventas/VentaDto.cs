using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Dtos.Ventas;

public class VentaDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public EstadoVenta EstadoVenta { get; set; }
    public CanalVenta Canal { get; set; }
    
    public string? NombreEntrega { get; set; }
    public string? TelefonoEntrega { get; set; }
    public string? DireccionEntrega { get; set; }
    public string? Ciudad { get; set; }
    public string? Provincia { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Observaciones { get; set; }

    public List<VentaDetalleDto> Detalles { get; set; } = new();
}
