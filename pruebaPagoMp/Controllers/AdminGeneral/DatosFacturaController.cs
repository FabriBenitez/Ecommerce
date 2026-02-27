using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.Dtos.Admin.Facturacion;
using pruebaPagoMp.Models.Administracion;
using pruebaPagoMp.Models.Bitacora;
using pruebaPagoMp.Services.Bitacora;

namespace pruebaPagoMp.Controllers.AdminGeneral;

[ApiController]
[Route("api/facturacion-config")]
public class DatosFacturaController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IBitacoraService _bitacoraService;

    public DatosFacturaController(ApplicationDbContext context, IBitacoraService bitacoraService)
    {
        _context = context;
        _bitacoraService = bitacoraService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Obtener()
    {
        var cfg = await ObtenerOCrearAsync();
        return Ok(Map(cfg));
    }

    [HttpPut]
    [Authorize(Roles = "AdminGeneral")]
    public async Task<IActionResult> Actualizar([FromBody] DatosFacturaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NombreComercial)) throw new InvalidOperationException("Nombre comercial requerido.");
        if (string.IsNullOrWhiteSpace(dto.TituloComprobante)) throw new InvalidOperationException("Titulo requerido.");
        if (string.IsNullOrWhiteSpace(dto.Direccion)) throw new InvalidOperationException("Direccion requerida.");
        if (string.IsNullOrWhiteSpace(dto.Telefono)) throw new InvalidOperationException("Telefono requerido.");
        if (string.IsNullOrWhiteSpace(dto.Email)) throw new InvalidOperationException("Email requerido.");
        if (string.IsNullOrWhiteSpace(dto.MensajeAgradecimiento)) throw new InvalidOperationException("Mensaje requerido.");

        var cfg = await ObtenerOCrearAsync();
        cfg.NombreComercial = dto.NombreComercial.Trim();
        cfg.TituloComprobante = dto.TituloComprobante.Trim();
        cfg.Direccion = dto.Direccion.Trim();
        cfg.Telefono = dto.Telefono.Trim();
        cfg.Email = dto.Email.Trim();
        cfg.MensajeAgradecimiento = dto.MensajeAgradecimiento.Trim();

        await _context.SaveChangesAsync();

        await _bitacoraService.RegistrarAsync(new BitacoraEntry
        {
            Accion = "FACTURA_DATOS_ACTUALIZAR",
            Detalle = $"Datos de factura actualizados para {cfg.NombreComercial}.",
            Resultado = "OK"
        });

        return Ok(Map(cfg));
    }

    private async Task<DatosFacturaEmpresa> ObtenerOCrearAsync()
    {
        var cfg = await _context.DatosFacturaEmpresas.FirstOrDefaultAsync();
        if (cfg != null) return cfg;

        cfg = new DatosFacturaEmpresa();
        _context.DatosFacturaEmpresas.Add(cfg);
        await _context.SaveChangesAsync();
        return cfg;
    }

    private static DatosFacturaDto Map(DatosFacturaEmpresa cfg)
    {
        return new DatosFacturaDto
        {
            NombreComercial = cfg.NombreComercial,
            TituloComprobante = cfg.TituloComprobante,
            Direccion = cfg.Direccion,
            Telefono = cfg.Telefono,
            Email = cfg.Email,
            MensajeAgradecimiento = cfg.MensajeAgradecimiento
        };
    }
}
