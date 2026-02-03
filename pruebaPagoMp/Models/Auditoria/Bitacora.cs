public class Bitacora
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public int? UsuarioId { get; set; }
    public string Accion { get; set; } = null!;
    public string? Detalle { get; set; }
    public string? IP { get; set; }
    public string Resultado { get; set; } = null!;
}
