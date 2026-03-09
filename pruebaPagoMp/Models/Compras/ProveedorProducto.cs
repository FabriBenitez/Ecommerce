namespace pruebaPagoMp.Models.Compras;

public class ProveedorProducto
{
    public int Id { get; set; }

    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public decimal CostoUnitario { get; set; }
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
}
