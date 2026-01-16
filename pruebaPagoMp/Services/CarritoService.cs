using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.DTOs;
using pruebaPagoMp.Models;

namespace pruebaPagoMp.Services
{
    public class CarritoService : ICarritoService
    {
        private readonly ApplicationDbContext _context;

        public CarritoService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<bool> AgregarProductoAsync(AgregarItemDto itemDto)
        {
            // 1. Buscar si el usuario ya tiene un carrito (o crear uno)
            var carrito = await _context.Carritos
                .FirstOrDefaultAsync(c => c.UsuarioId == itemDto.UsuarioId);

            if (carrito == null)
            {
                carrito = new Carrito { 
                    UsuarioId = itemDto.UsuarioId, 
                    FechaCreacion = DateTime.Now 
                };
                _context.Carritos.Add(carrito);
                await _context.SaveChangesAsync();
            }

            // 2. Buscar si el producto ya existe en el carrito
            var itemExistente = await _context.CarritoItems
                .FirstOrDefaultAsync(ci => ci.CarritoId == carrito.Id && ci.ProductoId == itemDto.ProductoId);

            if (itemExistente != null)
            {
                itemExistente.Cantidad += itemDto.Cantidad;
            }
            else
            {
                // Buscamos el precio actual del producto
                var producto = await _context.Productos.FindAsync(itemDto.ProductoId);
                if (producto == null) return false;

                _context.CarritoItems.Add(new CarritoItem
                {
                    CarritoId = carrito.Id,
                    ProductoId = itemDto.ProductoId,
                    Cantidad = itemDto.Cantidad,
                    PrecioUnitario = producto.Precio,
                    FechaCreacion = DateTime.Now
                });
            }

            return await _context.SaveChangesAsync() > 0;
        }

    }
}