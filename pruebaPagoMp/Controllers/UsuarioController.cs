using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Data;

[ApiController]
[Route("api/usuarios")]
public class UsuarioController : ControllerBase
{
    [Authorize]
    [HttpGet("perfil")]
    public IActionResult Perfil()
    {
        var email = User.Identity?.Name;
            return Ok(new
            {
                mensaje = "Acceso autorizado",
                usuario = email
            });
    }
}


