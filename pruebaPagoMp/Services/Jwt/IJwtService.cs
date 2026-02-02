using pruebaPagoMp.Data;
using pruebaPagoMp.Models;
public interface IJwtService
{
    string GenerateToken(Usuario usuario, List<string> roles);
}
