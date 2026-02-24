namespace pruebaPagoMp.Models.Compras;

public class FacturaProveedor
{
    public int Id { get; set; }

    public int CompraId { get; set; }
    public Compra Compra { get; set; } = null!;

    public string Numero { get; set; } = null!;
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
}