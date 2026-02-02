using Microsoft.AspNetCore.DataProtection;

namespace pruebaPagoMp.Security;

public class CampoProtegidoService
{
    private readonly IDataProtector _protector;

    public CampoProtegidoService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("pii.telefono.v1");
    }

    public string? Proteger(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? valor : _protector.Protect(valor);

    public string? Desproteger(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? valor : _protector.Unprotect(valor);
}
