using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Models.Ventas;

public class Reserva
{
    public int Id { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaVencimiento { get; set; }

    public int UsuarioAdminId { get; set; }
    public Usuario UsuarioAdmin { get; set; } = null!;

    public string? ClienteDni { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteTelefono { get; set; }

    public decimal Total { get; set; }
    public decimal MontoSenia { get; set; }
    public decimal SaldoPendiente { get; set; }
    public bool RequiereCompra { get; set; }
    public EstadoReserva Estado { get; set; } = EstadoReserva.PendienteAnticipo;
    public string? Observaciones { get; set; }

    public int? VentaId { get; set; }
    public Venta? Venta { get; set; }

    public ICollection<ReservaItem> Items { get; set; } = new List<ReservaItem>();
    public ICollection<ReservaPago> Pagos { get; set; } = new List<ReservaPago>();
}
