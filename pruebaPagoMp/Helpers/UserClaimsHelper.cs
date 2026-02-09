using System.Security.Claims;

namespace pruebaPagoMp.Helpers;

public static class UserClaimsHelper
{
    public static int ObtenerUsuarioId(ClaimsPrincipal user)
    {
        var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!int.TryParse(idStr, out var id))
            throw new UnauthorizedAccessException("Token inválido (sin usuarioId).");
        return id;
    }

    public static bool EsAdminVentas(ClaimsPrincipal user)
        => user.IsInRole("AdminVentas");
}

