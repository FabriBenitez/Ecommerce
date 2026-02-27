namespace pruebaPagoMp.Models.Bitacora;

public class BitacoraEntry
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int? UsuarioId { get; set; }
    public string Accion { get; set; } = null!;
    public string? Detalle { get; set; }
    public string? Ip { get; set; }
    public string Resultado { get; set; } = null!;
}
