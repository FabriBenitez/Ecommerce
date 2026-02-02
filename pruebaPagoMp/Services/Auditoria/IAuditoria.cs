public interface IAuditable
{
    DateTime FechaCreacion { get; set; }
    DateTime? FechaActualizacion { get; set; }
    int? CreadoPorUsuarioId { get; set; }
    int? ActualizadoPorUsuarioId { get; set; }
}
