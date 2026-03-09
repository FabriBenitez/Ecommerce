namespace pruebaPagoMp.DTOs;

public class ActualizarProductoDto
{
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public string? Editorial { get; set; }
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public string? ImagenUrl { get; set; }
    public int? ProveedorId { get; set; }
    public decimal? CostoProveedor { get; set; }
}
