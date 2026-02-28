namespace pruebaPagoMp.Security;

public interface IDigitoVerificadorService
{
    string CalcularDVH(params object?[] values);
    Task RecalcularDVVAsync(string tabla);
    Task<bool> VerificarTablaAsync(string tabla);
    Task<bool> VerificarTodoAsync();
    Task RecalcularEntidadAsync(object entity);
}
