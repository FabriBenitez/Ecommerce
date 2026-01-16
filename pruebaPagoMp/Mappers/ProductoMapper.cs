using pruebaPagoMp.DTOs;
using pruebaPagoMp.Models;

public static class ProductoMapper
{
    public static ProductoDto ToDto(this Producto producto)
    {
        return new ProductoDto
        {
            Id = producto.Id,
            Nombre = producto.Nombre,
            Precio = producto.Precio
        };
    }
}