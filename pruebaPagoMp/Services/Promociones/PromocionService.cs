using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.Dtos.Promociones;
using pruebaPagoMp.Models.Promociones;

namespace pruebaPagoMp.Services.Promociones;

public class PromocionService : IPromocionService
{
    private readonly ApplicationDbContext _context;

    public PromocionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> CrearAsync(PromocionCreateDto dto)
    {
        if (!dto.ProductoId.HasValue && string.IsNullOrWhiteSpace(dto.Genero))
            throw new InvalidOperationException("Debe indicar Libro (ProductoId) o Genero.");
        var porcentaje = dto.PorcentajeDescuento;
        var monto = dto.MontoDescuento;
        var tienePorcentaje = porcentaje.HasValue && porcentaje.Value > 0m;
        var tieneMonto = monto.HasValue && monto.Value > 0m;

        if (!tienePorcentaje && !tieneMonto)
            throw new InvalidOperationException("Debe indicar un descuento valido mayor a 0 (porcentaje o monto fijo).");
        if (tienePorcentaje && tieneMonto)
            throw new InvalidOperationException("Debe elegir solo un tipo de descuento: porcentaje o monto fijo.");
        if (porcentaje.HasValue && porcentaje.Value > 100m)
            throw new InvalidOperationException("El porcentaje no puede ser mayor a 100.");
        if (porcentaje.HasValue && porcentaje.Value <= 0m)
            throw new InvalidOperationException("El porcentaje debe ser mayor a 0.");
        if (monto.HasValue && monto.Value <= 0m)
            throw new InvalidOperationException("El monto fijo debe ser mayor a 0.");

        var fechaInicio = dto.FechaInicio.Date;
        var fechaFin = dto.FechaFin.Date.AddDays(1).AddTicks(-1);
        if (fechaFin < fechaInicio) throw new InvalidOperationException("FechaFin no puede ser menor a FechaInicio.");

        if (dto.ProductoId.HasValue)
        {
            var existeProducto = await _context.Productos.AnyAsync(p => p.Id == dto.ProductoId.Value);
            if (!existeProducto) throw new InvalidOperationException("El producto seleccionado no existe.");
        }

        var productoNombre = dto.ProductoId.HasValue
            ? await _context.Productos
                .Where(p => p.Id == dto.ProductoId.Value)
                .Select(p => p.Nombre)
                .FirstOrDefaultAsync()
            : null;

        var nombre = string.IsNullOrWhiteSpace(dto.Nombre)
            ? (dto.ProductoId.HasValue
                ? (string.IsNullOrWhiteSpace(productoNombre) ? "Promo libro" : productoNombre)
                : $"Promo genero {dto.Genero!.Trim()}")
            : dto.Nombre.Trim();

        var promo = new Promocion
        {
            Nombre = nombre,
            Descripcion = dto.Descripcion,
            ProductoId = dto.ProductoId,
            Genero = string.IsNullOrWhiteSpace(dto.Genero) ? null : dto.Genero.Trim(),
            PorcentajeDescuento = dto.PorcentajeDescuento,
            MontoDescuento = dto.MontoDescuento,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            Activa = dto.Activa
        };

        _context.Promociones.Add(promo);
        await _context.SaveChangesAsync();
        return promo.Id;
    }

    public async Task<List<PromocionListDto>> ListarAsync(bool? activas)
    {
        var q = _context.Promociones.AsNoTracking().AsQueryable();

        if (activas.HasValue)
            q = q.Where(p => p.Activa == activas.Value);

        return await q
            .OrderByDescending(p => p.FechaInicio)
            .Select(p => new PromocionListDto
            {
                Id = p.Id,
                Nombre = p.ProductoId != null
                    ? (_context.Productos.Where(prod => prod.Id == p.ProductoId).Select(prod => prod.Nombre).FirstOrDefault() ?? p.Nombre)
                    : p.Nombre,
                ProductoId = p.ProductoId,
                Genero = p.Genero,
                ProductoNombre = p.ProductoId == null
                    ? null
                    : _context.Productos.Where(prod => prod.Id == p.ProductoId).Select(prod => prod.Nombre).FirstOrDefault(),
                ProductoImagenUrl = p.ProductoId == null
                    ? null
                    : _context.Productos.Where(prod => prod.Id == p.ProductoId).Select(prod => prod.ImagenUrl).FirstOrDefault(),
                ProductoEditorial = p.ProductoId == null
                    ? null
                    : _context.Productos.Where(prod => prod.Id == p.ProductoId).Select(prod => prod.Editorial).FirstOrDefault(),
                FechaInicio = p.FechaInicio,
                FechaFin = p.FechaFin,
                Activa = p.Activa
            })
            .ToListAsync();
    }

    public async Task DesactivarAsync(int id)
    {
        var promo = await _context.Promociones.FirstOrDefaultAsync(x => x.Id == id);
        if (promo == null) throw new InvalidOperationException("Promocion inexistente.");
        promo.Activa = false;
        await _context.SaveChangesAsync();
    }

    public async Task<int> DesactivarPorProductoAsync(int productoId)
    {
        var promos = await _context.Promociones
            .Where(x => x.Activa && x.ProductoId == productoId)
            .ToListAsync();

        if (promos.Count == 0) return 0;
        promos.ForEach(x => x.Activa = false);
        await _context.SaveChangesAsync();
        return promos.Count;
    }

    public async Task<int> DesactivarPorEditorialAsync(string editorial)
    {
        if (string.IsNullOrWhiteSpace(editorial)) throw new InvalidOperationException("Editorial requerida.");
        var edNorm = editorial.Trim().ToLower();

        var productoIds = await _context.Productos
            .Where(p => p.Editorial != null && p.Editorial.ToLower() == edNorm)
            .Select(p => p.Id)
            .ToListAsync();

        if (productoIds.Count == 0) return 0;

        var promos = await _context.Promociones
            .Where(x => x.Activa && x.ProductoId != null && productoIds.Contains(x.ProductoId.Value))
            .ToListAsync();

        if (promos.Count == 0) return 0;
        promos.ForEach(x => x.Activa = false);
        await _context.SaveChangesAsync();
        return promos.Count;
    }

    public async Task<int> DesactivarPorGeneroAsync(string genero)
    {
        if (string.IsNullOrWhiteSpace(genero)) throw new InvalidOperationException("Genero requerido.");
        var genNorm = genero.Trim().ToLower();

        var promos = await _context.Promociones
            .Where(x => x.Activa && x.Genero != null && x.Genero.ToLower() == genNorm)
            .ToListAsync();

        if (promos.Count == 0) return 0;
        promos.ForEach(x => x.Activa = false);
        await _context.SaveChangesAsync();
        return promos.Count;
    }
}
