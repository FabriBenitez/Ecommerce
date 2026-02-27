using pruebaPagoMp.Dtos.Admin.Usuarios;

namespace pruebaPagoMp.Services.Admin.Usuarios;

public interface IUsuariosAdminService
{
    Task<int> CrearUsuarioInternoAsync(CrearUsuarioInternoDto dto);
    Task<List<UsuarioListDto>> ListarAsync();
    Task<UsuarioDetalleDto?> ObtenerDetalleAsync(int id);
    Task ActualizarUsuarioInternoAsync(int usuarioId, ActualizarUsuarioInternoDto dto);
    Task AsignarRolesAsync(int usuarioId, AsignarRolesDto dto);
    Task CambiarActivoAsync(int usuarioId, CambiarActivoDto dto);
}
