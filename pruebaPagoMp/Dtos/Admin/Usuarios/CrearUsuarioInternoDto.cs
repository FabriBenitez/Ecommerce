namespace pruebaPagoMp.Dtos.Admin.Usuarios;

public class CrearUsuarioInternoDto
{
    public string Email { get; set; } = null!;
    public string NombreCompleto { get; set; } = null!;
    public string? Telefono { get; set; }
    public string? Dni { get; set; }
    public string PasswordTemporal { get; set; } = null!;
    public List<string> Roles { get; set; } = new();
}
