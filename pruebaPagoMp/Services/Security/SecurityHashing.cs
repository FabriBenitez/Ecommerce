using System.Security.Cryptography;
using System.Text;

namespace pruebaPagoMp.Security;

public static class SecurityHashing
{
    public static string Sha256Normalized(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(normalized)));
    }
}
