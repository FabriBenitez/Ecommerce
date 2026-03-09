using System.ComponentModel.DataAnnotations;
using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Dtos.Ventas;

public class CrearReservaPresencialDto
{
    [Required(ErrorMessage = "El DNI es obligatorio.")]
    [StringLength(20, ErrorMessage = "El DNI no puede superar 20 caracteres.")]
    public string? ClienteDni { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(200, ErrorMessage = "El nombre no puede superar 200 caracteres.")]
    public string? ClienteNombre { get; set; }

    [Required(ErrorMessage = "El telefono es obligatorio.")]
    [StringLength(50, ErrorMessage = "El telefono no puede superar 50 caracteres.")]
    public string? ClienteTelefono { get; set; }
    public string? Observaciones { get; set; }
    public List<CrearReservaPresencialItemDto> Items { get; set; } = new();
    public List<PagoItemDto> PagosAnticipo { get; set; } = new();
}

public class CrearReservaPresencialItemDto
{
    public int? ProductoId { get; set; }
    public string? NombreManual { get; set; }
    public decimal? PrecioUnitarioManual { get; set; }
    public int Cantidad { get; set; }
    public bool EsAConseguir { get; set; }
}

public class CompletarReservaPagoDto
{
    public List<PagoItemDto> Pagos { get; set; } = new();
    public string? Observaciones { get; set; }
}

public class ReservaListItemDto
{
    public int Id { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string? ClienteDni { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteTelefono { get; set; }
    public decimal Total { get; set; }
    public decimal MontoSenia { get; set; }
    public decimal SaldoPendiente { get; set; }
    public bool RequiereCompra { get; set; }
    public EstadoReserva Estado { get; set; }
    public int CantidadItems { get; set; }
    public int? VentaId { get; set; }
    public int? PedidoSeniaId { get; set; }
    public int? EstadoPedidoSenia { get; set; }
}

public class ReservaDetalleDto
{
    public int Id { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string? ClienteDni { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteTelefono { get; set; }
    public decimal Total { get; set; }
    public decimal MontoSenia { get; set; }
    public decimal SaldoPendiente { get; set; }
    public bool RequiereCompra { get; set; }
    public EstadoReserva Estado { get; set; }
    public string? Observaciones { get; set; }
    public int? VentaId { get; set; }
    public int? PedidoSeniaId { get; set; }
    public int? EstadoPedidoSenia { get; set; }
    public List<ReservaDetalleItemDto> Items { get; set; } = new();
    public List<ReservaDetallePagoDto> Pagos { get; set; } = new();
}

public class ReservaDetalleItemDto
{
    public int ProductoId { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public bool EsAConseguir { get; set; }
}

public class ReservaDetallePagoDto
{
    public MedioPago MedioPago { get; set; }
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
    public DateTime Fecha { get; set; }
}

public class ReservaLibroResumenDto
{
    public string NombreLibro { get; set; } = string.Empty;
    public int CantidadTotal { get; set; }
    public int CantidadReservas { get; set; }
    public int CantidadConStock { get; set; }
    public int CantidadAConseguir { get; set; }
}
