using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.Dtos.Caja;
using pruebaPagoMp.Models.Bitacora;
using pruebaPagoMp.Models.Caja.Enums;
using pruebaPagoMp.Security;
using pruebaPagoMp.Services.Bitacora;

namespace pruebaPagoMp.Services.Caja;

public class CajaService : ICajaService
{
    private readonly ApplicationDbContext _context;
    private readonly IBitacoraService _bitacoraService;
    private readonly IDigitoVerificadorService _dvService;

    public CajaService(ApplicationDbContext context, IBitacoraService bitacoraService, IDigitoVerificadorService dvService)
    {
        _context = context;
        _bitacoraService = bitacoraService;
        _dvService = dvService;
    }

    public async Task<CajaResumenDto> AbrirAsync(AbrirCajaDto dto)
    {
        if (dto.SaldoInicial < 0m) throw new InvalidOperationException("El saldo inicial no puede ser negativo.");

        var abierta = await _context.Cajas.FirstOrDefaultAsync(c => c.Abierta);
        if (abierta != null) throw new InvalidOperationException("Ya existe una caja abierta.");

        var caja = new pruebaPagoMp.Models.Caja.Caja
        {
            SaldoInicial = dto.SaldoInicial,
            Observaciones = dto.Observaciones,
            FechaApertura = DateTime.UtcNow,
            Abierta = true
        };

        _context.Cajas.Add(caja);
        await _context.SaveChangesAsync();
        await _dvService.RecalcularDVVAsync("MovimientosCaja");

        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            Accion = "CAJA_APERTURA",
            Detalle = $"Caja #{caja.Id} abierta con saldo inicial {caja.SaldoInicial}.",
            Resultado = "OK"
        });

        return await ObtenerResumenAsync();
    }

    public async Task<CajaResumenDto> CerrarAsync(CerrarCajaDto dto)
    {
        if (dto.SaldoFinal < 0m) throw new InvalidOperationException("El saldo final no puede ser negativo.");

        var caja = await _context.Cajas.FirstOrDefaultAsync(c => c.Abierta);
        if (caja == null) throw new InvalidOperationException("No hay caja abierta.");

        var fechaCierre = DateTime.UtcNow;
        var movimientos = await _context.MovimientosCaja
            .AsNoTracking()
            .Where(m => m.Fecha >= caja.FechaApertura && m.Fecha <= fechaCierre)
            .ToListAsync();

        var ingresos = movimientos.Where(m => m.Tipo == TipoMovimientoCaja.Ingreso).Sum(m => m.Monto);
        var egresos = movimientos.Where(m => m.Tipo == TipoMovimientoCaja.Egreso).Sum(m => m.Monto);
        var saldoEsperado = decimal.Round(caja.SaldoInicial + ingresos - egresos, 2);
        var saldoDeclarado = decimal.Round(dto.SaldoFinal, 2);

        if (Math.Abs(saldoEsperado - saldoDeclarado) > 0.01m)
            throw new InvalidOperationException($"El saldo final no coincide. Esperado: {saldoEsperado}, informado: {saldoDeclarado}.");

        caja.Abierta = false;
        caja.FechaCierre = fechaCierre;
        caja.SaldoFinal = dto.SaldoFinal;
        caja.Observaciones = dto.Observaciones ?? caja.Observaciones;

        await _context.SaveChangesAsync();
        await _dvService.RecalcularDVVAsync("MovimientosCaja");

        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            Accion = "CAJA_CIERRE",
            Detalle = $"Caja #{caja.Id} cerrada con saldo final {caja.SaldoFinal}.",
            Resultado = "OK"
        });

        return await ObtenerResumenAsync();
    }

    public async Task<CajaResumenDto> ObtenerResumenAsync()
    {
        var caja = await _context.Cajas
            .AsNoTracking()
            .OrderByDescending(c => c.FechaApertura)
            .FirstOrDefaultAsync();

        if (caja == null)
        {
            return new CajaResumenDto
            {
                Abierta = false
            };
        }

        if (!caja.Abierta)
        {
            return new CajaResumenDto
            {
                CajaId = caja.Id,
                Abierta = false,
                SaldoInicial = 0m,
                Ingresos = 0m,
                Egresos = 0m,
                SaldoActual = 0m,
                FechaApertura = caja.FechaApertura,
                FechaCierre = caja.FechaCierre
            };
        }

        var inicio = caja.FechaApertura;
        var fin = caja.FechaCierre ?? DateTime.UtcNow;

        var movimientos = await _context.MovimientosCaja
            .AsNoTracking()
            .Where(m => m.Fecha >= inicio && m.Fecha <= fin)
            .ToListAsync();
        var ingresos = movimientos.Where(m => m.Tipo == TipoMovimientoCaja.Ingreso).Sum(m => m.Monto);
        var egresos = movimientos.Where(m => m.Tipo == TipoMovimientoCaja.Egreso).Sum(m => m.Monto);

        return new CajaResumenDto
        {
            CajaId = caja.Id,
            Abierta = caja.Abierta,
            SaldoInicial = caja.SaldoInicial,
            Ingresos = ingresos,
            Egresos = egresos,
            SaldoActual = caja.SaldoInicial + ingresos - egresos,
            FechaApertura = caja.FechaApertura,
            FechaCierre = caja.FechaCierre
        };
    }

    public async Task<List<MovimientoCajaDto>> ListarMovimientosAsync(DateTime? desde, DateTime? hasta)
    {
        var q = _context.MovimientosCaja.AsNoTracking().AsQueryable();

        if (desde.HasValue) q = q.Where(m => m.Fecha >= desde.Value);
        if (hasta.HasValue) q = q.Where(m => m.Fecha <= hasta.Value);

        return await q
            .OrderByDescending(m => m.Fecha)
            .Select(m => new MovimientoCajaDto
            {
                Id = m.Id,
                Fecha = m.Fecha,
                Tipo = m.Tipo,
                Monto = m.Monto,
                Concepto = m.Concepto,
                VentaId = m.VentaId,
                MedioPago = m.MedioPago,
                Referencia = m.Referencia
            })
            .ToListAsync();
    }

    public async Task<List<CajaHistorialDiarioDto>> ListarHistorialDiarioAsync(int dias)
    {
        var diasNormalizados = Math.Clamp(dias, 1, 365);
        var desde = DateTime.UtcNow.Date.AddDays(-(diasNormalizados - 1));

        var cajas = await _context.Cajas
            .AsNoTracking()
            .Where(c => c.FechaApertura >= desde)
            .OrderByDescending(c => c.FechaApertura)
            .ToListAsync();

        if (cajas.Count == 0) return new List<CajaHistorialDiarioDto>();

        var inicioRango = cajas.Min(c => c.FechaApertura);
        var finRango = cajas.Max(c => c.FechaCierre ?? DateTime.UtcNow);

        var movimientos = await _context.MovimientosCaja
            .AsNoTracking()
            .Where(m => m.Fecha >= inicioRango && m.Fecha <= finRango)
            .Select(m => new { m.Fecha, m.Tipo, m.Monto })
            .ToListAsync();

        var ahora = DateTime.UtcNow;
        var resultado = new List<CajaHistorialDiarioDto>(cajas.Count);

        foreach (var caja in cajas)
        {
            var finCaja = caja.FechaCierre ?? ahora;
            var movimientosCaja = movimientos
                .Where(m => m.Fecha >= caja.FechaApertura && m.Fecha <= finCaja)
                .ToList();

            var ingresos = movimientosCaja
                .Where(m => m.Tipo == TipoMovimientoCaja.Ingreso)
                .Sum(m => m.Monto);

            var egresos = movimientosCaja
                .Where(m => m.Tipo == TipoMovimientoCaja.Egreso)
                .Sum(m => m.Monto);

            var saldoEsperado = decimal.Round(caja.SaldoInicial + ingresos - egresos, 2);
            decimal? diferenciaCierre = null;

            if (caja.SaldoFinal.HasValue)
            {
                diferenciaCierre = decimal.Round(caja.SaldoFinal.Value - saldoEsperado, 2);
            }

            resultado.Add(new CajaHistorialDiarioDto
            {
                CajaId = caja.Id,
                Fecha = caja.FechaApertura.Date,
                FechaApertura = caja.FechaApertura,
                FechaCierre = caja.FechaCierre,
                Abierta = caja.Abierta,
                SaldoInicial = caja.SaldoInicial,
                Ingresos = ingresos,
                Egresos = egresos,
                SaldoEsperado = saldoEsperado,
                SaldoFinal = caja.SaldoFinal,
                DiferenciaCierre = diferenciaCierre,
                CantidadMovimientos = movimientosCaja.Count
            });
        }

        return resultado;
    }
}
