using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Data;
using pruebaPagoMp.Models;
using pruebaPagoMp.Services;
using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.DTOs;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using pruebaPagoMp.Security;
using pruebaPagoMp.Models.Bitacora;
using pruebaPagoMp.Services.Bitacora;


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

    public AuthController(
        AuthService authService,
        ApplicationDbContext context,
        IJwtService jwtService,
        SecurityLogger securityLogger,
        PasswordHasher passwordHasher,
        IBitacoraService bitacoraService
    )
    {
        _authService = authService;
        _context = context;
        _jwtService = jwtService;
        _securityLogger = securityLogger;
        _passwordHasher = passwordHasher;
        _bitacoraService = bitacoraService;
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
}
