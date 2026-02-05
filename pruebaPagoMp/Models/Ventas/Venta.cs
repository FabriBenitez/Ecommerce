using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Models.Ventas;

public class Venta
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    // FK Usuario (quien compra en web / quien registra si presencial, según tu criterio)
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    // Total calculado al confirmar (sumatoria de Detalles)
    public decimal Total { get; set; }

    public EstadoVenta EstadoVenta { get; set; } = EstadoVenta.Pendiente;

    public CanalVenta Canal { get; set; }

    // Navegación
    public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();

    // Recomendado para fase 2/3 (no es obligatorio pero ayuda)
    public string? Observaciones { get; set; } // ej: "Venta presencial - efectivo"
}

