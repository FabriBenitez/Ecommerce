namespace pruebaPagoMp.Dtos.Promociones;

public class PromocionListDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public int? ProductoId { get; set; }
    public string? ProductoNombre { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public bool Activa { get; set; }
}
