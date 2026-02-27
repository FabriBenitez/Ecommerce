namespace pruebaPagoMp.Dtos.Reportes;

public class ReporteStockDto
{
    public int ProductoId { get; set; }
    public string Producto { get; set; } = null!;
    public int StockActual { get; set; }
}
