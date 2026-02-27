namespace pruebaPagoMp.Dtos.Admin.Usuarios;

public class ActualizarUsuarioInternoDto
{
    public string Email { get; set; } = null!;
    public string? Dni { get; set; }
    public string Rol { get; set; } = null!;
    public string? NuevaPassword { get; set; }
}
