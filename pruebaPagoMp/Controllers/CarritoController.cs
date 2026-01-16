using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.DTOs;
using pruebaPagoMp.Services;

namespace pruebaPagoMp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CarritoController : ControllerBase
    {
        private readonly ICarritoService _carritoService;

        public CarritoController(ICarritoService carritoService)
        {
            _carritoService = carritoService;
        }

        [HttpPost("agregar")]
        public async Task<IActionResult> AgregarAlCarrito(AgregarItemDto itemDto)
        {
            var resultado = await _carritoService.AgregarProductoAsync(itemDto);
            if (resultado) return Ok(new { mensaje = "Producto agregado correctamente" });
            return BadRequest("No se pudo agregar el producto");
        }
    }
}