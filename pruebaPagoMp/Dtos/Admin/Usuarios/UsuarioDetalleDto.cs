namespace pruebaPagoMp.Dtos.Admin.Usuarios;

public class UsuarioDetalleDto
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string NombreCompleto { get; set; } = null!;
    public string? Telefono { get; set; }
    public string? Dni { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? UltimoLogin { get; set; }
    public List<string> Roles { get; set; } = new();
}
