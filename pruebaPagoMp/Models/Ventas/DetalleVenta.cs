namespace pruebaPagoMp.Models.Ventas;

public class DetalleVenta
{
    public int Id { get; set; }

    public int VentaId { get; set; }
    public Venta Venta { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public int Cantidad { get; set; }

    // Snapshot del precio al momento de la venta (clave para inmutabilidad)
    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal { get; set; }
}
