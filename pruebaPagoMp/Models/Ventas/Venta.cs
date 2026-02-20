using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Models.Ventas;

public class Venta
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public decimal Total { get; set; }
    public EstadoVenta EstadoVenta { get; set; } = EstadoVenta.Pendiente;
    public CanalVenta Canal { get; set; }
    public string? ClienteDni { get; set; }
    public string? ClienteNombre { get; set; }

    public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();

    // ✅ NUEVO: datos de entrega separados
    public string? NombreEntrega { get; set; }
    public string? TelefonoEntrega { get; set; }
    public string? DireccionEntrega { get; set; }
    public string? Ciudad { get; set; }
    public string? Provincia { get; set; }
    public string? CodigoPostal { get; set; }

    public string? Observaciones { get; set; }

    public string? MercadoPagoPreferenceId { get; set; }
    public string? MercadoPagoUrlPago { get; set; }
    public string? MercadoPagoPaymentId { get; set; }
    public string? MercadoPagoEstado { get; set; }

    public EstadoRetiro EstadoRetiro { get; set; } = EstadoRetiro.Pendiente;
    public ICollection<VentaPago> Pagos { get; set; } = new List<VentaPago>();
}

