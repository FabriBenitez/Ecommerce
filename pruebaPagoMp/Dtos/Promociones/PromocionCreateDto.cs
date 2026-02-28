namespace pruebaPagoMp.Dtos.Promociones;

public class PromocionCreateDto
{
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public int? ProductoId { get; set; }
    public string? Genero { get; set; }
    public decimal? PorcentajeDescuento { get; set; }
    public decimal? MontoDescuento { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public bool Activa { get; set; } = true;
}
