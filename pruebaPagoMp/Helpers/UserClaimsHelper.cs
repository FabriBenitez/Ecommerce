using System.Security.Claims;

namespace pruebaPagoMp.Helpers;

public static class UserClaimsHelper
{
    public static int ObtenerUsuarioId(ClaimsPrincipal user)
    {
        var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(idStr))
            throw new UnauthorizedAccessException("No se encontró el Id del usuario en el token.");

        return int.Parse(idStr);
    }
}
