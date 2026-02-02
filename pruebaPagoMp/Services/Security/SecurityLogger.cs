namespace pruebaPagoMp.Security;

public class SecurityLogger
{
    private readonly ILogger<SecurityLogger> _logger;

    public SecurityLogger(ILogger<SecurityLogger> logger)
    {
        _logger = logger;
    }

    public void LoginOk(int userId, string ip)
        => _logger.LogInformation("SEC_LOGIN_OK userId={UserId} ip={Ip}", userId, ip);

    public void LoginFail(string email, string ip)
        => _logger.LogWarning("SEC_LOGIN_ERROR email={Email} ip={Ip}", email, ip);

    public void PagoCreado(int pedidoId, int userId)
        => _logger.LogInformation("SEC_PAGO_CREADO pedidoId={PedidoId} userId={UserId}", pedidoId, userId);
}
