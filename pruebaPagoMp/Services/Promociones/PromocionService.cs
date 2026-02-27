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
        if (string.IsNullOrWhiteSpace(dto.Nombre)) throw new InvalidOperationException("Nombre requerido.");
        if ((!dto.PorcentajeDescuento.HasValue || dto.PorcentajeDescuento.Value <= 0m)
            && (!dto.MontoDescuento.HasValue || dto.MontoDescuento.Value <= 0m))
        {
            throw new InvalidOperationException("Debe indicar un descuento valido (porcentaje o monto fijo).");
        }

        var fechaInicio = dto.FechaInicio.Date;
        var fechaFin = dto.FechaFin.Date.AddDays(1).AddTicks(-1);
        if (fechaFin < fechaInicio) throw new InvalidOperationException("FechaFin no puede ser menor a FechaInicio.");

        if (dto.ProductoId.HasValue)
        {
            var existeProducto = await _context.Productos.AnyAsync(p => p.Id == dto.ProductoId.Value);
            if (!existeProducto) throw new InvalidOperationException("El producto seleccionado no existe.");
        }

        var promo = new Promocion
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion,
            ProductoId = dto.ProductoId,
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
                Nombre = p.Nombre,
                ProductoId = p.ProductoId,
                ProductoNombre = p.ProductoId == null
                    ? null
                    : _context.Productos.Where(prod => prod.Id == p.ProductoId).Select(prod => prod.Nombre).FirstOrDefault(),
                FechaInicio = p.FechaInicio,
                FechaFin = p.FechaFin,
                Activa = p.Activa
            })
            .ToListAsync();
    }
}
