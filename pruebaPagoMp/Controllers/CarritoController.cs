using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Dtos.Carritos;
using pruebaPagoMp.Helpers;
using pruebaPagoMp.Services.Carritos;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/carritos")]
[Authorize]
public class CarritosController : ControllerBase
{
    private readonly ICarritoService _carritoService;

    public CarritosController(ICarritoService carritoService)
    {
        _carritoService = carritoService;
    }

    [HttpGet("actual")]
    public async Task<IActionResult> ObtenerCarritoActual()
    {
        var usuarioId = UserClaimsHelper.ObtenerUsuarioId(User);
        var carrito = await _carritoService.ObtenerCarritoActualAsync(usuarioId);
        return Ok(carrito);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AgregarCarritoItem([FromBody] AgregarCarritoItemDto dto)
    {
        var usuarioId = UserClaimsHelper.ObtenerUsuarioId(User);
        var carrito = await _carritoService.AgregarCarritoItemAsync(usuarioId, dto);
        return Ok(carrito);
    }

    [HttpPut("items/{itemId:int}")]
    public async Task<IActionResult> ActualizarCantidadCarritoItem([FromRoute] int itemId, [FromBody] ActualizarCantidadDto dto)
    {
        var usuarioId = UserClaimsHelper.ObtenerUsuarioId(User);
        var carrito = await _carritoService.ActualizarCantidadCarritoItemAsync(usuarioId, itemId, dto.Cantidad);
        return Ok(carrito);
    }

    [HttpDelete("items/{itemId:int}")]
    public async Task<IActionResult> EliminarCarritoItem([FromRoute] int itemId)
    {
        var usuarioId = UserClaimsHelper.ObtenerUsuarioId(User);
        var carrito = await _carritoService.EliminarCarritoItemAsync(usuarioId, itemId);
        return Ok(carrito);
    }
}
