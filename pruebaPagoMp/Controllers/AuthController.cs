using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Data;
using pruebaPagoMp.Models;
using pruebaPagoMp.Services;
using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.DTOs;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using pruebaPagoMp.Security;
using pruebaPagoMp.Models.Bitacora;
using pruebaPagoMp.Services.Bitacora;
using pruebaPagoMp.Services.Email;
using Microsoft.AspNetCore.WebUtilities;


[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly SecurityLogger _securityLogger;
    private readonly PasswordHasher _passwordHasher;
    private readonly IBitacoraService _bitacoraService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IDigitoVerificadorService _dvService;
    private readonly IWebHostEnvironment _env;

    public AuthController(
        AuthService authService,
        ApplicationDbContext context,
        IJwtService jwtService,
        SecurityLogger securityLogger,
        PasswordHasher passwordHasher,
        IBitacoraService bitacoraService,
        IEmailService emailService,
        IConfiguration configuration,
        IDigitoVerificadorService dvService,
        IWebHostEnvironment env
    )
    {
        _authService = authService;
        _context = context;
        _jwtService = jwtService;
        _securityLogger = securityLogger;
        _passwordHasher = passwordHasher;
        _bitacoraService = bitacoraService;
        _emailService = emailService;
        _configuration = configuration;
        _dvService = dvService;
        _env = env;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        try
        {
            _authService.Register(dto);
            _bitacoraService.RegistrarAsync(new BitacoraEntry
            {
                Accion = "AUTH_REGISTER",
                Detalle = $"Registro usuario {dto.Email}",
                Ip = ip,
                Resultado = "OK"
            }).GetAwaiter().GetResult();
            return Ok("Usuario registrado");
        }
        catch (Exception ex)
        {
            _bitacoraService.RegistrarAsync(new BitacoraEntry
            {
                Accion = "AUTH_REGISTER",
                Detalle = $"Error registro {dto.Email}: {ex.Message}",
                Ip = ip,
                Resultado = "ERROR"
            }).GetAwaiter().GetResult();
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var email = dto.Email?.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(dto.Password))
            return Unauthorized("Credenciales invalidas");

        // 1. Buscar usuario
        var usuario = _context.Usuarios
            .FirstOrDefault(u => u.Activo && u.Email.ToLower() == email.ToLower());

        if (usuario == null)
        {
            _securityLogger.LoginFail(email, ip);
            await _bitacoraService.RegistrarAsync(new BitacoraEntry
            {
                Accion = "AUTH_LOGIN",
                Detalle = $"Login fallido {email}",
                Ip = ip,
                Resultado = "ERROR"
            });
            return Unauthorized("Credenciales invalidas");
        }

        // 2. Verificar password (compatibilidad: BCrypt y PBKDF2 legacy)
        var passwordOk = VerifyPassword(dto.Password, usuario.PasswordHash);

        if (!passwordOk)
        {
            _securityLogger.LoginFail(email, ip);
            await _bitacoraService.RegistrarAsync(new BitacoraEntry
            {
                UsuarioId = usuario.Id,
                Accion = "AUTH_LOGIN",
                Detalle = $"Password inválida {email}",
                Ip = ip,
                Resultado = "ERROR"
            });
            return Unauthorized("Credenciales invalidas");
        }

        // 3. Obtener roles
        var roles = _context.UsuarioRoles
            .Where(ur => ur.UsuarioId == usuario.Id)
            .Select(ur => ur.Rol.Nombre)
            .ToList();

        // 4. Generar access token
        var accessToken = _jwtService.GenerateToken(usuario, roles);

        // 5. Generar refresh token
        var refreshToken = new RefreshToken
        {
            UsuarioId = usuario.Id,
            Token = _authService.GenerateRefreshToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshToken);

        // 6. Auditoria minima
        usuario.UltimoLogin = DateTime.UtcNow;

        _context.SaveChanges();

        // 7. Log de seguridad (OK)
        _securityLogger.LoginOk(usuario.Id, ip);
        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            UsuarioId = usuario.Id,
            Accion = "AUTH_LOGIN",
            Detalle = $"Login OK {email}",
            Ip = ip,
            Resultado = "OK"
        });

        // 8. Respuesta FINAL
        return Ok(new
        {
            accessToken,
            refreshToken = refreshToken.Token
        });
    }

    [Authorize]
    [HttpGet("perfil")]
    public IActionResult Perfil()
    {
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return Ok(new
        {
            userId,
            email
        });
    }

    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] RefreshTokenDto dto)
    {
        var storedToken = _context.RefreshTokens
            .Include(rt => rt.Usuario)
            .FirstOrDefault(rt =>
                rt.Token == dto.RefreshToken &&
                !rt.IsRevoked &&
                rt.ExpiresAt > DateTime.UtcNow
            );

        if (storedToken == null)
            return Unauthorized("Refresh token invalido");

        var usuario = storedToken.Usuario;

        var roles = _context.UsuarioRoles
            .Where(ur => ur.UsuarioId == usuario.Id)
            .Select(ur => ur.Rol.Nombre)
            .ToList();

        // Revocar el token usado
        storedToken.IsRevoked = true;

        // Nuevo refresh token
        var newRefreshToken = new RefreshToken
        {
            UsuarioId = usuario.Id,
            Token = _authService.GenerateRefreshToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _context.RefreshTokens.Add(newRefreshToken);

        var newAccessToken = _jwtService.GenerateToken(usuario, roles);

        _context.SaveChanges();

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken.Token
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        int.TryParse(userIdStr, out var userId);

        try
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == dto.RefreshToken && x.UsuarioId == userId && !x.IsRevoked);

            if (token != null)
            {
                token.IsRevoked = true;
                await _context.SaveChangesAsync();
            }

            await _bitacoraService.RegistrarAsync(new BitacoraEntry
            {
                UsuarioId = userId > 0 ? userId : null,
                Accion = "AUTH_LOGOUT",
                Detalle = "Logout OK",
                Ip = ip,
                Resultado = "OK"
            });

            return Ok();
        }
        catch (Exception ex)
        {
            await _bitacoraService.RegistrarAsync(new BitacoraEntry
            {
                UsuarioId = userId > 0 ? userId : null,
                Accion = "AUTH_LOGOUT",
                Detalle = ex.Message,
                Ip = ip,
                Resultado = "ERROR"
            });

            return BadRequest("No se pudo cerrar sesión.");
        }
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var email = dto.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
            return Ok(new { message = "Si el correo existe, enviamos instrucciones para recuperar la cuenta." });

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Activo && u.Email.ToLower() == email.ToLower());

        if (usuario == null)
        {
            await _bitacoraService.RegistrarAsync(new BitacoraEntry
            {
                Accion = "AUTH_FORGOT_PASSWORD",
                Detalle = $"Solicitud para email inexistente {email}",
                Ip = ip,
                Resultado = "OK"
            });

            return Ok(new { message = "Si el correo existe, enviamos instrucciones para recuperar la cuenta." });
        }

        var now = DateTime.UtcNow;
        var activos = await _context.PasswordResetTokens
            .Where(t => t.UsuarioId == usuario.Id && t.UsedAt == null && t.ExpiresAt > now)
            .ToListAsync();

        foreach (var item in activos)
            item.UsedAt = now;

        var rawToken = GenerateResetToken();
        var tokenHash = HashToken(rawToken);

        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UsuarioId = usuario.Id,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(30)
        });

        await _context.SaveChangesAsync();

        var frontendBaseUrl = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
        var link = $"{frontendBaseUrl}/reset-password?email={Uri.EscapeDataString(usuario.Email)}&token={Uri.EscapeDataString(rawToken)}";
        var subject = "Recupera tu contrasena";
        var body = $@"
