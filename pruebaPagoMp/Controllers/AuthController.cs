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


[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly SecurityLogger _securityLogger;
    private readonly PasswordHasher _passwordHasher;

    public AuthController(
        AuthService authService,
        ApplicationDbContext context,
        IJwtService jwtService,
        SecurityLogger securityLogger,
        PasswordHasher passwordHasher
    )
    {
        _authService = authService;
        _context = context;
        _jwtService = jwtService;
        _securityLogger = securityLogger;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterDto dto)
    {
        try
        {
            _authService.Register(dto);
            return Ok("Usuario registrado");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // 1. Buscar usuario
        var usuario = _context.Usuarios
            .FirstOrDefault(u => u.Email == dto.Email && u.Activo);

        if (usuario == null)
        {
            _securityLogger.LoginFail(dto.Email, ip);
            return Unauthorized("Credenciales invalidas");
        }

        // 2. Verificar password (compatibilidad: BCrypt y PBKDF2 legacy)
        var passwordOk = VerifyPassword(dto.Password, usuario.PasswordHash);

        if (!passwordOk)
        {
            _securityLogger.LoginFail(dto.Email, ip);
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
