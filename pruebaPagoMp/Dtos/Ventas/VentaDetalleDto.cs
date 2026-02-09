namespace pruebaPagoMp.Dtos.Ventas;

public class VentaDetalleDto
{
    public int ProductoId { get; set; }
    public string NombreProducto { get; set; } = null!;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
