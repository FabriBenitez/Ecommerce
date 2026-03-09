using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using pruebaPagoMp.DTOs;
using pruebaPagoMp.Services;
using System.Text.RegularExpressions;

namespace pruebaPagoMp.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // La ruta será: api/productos
    public class ProductosController : ControllerBase
    {
        private readonly IProductoService _productoService;
        private readonly IWebHostEnvironment _env;

        public ProductosController(IProductoService productoService, IWebHostEnvironment env)
        {
            _productoService = productoService;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> GetCatalogo()
        {
            var productos = await _productoService.GetCatalogoAsync();
            return Ok(productos);
        }

        [HttpPost]
        [Authorize(Roles = "AdminCompras,AdminGeneral")]
        public async Task<IActionResult> Crear([FromForm] CrearProductoFormDto form)
        {
            try
            {
                if (form.Imagen == null || form.Imagen.Length == 0)
                    return BadRequest(new { error = "La imagen del libro es obligatoria." });

                var imagenUrl = await GuardarImagenAsync(form.Imagen);
                var dto = new CrearProductoDto
                {
                    Nombre = form.Nombre,
                    Descripcion = form.Descripcion,
                    Editorial = form.Editorial,
                    Precio = form.Precio,
                    Stock = form.Stock,
                    ImagenUrl = imagenUrl,
                    ProveedorId = form.ProveedorId
                };
                var id = await _productoService.CrearAsync(dto);
                return Ok(new { id });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("import-csv")]
        [Authorize(Roles = "AdminCompras,AdminGeneral")]
        public async Task<IActionResult> ImportCsv([FromForm] IFormFile file, [FromForm] int? proveedorId)
        {
            if (file == null || file.Length == 0) return BadRequest(new { error = "Archivo vacio." });
            try
            {
                await using var stream = file.OpenReadStream();
                var creados = await _productoService.ImportarArchivoAsync(stream, file.FileName, proveedorId);
                return Ok(new { creados });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "AdminCompras,AdminGeneral")]
        public async Task<IActionResult> Actualizar([FromRoute] int id, [FromForm] ActualizarProductoFormDto form)
        {
            try
            {
                string? imagenUrl = null;
                if (form.Imagen != null && form.Imagen.Length > 0)
                {
                    imagenUrl = await GuardarImagenAsync(form.Imagen);
                }

                var dto = new ActualizarProductoDto
                {
                    Nombre = form.Nombre,
                    Descripcion = form.Descripcion,
                    Editorial = form.Editorial,
                    Precio = form.Precio,
                    Stock = form.Stock,
                    ImagenUrl = imagenUrl,
                    ProveedorId = form.ProveedorId
                };

                await _productoService.ActualizarAsync(id, dto);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "AdminCompras,AdminGeneral")]
        public async Task<IActionResult> Eliminar([FromRoute] int id)
        {
            try
            {
                await _productoService.EliminarAsync(id);
                return Ok(new { ok = true });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task<string> GuardarImagenAsync(IFormFile imagen)
        {
            var root = _env.WebRootPath;
            if (string.IsNullOrWhiteSpace(root))
            {
                root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var folder = Path.Combine(root, "uploads", "productos");
            Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(imagen.FileName).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";

            var baseName = Path.GetFileNameWithoutExtension(imagen.FileName);
            baseName = Regex.Replace(baseName, "[^a-zA-Z0-9_-]+", "-").Trim('-');
            if (string.IsNullOrWhiteSpace(baseName)) baseName = "producto";

            var fileName = $"{baseName}-{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(folder, fileName);
            await using var fs = System.IO.File.Create(fullPath);
            await imagen.CopyToAsync(fs);

            return $"/uploads/productos/{fileName}";
        }

        public class CrearProductoFormDto
        {
            public string Nombre { get; set; } = string.Empty;
            public string? Descripcion { get; set; }
            public string? Editorial { get; set; }
            public decimal Precio { get; set; }
            public int Stock { get; set; }
            public IFormFile? Imagen { get; set; }
            public int? ProveedorId { get; set; }
        }

        public class ActualizarProductoFormDto
        {
            public string Nombre { get; set; } = string.Empty;
            public string? Descripcion { get; set; }
            public string? Editorial { get; set; }
            public decimal Precio { get; set; }
            public int Stock { get; set; }
            public IFormFile? Imagen { get; set; }
            public int? ProveedorId { get; set; }
        }
    }
}
