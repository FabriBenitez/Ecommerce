using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.Dtos.Carritos;
using pruebaPagoMp.Models;

namespace pruebaPagoMp.Services.Carritos;

public class CarritoService : ICarritoService
{
    private readonly ApplicationDbContext _context;

    public CarritoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CarritoDto> ObtenerCarritoActualAsync(int usuarioId)
    {
        var carrito = await ObtenerOCrearCarritoAsync(usuarioId);
        return await MapearCarritoAsync(carrito.Id);
    }

    public async Task<CarritoDto> AgregarCarritoItemAsync(int usuarioId, AgregarCarritoItemDto dto)
    {
        if (dto.Cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a 0.");

        var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == dto.ProductoId);
        if (producto == null)
            throw new KeyNotFoundException("Producto no encontrado.");

        if (producto.Stock < dto.Cantidad)
            throw new InvalidOperationException("Stock insuficiente.");

        var carrito = await ObtenerOCrearCarritoAsync(usuarioId);

        var item = await _context.CarritoItems
            .FirstOrDefaultAsync(ci => ci.CarritoId == carrito.Id && ci.ProductoId == dto.ProductoId);

        if (item == null)
        {
            item = new CarritoItem
            {
                CarritoId = carrito.Id,
                ProductoId = dto.ProductoId,
                Cantidad = dto.Cantidad,
                PrecioUnitario = producto.Precio
            };

            _context.CarritoItems.Add(item);
        }
        else
        {
            var nuevaCantidad = item.Cantidad + dto.Cantidad;

            if (producto.Stock < nuevaCantidad)
                throw new InvalidOperationException("Stock insuficiente para la cantidad total solicitada.");

            item.Cantidad = nuevaCantidad;

        }

        await _context.SaveChangesAsync();
        return await MapearCarritoAsync(carrito.Id);
    }

    public async Task<CarritoDto> ActualizarCantidadCarritoItemAsync(int usuarioId, int itemId, int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a 0.");

        var item = await _context.CarritoItems
            .Include(ci => ci.Carrito)
            .FirstOrDefaultAsync(ci => ci.Id == itemId);

        if (item == null)
            throw new KeyNotFoundException("Item del carrito no encontrado.");

        if (item.Carrito.UsuarioId != usuarioId)
            throw new UnauthorizedAccessException("No podés modificar un carrito que no es tuyo.");

        var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == item.ProductoId);
        if (producto == null)
            throw new KeyNotFoundException("Producto no encontrado.");

        if (producto.Stock < cantidad)
            throw new InvalidOperationException("Stock insuficiente.");

        item.Cantidad = cantidad;

        await _context.SaveChangesAsync();
        return await MapearCarritoAsync(item.CarritoId);
    }

    public async Task<CarritoDto> EliminarCarritoItemAsync(int usuarioId, int itemId)
    {
        var item = await _context.CarritoItems
            .Include(ci => ci.Carrito)
            .FirstOrDefaultAsync(ci => ci.Id == itemId);

        if (item == null)
            throw new KeyNotFoundException("Item del carrito no encontrado.");

        if (item.Carrito.UsuarioId != usuarioId)
            throw new UnauthorizedAccessException("No podés modificar un carrito que no es tuyo.");

        _context.CarritoItems.Remove(item);
        await _context.SaveChangesAsync();

        return await MapearCarritoAsync(item.CarritoId);
    }

    // ------------------------
    // Helpers internos
    // ------------------------

    private async Task<Carrito> ObtenerOCrearCarritoAsync(int usuarioId)
    {
        // Buscar carrito existente del usuario
        var carrito = await _context.Carritos
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

        if (carrito != null)
            return carrito;

        // Crear carrito nuevo asociado al usuario
        carrito = new Carrito
        {
            UsuarioId = usuarioId
            // FechaCreacion ya se setea por default en DB (según tu config)
        };

        _context.Carritos.Add(carrito);
        await _context.SaveChangesAsync();

        return carrito;
    }

    private async Task<CarritoDto> MapearCarritoAsync(int carritoId)
    {
        var carrito = await _context.Carritos
            .Include(c => c.CarritoItems)
                .ThenInclude(ci => ci.Producto)
            .FirstAsync(c => c.Id == carritoId);

        var dto = new CarritoDto
        {
            CarritoId = carrito.Id,
            Items = carrito.CarritoItems.Select(ci => new CarritoItemDto
            {
                ItemId = ci.Id,
                ProductoId = ci.ProductoId,
                NombreProducto = ci.Producto?.Nombre ?? "",
                ImagenUrl = ci.Producto?.ImagenUrl,
                Cantidad = ci.Cantidad,
                PrecioUnitario = ci.PrecioUnitario,
                Subtotal = ci.PrecioUnitario * ci.Cantidad
            }).ToList()
        };

        dto.Total = dto.Items.Sum(i => i.Subtotal);
        return dto;
    }
}
