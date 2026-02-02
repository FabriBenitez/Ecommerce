using System.Security.Cryptography;
using System.Text;

namespace pruebaPagoMp.Security;

public static class DigitoVerificadorHelper
{
    public static string CalcularUsuario(string email, string nombre, string? telefono, bool activo)
    {
        var raw = $"{email}|{nombre}|{telefono}|{activo}";
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
    }
}
