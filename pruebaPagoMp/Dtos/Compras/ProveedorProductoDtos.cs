namespace pruebaPagoMp.Dtos.Compras;

public class ProveedorProductoUpsertDto
{
    public int ProductoId { get; set; }
    public decimal CostoUnitario { get; set; }
}

public class ProveedorProductoListDto
{
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal CostoUnitario { get; set; }
    public int StockActual { get; set; }
}
