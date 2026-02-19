namespace pruebaPagoMp.Dtos.Ventas;

public class CrearVentaPresencialDto
{
    public string? ClienteDni { get; set; }
    public string? ClienteNombre { get; set; }

    public string? Observaciones { get; set; }

    public List<CrearVentaPresencialItemDto> Items { get; set; } = new();
    public List<PagoItemDto> Pagos { get; set; } = new();

}

public class CrearVentaPresencialItemDto
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
}
