using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.DTOs;
using pruebaPagoMp.Models;
using System.Security.Cryptography;
using pruebaPagoMp.Services;
using pruebaPagoMp.Security;


public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher _hasher;
    private readonly IJwtService _jwtService;
    private readonly SecurityLogger _securityLogger;
    private readonly IDigitoVerificadorService _dvService;
    private string ip = "unknown";

    public AuthService(
        ApplicationDbContext context,
        PasswordHasher hasher,
        IJwtService jwtService,
        SecurityLogger securityLogger,
        IDigitoVerificadorService dvService)
    {
        _context = context;
        _hasher = hasher;
        _jwtService = jwtService;
        _securityLogger = securityLogger;
        _dvService = dvService;
    }

    public void Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.NombreCompleto) ||
            string.IsNullOrWhiteSpace(dto.Dni) ||
            string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.ConfirmPassword))
        {
            throw new Exception("Faltan campos obligatorios para registrarse.");
        }

        if (!string.Equals(dto.Password, dto.ConfirmPassword, StringComparison.Ordinal))
            throw new Exception("Las contrasenas no coinciden.");

        if (_context.Usuarios.Any(u => u.Email == dto.Email))
            throw new Exception("El email ya existe");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var usuario = new Usuario
        {
            Email = dto.Email,
            NombreCompleto = dto.NombreCompleto,
            Dni = dto.Dni,
            DniHash = SecurityHashing.Sha256Normalized(dto.Dni),
            Telefono = dto.telefono,
            PasswordHash = passwordHash,
            FechaCreacion = DateTime.Now,
            DigitoVerificador = ""
        };

        _dvService.RecalcularEntidadAsync(usuario).GetAwaiter().GetResult();

        _context.Usuarios.Add(usuario);
        _context.SaveChanges();

        var rolCliente = _context.Roles.Single(r => r.Nombre == "Cliente");

        _context.UsuarioRoles.Add(new UsuarioRol
        {
            UsuarioId = usuario.Id,
            RolId = rolCliente.Id
        });

        _context.SaveChanges();
        _dvService.RecalcularDVVAsync("Usuarios").GetAwaiter().GetResult();
    }

    public string Login(LoginDto dto)
    {
        var usuario = _context.Usuarios
            .FirstOrDefault(u => u.Email == dto.Email && u.Activo);

        if (usuario == null)
            throw new Exception("Credenciales invalidas");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
            throw new Exception("Credenciales invalidas");

        var roles = _context.UsuarioRoles
            .Where(ur => ur.UsuarioId == usuario.Id)
            .Select(ur => ur.Rol.Nombre)
            .ToList();

        var token = _jwtService.GenerateToken(usuario, roles);


        usuario.UltimoLogin = DateTime.UtcNow;
        _context.SaveChanges();

        _securityLogger.LoginOk(usuario.Id, ip);

        return token;
    }
    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
