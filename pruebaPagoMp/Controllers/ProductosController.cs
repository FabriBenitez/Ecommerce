using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.DTOs;
using pruebaPagoMp.Services;

namespace pruebaPagoMp.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // La ruta será: api/productos
    public class ProductosController : ControllerBase
    {
        private readonly IProductoService _productoService;

        public ProductosController(IProductoService productoService)
        {
            _productoService = productoService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> GetCatalogo()
        {
            var productos = await _productoService.GetCatalogoAsync();
            return Ok(productos);
        }
    }
}