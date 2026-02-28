using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace pruebaPagoMp.Security;

public class AesCryptoService : ICryptoService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesCryptoService(IConfiguration configuration)
    {
        var keyB64 = configuration["Security:AesKey"];
        var ivB64 = configuration["Security:AesIV"];

        if (string.IsNullOrWhiteSpace(keyB64) || string.IsNullOrWhiteSpace(ivB64))
            throw new InvalidOperationException("Falta Security:AesKey o Security:AesIV en configuración.");

        _key = Convert.FromBase64String(keyB64);
        _iv = Convert.FromBase64String(ivB64);

        if (_key.Length != 32) throw new InvalidOperationException("Security:AesKey debe ser 32 bytes en base64.");
        if (_iv.Length != 16) throw new InvalidOperationException("Security:AesIV debe ser 16 bytes en base64.");
    }

    public string Encrypt(string plain)
    {
        if (string.IsNullOrWhiteSpace(plain)) return plain;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var bytes = Encoding.UTF8.GetBytes(plain.Trim());
        using var encryptor = aes.CreateEncryptor();
        var cipher = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        return Convert.ToBase64String(cipher);
    }

    public string Decrypt(string cipher)
    {
        if (string.IsNullOrWhiteSpace(cipher)) return cipher;

        var payload = Convert.FromBase64String(cipher);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var plain = decryptor.TransformFinalBlock(payload, 0, payload.Length);
        return Encoding.UTF8.GetString(plain);
    }
}
