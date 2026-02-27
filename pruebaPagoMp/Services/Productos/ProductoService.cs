using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.DTOs;
using pruebaPagoMp.Models;

namespace pruebaPagoMp.Services
{
    public class ProductoService : IProductoService
    {
        private readonly ApplicationDbContext _context;

        public ProductoService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<IEnumerable<ProductoDto>> GetCatalogoAsync()
        {
            var hoy = DateTime.Today;

            // Buscamos los productos y calculamos su promo activa (si existe) para el catálogo.
            return await _context.Productos
                .Select(p => new
                {
                    Producto = p,
                    Promocion = _context.Promociones
                        .Where(pr =>
                            pr.Activa &&
                            (pr.ProductoId == null || pr.ProductoId == p.Id) &&
                            pr.FechaInicio.Date <= hoy &&
                            pr.FechaFin.Date >= hoy)
                        .OrderByDescending(pr => pr.ProductoId == p.Id)
                        .ThenByDescending(pr => pr.PorcentajeDescuento ?? 0m)
                        .ThenByDescending(pr => pr.MontoDescuento ?? 0m)
                        .FirstOrDefault()
                })
                .Select(x => new ProductoDto
                {
                    Id = x.Producto.Id,
                    Nombre = x.Producto.Nombre,
                    Descripcion = x.Producto.Descripcion,
                    Precio = x.Producto.Precio,
                    Stock = x.Producto.Stock,
                    ImagenUrl = x.Producto.ImagenUrl,
                    TienePromocionActiva = x.Promocion != null,
                    PromocionNombre = x.Promocion != null ? x.Promocion.Nombre : null,
                    PorcentajeDescuento = x.Promocion != null ? x.Promocion.PorcentajeDescuento : null,
                    MontoDescuento = x.Promocion != null ? x.Promocion.MontoDescuento : null,
                    PrecioFinal = x.Promocion == null
                        ? null
                        : (
                            x.Producto.Precio
                            - ((x.Promocion.PorcentajeDescuento ?? 0m) / 100m * x.Producto.Precio)
                            - (x.Promocion.MontoDescuento ?? 0m)
                          ) < 0m
                            ? 0m
                            : (
                                x.Producto.Precio
                                - ((x.Promocion.PorcentajeDescuento ?? 0m) / 100m * x.Producto.Precio)
                                - (x.Promocion.MontoDescuento ?? 0m)
                              )
                })
                .ToListAsync();
        }
    }
}
