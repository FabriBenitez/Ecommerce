using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Models.Ventas;

public class ReservaPago
{
    public int Id { get; set; }

    public int ReservaId { get; set; }
    public Reserva Reserva { get; set; } = null!;

    public MedioPago MedioPago { get; set; }
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
