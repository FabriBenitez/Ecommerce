namespace pruebaPagoMp.Dtos.Carritos;

public class CarritoDto
{
    public int CarritoId { get; set; }
    public List<CarritoItemDto> Items { get; set; } = new();
    public decimal Total { get; set; }
}

public class CarritoItemDto
{
    public int ItemId { get; set; }
    public int ProductoId { get; set; }
    public string NombreProducto { get; set; } = "";
    public string? ImagenUrl { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
