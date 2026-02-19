using pruebaPagoMp.Models.Caja.Enums;
using pruebaPagoMp.Models.Ventas;
using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Models.Caja;

public class MovimientoCaja
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public TipoMovimientoCaja Tipo { get; set; }
    public decimal Monto { get; set; }

    public string Concepto { get; set; } = "Venta Presencial";

    // link opcional a la venta
    public int? VentaId { get; set; }
    public Venta? Venta { get; set; }

    public MedioPago MedioPago { get; set; }
    public string? Referencia { get; set; }
}
