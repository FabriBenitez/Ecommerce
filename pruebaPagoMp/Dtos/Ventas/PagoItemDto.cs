using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Dtos.Ventas;

public class PagoItemDto
{
    public MedioPago MedioPago { get; set; }
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
}
