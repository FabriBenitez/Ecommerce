using pruebaPagoMp.Models.Compras.Enums;

namespace pruebaPagoMp.Dtos.Compras;

public class CrearCompraDto
{
    public int ProveedorId { get; set; }
    public List<CrearCompraItemDto> Items { get; set; } = new();
}

public class CrearCompraItemDto
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
}

public class CompraListDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Proveedor { get; set; } = null!;
    public decimal Total { get; set; }
    public EstadoCompra EstadoCompra { get; set; }
}