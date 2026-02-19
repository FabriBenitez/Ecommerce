using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Models.Ventas;

public class VentaPago
{
    public int Id { get; set; }

    public int VentaId { get; set; }
    public Venta Venta { get; set; } = null!;

    public MedioPago MedioPago { get; set; }
    public decimal Monto { get; set; }

    // opcional (ej: nro transferencia, cupón, etc.)
    public string? Referencia { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
