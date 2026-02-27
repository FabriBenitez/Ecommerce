using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.Dtos.Admin.Usuarios;
using pruebaPagoMp.Models;
using pruebaPagoMp.Models.Bitacora;
using pruebaPagoMp.Services.Bitacora;

namespace pruebaPagoMp.Services.Admin.Usuarios;

public class UsuariosAdminService : IUsuariosAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly IBitacoraService _bitacoraService;

    public UsuariosAdminService(ApplicationDbContext context, IBitacoraService bitacoraService)
    {
        _context = context;
        _bitacoraService = bitacoraService;
    }

    public async Task<int> CrearUsuarioInternoAsync(CrearUsuarioInternoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email)) throw new InvalidOperationException("Email requerido.");
        if (string.IsNullOrWhiteSpace(dto.NombreCompleto)) throw new InvalidOperationException("NombreCompleto requerido.");
        if (string.IsNullOrWhiteSpace(dto.PasswordTemporal)) throw new InvalidOperationException("PasswordTemporal requerida.");

        var existe = await _context.Usuarios.AnyAsync(u => u.Email == dto.Email);
        if (existe) throw new InvalidOperationException("Ya existe un usuario con ese email.");

        var usuario = new Usuario
        {
            Email = dto.Email.Trim(),
            NombreCompleto = dto.NombreCompleto.Trim(),
            Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
            Dni = string.IsNullOrWhiteSpace(dto.Dni) ? null : dto.Dni.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordTemporal),
            DigitoVerificador = "INIT",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        if (dto.Roles.Count > 0)
        {
            var roles = await _context.Roles
                .Where(r => dto.Roles.Contains(r.Nombre))
                .ToListAsync();

            foreach (var rol in roles)
            {
                _context.UsuarioRoles.Add(new UsuarioRol { UsuarioId = usuario.Id, RolId = rol.Id });
            }

            await _context.SaveChangesAsync();
        }

        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            UsuarioId = usuario.Id,
            Accion = "USUARIO_INTERNO_CREAR",
            Detalle = $"Usuario {usuario.Email} creado con {dto.Roles.Count} roles solicitados.",
            Resultado = "OK"
        });

        return usuario.Id;
    }

    public async Task<List<UsuarioListDto>> ListarAsync()
    {
        return await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.UsuarioRoles.Any(ur => ur.Rol.Nombre == "AdminCompras" || ur.Rol.Nombre == "AdminVentas"))
            .OrderBy(u => u.Email)
            .Select(u => new UsuarioListDto
            {
                Id = u.Id,
                Email = u.Email,
                Dni = u.Dni,
                NombreCompleto = u.NombreCompleto,
                Activo = u.Activo,
                Roles = u.UsuarioRoles.Select(ur => ur.Rol.Nombre).ToList()
            })
            .ToListAsync();
    }

    public async Task ActualizarUsuarioInternoAsync(int usuarioId, ActualizarUsuarioInternoDto dto)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario == null) throw new InvalidOperationException("Usuario inexistente.");
        if (string.IsNullOrWhiteSpace(dto.Email)) throw new InvalidOperationException("Email requerido.");
        if (string.IsNullOrWhiteSpace(dto.Rol)) throw new InvalidOperationException("Rol requerido.");
        if (dto.Rol != "AdminCompras" && dto.Rol != "AdminVentas")
            throw new InvalidOperationException("Solo se permite rol AdminCompras o AdminVentas.");

        var existeEmail = await _context.Usuarios.AnyAsync(u => u.Id != usuarioId && u.Email == dto.Email);
        if (existeEmail) throw new InvalidOperationException("Ya existe un usuario con ese email.");

        usuario.Email = dto.Email.Trim();
        usuario.Dni = string.IsNullOrWhiteSpace(dto.Dni) ? null : dto.Dni.Trim();

        if (!string.IsNullOrWhiteSpace(dto.NuevaPassword))
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword.Trim());

        var rolDestino = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == dto.Rol);
        if (rolDestino == null) throw new InvalidOperationException("Rol no existente.");

        var actuales = _context.UsuarioRoles.Where(ur => ur.UsuarioId == usuarioId);
        _context.UsuarioRoles.RemoveRange(actuales);
        _context.UsuarioRoles.Add(new UsuarioRol { UsuarioId = usuarioId, RolId = rolDestino.Id });

        await _context.SaveChangesAsync();

        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            UsuarioId = usuarioId,
            Accion = "USUARIO_ADMIN_ACTUALIZAR",
            Detalle = $"Usuario actualizado. Email={usuario.Email}, Rol={dto.Rol}",
            Resultado = "OK"
        });
    }

    public async Task<UsuarioDetalleDto?> ObtenerDetalleAsync(int id)
    {
        return await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UsuarioDetalleDto
            {
                Id = u.Id,
                Email = u.Email,
                NombreCompleto = u.NombreCompleto,
                Telefono = u.Telefono,
                Dni = u.Dni,
                Activo = u.Activo,
                FechaCreacion = u.FechaCreacion,
                UltimoLogin = u.UltimoLogin,
                Roles = u.UsuarioRoles.Select(ur => ur.Rol.Nombre).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task AsignarRolesAsync(int usuarioId, AsignarRolesDto dto)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario == null) throw new InvalidOperationException("Usuario inexistente.");

        var actuales = _context.UsuarioRoles.Where(ur => ur.UsuarioId == usuarioId);
        _context.UsuarioRoles.RemoveRange(actuales);

        if (dto.Roles.Count > 0)
        {
            var roles = await _context.Roles.Where(r => dto.Roles.Contains(r.Nombre)).ToListAsync();
            foreach (var rol in roles)
            {
                _context.UsuarioRoles.Add(new UsuarioRol { UsuarioId = usuarioId, RolId = rol.Id });
            }
        }

        await _context.SaveChangesAsync();

        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            UsuarioId = usuarioId,
            Accion = "USUARIO_ASIGNAR_ROLES",
            Detalle = $"Roles asignados: {string.Join(", ", dto.Roles)}",
            Resultado = "OK"
        });
    }

    public async Task CambiarActivoAsync(int usuarioId, CambiarActivoDto dto)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario == null) throw new InvalidOperationException("Usuario inexistente.");

        usuario.Activo = dto.Activo;
        await _context.SaveChangesAsync();

        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            UsuarioId = usuarioId,
            Accion = "USUARIO_CAMBIAR_ACTIVO",
            Detalle = $"Nuevo estado activo: {dto.Activo}",
            Resultado = "OK"
        });
    }
}
