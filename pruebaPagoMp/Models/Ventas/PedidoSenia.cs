using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Models.Ventas;

public class PedidoSenia
{
    public int Id { get; set; }
    public int ReservaId { get; set; }
    public Reserva Reserva { get; set; } = null!;

    public string? ClienteDni { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteTelefono { get; set; }

    public EstadoPedidoSenia Estado { get; set; } = EstadoPedidoSenia.PendienteCompra;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
}
