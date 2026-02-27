namespace pruebaPagoMp.Dtos.Admin.Usuarios;

public class UsuarioListDto
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string? Dni { get; set; }
    public string NombreCompleto { get; set; } = null!;
    public bool Activo { get; set; }
    public List<string> Roles { get; set; } = new();
}
