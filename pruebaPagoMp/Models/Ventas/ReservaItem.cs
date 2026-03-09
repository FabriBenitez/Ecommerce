namespace pruebaPagoMp.Models.Ventas;

public class ReservaItem
{
    public int Id { get; set; }

    public int ReservaId { get; set; }
    public Reserva Reserva { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public bool EsAConseguir { get; set; }
}
