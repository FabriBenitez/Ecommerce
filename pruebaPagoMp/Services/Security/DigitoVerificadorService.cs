using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.Models;
using pruebaPagoMp.Models.Caja;
using pruebaPagoMp.Models.Compras;
using pruebaPagoMp.Models.Ventas;

namespace pruebaPagoMp.Security;

public class DigitoVerificadorService : IDigitoVerificadorService
{
    private readonly ApplicationDbContext _context;

    public DigitoVerificadorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public string CalcularDVH(params object?[] values)
    {
        var raw = string.Join("|", values.Select(Normalize));
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
    }

    public async Task RecalcularEntidadAsync(object entity)
    {
        switch (entity)
        {
            case Usuario u:
                u.DigitoVerificador = CalcularDVH(u.Id, u.Email, u.NombreCompleto, u.Dni, u.Activo, u.FechaCreacion, u.UltimoLogin);
                break;
            case Proveedor p:
                p.DigitoVerificador = CalcularDVH(p.Id, p.RazonSocial, p.CUIT, p.Email, p.Telefono, p.Activo);
                break;
            case Compra c:
                c.DigitoVerificador = CalcularDVH(c.Id, c.ProveedorId, c.Fecha, c.Total, c.EstadoCompra);
                break;
            case Venta v:
                v.DigitoVerificador = CalcularDVH(v.Id, v.UsuarioId, v.Fecha, v.Total, v.EstadoVenta, v.Canal, v.ClienteDni, v.EstadoRetiro);
                break;
            case MovimientoCaja m:
                m.DigitoVerificador = CalcularDVH(m.Id, m.Fecha, m.Tipo, m.Monto, m.Concepto, m.VentaId, m.MedioPago, m.Referencia);
                break;
        }

        await Task.CompletedTask;
    }

    public async Task RecalcularDVVAsync(string tabla)
    {
        var dvhs = await ObtenerDVHsAsync(tabla);
        var dvv = CalcularDVH(tabla, string.Join("|", dvhs.OrderBy(x => x, StringComparer.Ordinal)));

        var row = await _context.DigitosVerificadores.FirstOrDefaultAsync(x => x.Tabla == tabla);
        if (row == null)
        {
            _context.DigitosVerificadores.Add(new DigitoVerificador { Tabla = tabla, Valor = dvv });
        }
        else
        {
            row.Valor = dvv;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> VerificarTablaAsync(string tabla)
    {
        var row = await _context.DigitosVerificadores.AsNoTracking().FirstOrDefaultAsync(x => x.Tabla == tabla);
        if (row == null) return false;

        var dvhs = await ObtenerDVHsAsync(tabla);
        var calculado = CalcularDVH(tabla, string.Join("|", dvhs.OrderBy(x => x, StringComparer.Ordinal)));
        return string.Equals(row.Valor, calculado, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> VerificarTodoAsync()
    {
        var tablas = new[] { "Usuarios", "Proveedores", "Compras", "Ventas", "MovimientosCaja" };
        foreach (var tabla in tablas)
        {
            if (!await VerificarTablaAsync(tabla)) return false;
        }

        return true;
    }

    private async Task<List<string>> ObtenerDVHsAsync(string tabla)
    {
        return tabla switch
        {
            "Usuarios" => await _context.Usuarios.AsNoTracking().Select(x => x.DigitoVerificador).Where(x => !string.IsNullOrWhiteSpace(x)).ToListAsync(),
            "Proveedores" => await _context.Proveedores.AsNoTracking().Select(x => x.DigitoVerificador).Where(x => !string.IsNullOrWhiteSpace(x)).ToListAsync(),
            "Compras" => await _context.Compras.AsNoTracking().Select(x => x.DigitoVerificador).Where(x => !string.IsNullOrWhiteSpace(x)).ToListAsync(),
            "Ventas" => await _context.Ventas.AsNoTracking().Select(x => x.DigitoVerificador).Where(x => !string.IsNullOrWhiteSpace(x)).ToListAsync(),
            "MovimientosCaja" => await _context.MovimientosCaja.AsNoTracking().Select(x => x.DigitoVerificador).Where(x => !string.IsNullOrWhiteSpace(x)).ToListAsync(),
            _ => throw new InvalidOperationException($"Tabla DVV no soportada: {tabla}.")
        };
    }

    private static string Normalize(object? value)
    {
        if (value == null) return "";

        return value switch
        {
            DateTime dt => dt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            decimal dec => dec.ToString("0.############################", CultureInfo.InvariantCulture),
            double dbl => dbl.ToString("R", CultureInfo.InvariantCulture),
            float fl => fl.ToString("R", CultureInfo.InvariantCulture),
            bool b => b ? "1" : "0",
            _ => value.ToString()?.Trim() ?? ""
        };
    }
}
