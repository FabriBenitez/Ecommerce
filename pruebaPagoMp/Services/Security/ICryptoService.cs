namespace pruebaPagoMp.Security;

public interface ICryptoService
{
    string Encrypt(string plain);
    string Decrypt(string cipher);
}
