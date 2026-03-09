namespace pruebaPagoMp.Dtos.Reportes;

public class ReporteEjecutivoDto
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public DateTime GeneradoEn { get; set; }
    public RentabilidadResumenDto Rentabilidad { get; set; } = new();
    public CanalResumenDto Canal { get; set; } = new();
    public CajaResumenDto Caja { get; set; } = new();
    public InventarioResumenDto Inventario { get; set; } = new();
    public ProveedoresResumenDto Proveedores { get; set; } = new();
}

public class RentabilidadResumenDto
{
    public decimal TotalVendido { get; set; }
    public decimal TotalComprado { get; set; }
    public decimal MargenBrutoEstimado { get; set; }
    public decimal TicketPromedio { get; set; }
    public int CantidadVentas { get; set; }
    public decimal CrecimientoPorcentualVsPeriodoAnterior { get; set; }
}

public class CanalResumenDto
{
    public decimal VentasWeb { get; set; }
    public decimal VentasPresencial { get; set; }
    public decimal PorcentajeWeb { get; set; }
    public decimal PorcentajePresencial { get; set; }
}

public class CajaResumenDto
{
    public decimal Ingresos { get; set; }
    public decimal Egresos { get; set; }
    public decimal TotalCompras { get; set; }
    public decimal TotalNotasCredito { get; set; }
    public int CantidadNotasCredito { get; set; }
    public decimal SaldoNeto { get; set; }
    public string MedioPagoPredominante { get; set; } = "Sin datos";
}

public class InventarioResumenDto
{
    public int StockMinimoConfigurado { get; set; }
    public decimal ValorTotalInventario { get; set; }
    public int ProductosStockBajo { get; set; }
    public int ProductosSinRotacion60Dias { get; set; }
    public List<TopProductoVendidoDto> TopProductosVendidos { get; set; } = new();
    public List<ProductoSinRotacionDto> ProductosSinRotacionDetalle { get; set; } = new();
}

public class TopProductoVendidoDto
{
    public int ProductoId { get; set; }
    public string Producto { get; set; } = "";
    public int CantidadVendida { get; set; }
    public decimal TotalVendido { get; set; }
}

public class ProductoSinRotacionDto
{
    public int ProductoId { get; set; }
    public string Producto { get; set; } = "";
    public int StockActual { get; set; }
    public int DiasSinVenta { get; set; }
}

public class ProveedoresResumenDto
{
    public decimal TotalInvertido { get; set; }
    public int FrecuenciaCompras { get; set; }
    public ProveedorResumenDto? ProveedorMayorVolumen { get; set; }
    public List<ProveedorResumenDto> Ranking { get; set; } = new();
}

public class ProveedorResumenDto
{
    public int ProveedorId { get; set; }
    public string Proveedor { get; set; } = "";
    public decimal TotalInvertido { get; set; }
    public int FrecuenciaCompras { get; set; }
}
