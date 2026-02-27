using pruebaPagoMp.Models.Caja.Enums;
using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Dtos.Caja;

public class MovimientoCajaDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public TipoMovimientoCaja Tipo { get; set; }
    public decimal Monto { get; set; }
    public string Concepto { get; set; } = null!;
    public int? VentaId { get; set; }
    public MedioPago MedioPago { get; set; }
    public string? Referencia { get; set; }
}