<p>Recibimos una solicitud para restablecer tu contrasena.</p>
<p>Haz clic aqui para continuar:</p>
<p><a href=""{link}"">{link}</a></p>
<p>Este enlace vence en 30 minutos y solo se puede usar una vez.</p>
<p>Si no solicitaste este cambio, puedes ignorar este mensaje.</p>";

        try
        {
            await _emailService.SendAsync(usuario.Email, subject, body);
        }
        catch (Exception ex)
        {
            await _bitacoraService.RegistrarAsync(new BitacoraEntry
            {
                UsuarioId = usuario.Id,
                Accion = "AUTH_FORGOT_PASSWORD",
                Detalle = $"Error enviando email: {ex.Message}",
                Ip = ip,
                Resultado = "ERROR"
            });
            if (_env.IsDevelopment())
            {
                await _bitacoraService.RegistrarAsync(new BitacoraEntry
                {
                    UsuarioId = usuario.Id,
                    Accion = "AUTH_FORGOT_PASSWORD",
                    Detalle = $"Modo desarrollo sin SMTP. Link recupero: {link}",
                    Ip = ip,
                    Resultado = "OK"
                });

                return Ok(new
                {
                    message = "SMTP no configurado. Se genero el link de recupero en modo desarrollo.",
                    devResetLink = link
                });
            }

            return StatusCode(500, new { error = "No se pudo enviar el correo de recupero." });
        }

        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            UsuarioId = usuario.Id,
            Accion = "AUTH_FORGOT_PASSWORD",
            Detalle = $"Solicitud recupero enviada a {usuario.Email}",
            Ip = ip,
            Resultado = "OK"
        });

        return Ok(new { message = "Si el correo existe, enviamos instrucciones para recuperar la cuenta." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var email = dto.Email?.Trim();
        var newPassword = dto.NewPassword?.Trim();
        var confirmPassword = dto.ConfirmPassword?.Trim();

        if (string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(dto.Token)
            || string.IsNullOrWhiteSpace(newPassword)
            || string.IsNullOrWhiteSpace(confirmPassword))
            return BadRequest(new { error = "Datos incompletos." });

        if (newPassword.Length < 6)
            return BadRequest(new { error = "La nueva contrasena debe tener al menos 6 caracteres." });
        if (newPassword != confirmPassword)
            return BadRequest(new { error = "Las contrasenas no coinciden." });

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Activo && u.Email.ToLower() == email.ToLower());

        if (usuario == null)
            return BadRequest(new { error = "Token invalido o vencido." });

        var tokenHash = HashToken(dto.Token.Trim());
        var now = DateTime.UtcNow;
        var resetToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t =>
                t.UsuarioId == usuario.Id
                && t.TokenHash == tokenHash
                && t.UsedAt == null
                && t.ExpiresAt > now);

        if (resetToken == null)
        {
            await _bitacoraService.RegistrarAsync(new BitacoraEntry
            {
                UsuarioId = usuario.Id,
                Accion = "AUTH_RESET_PASSWORD",
                Detalle = "Intento con token invalido o vencido",
                Ip = ip,
                Resultado = "ERROR"
            });
            return BadRequest(new { error = "Token invalido o vencido." });
        }

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _dvService.RecalcularEntidadAsync(usuario);
        resetToken.UsedAt = now;

        var userTokens = await _context.RefreshTokens
            .Where(x => x.UsuarioId == usuario.Id && !x.IsRevoked)
            .ToListAsync();
        foreach (var item in userTokens)
            item.IsRevoked = true;

        await _context.SaveChangesAsync();

        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            UsuarioId = usuario.Id,
            Accion = "AUTH_RESET_PASSWORD",
            Detalle = "Password restablecida correctamente",
            Ip = ip,
            Resultado = "OK"
        });

        return Ok(new { message = "Contrasena actualizada correctamente." });
    }

    private bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
            return false;

        try
        {
            if (storedHash.StartsWith("$2"))
            {
                return BCrypt.Net.BCrypt.Verify(password, storedHash);
            }

            return _passwordHasher.Verify(password, storedHash);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (SaltParseException)
        {
            return false;
        }
    }

    private static string GenerateResetToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
