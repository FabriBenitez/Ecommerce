using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pruebaPagoMp.Services.Promociones;

namespace pruebaPagoMp.Controllers;

[ApiController]
[Route("api/promociones/cliente")]
[Authorize]
public class PromocionesClienteController : ControllerBase
{
    private readonly IPromocionService _service;

    public PromocionesClienteController(IPromocionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> ListarActivas()
    {
        var data = await _service.ListarAsync(true);
        return Ok(data);
    }
}
