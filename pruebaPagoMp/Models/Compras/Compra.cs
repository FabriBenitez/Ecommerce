using pruebaPagoMp.Models.Compras.Enums;

namespace pruebaPagoMp.Models.Compras;

public class Compra
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;

    public decimal Total { get; set; }
    public EstadoCompra EstadoCompra { get; set; } = EstadoCompra.Pendiente;

    public ICollection<DetalleCompra> Detalles { get; set; } = new List<DetalleCompra>();

    public FacturaProveedor? FacturaProveedor { get; set; }
    public string DigitoVerificador { get; set; } = "";
}
