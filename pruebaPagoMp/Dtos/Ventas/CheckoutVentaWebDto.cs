using System.ComponentModel.DataAnnotations;

namespace pruebaPagoMp.Dtos.Ventas;

public class CheckoutVentaWebDto
{
    [Required(ErrorMessage = "El DNI es obligatorio.")]
    [StringLength(20, ErrorMessage = "El DNI no puede superar 20 caracteres.")]
    public string ClienteDni { get; set; } = null!;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(200, ErrorMessage = "El nombre no puede superar 200 caracteres.")]
    public string? NombreEntrega { get; set; } = null;

    [Required(ErrorMessage = "El telefono es obligatorio.")]
    [StringLength(50, ErrorMessage = "El telefono no puede superar 50 caracteres.")]
    public string? TelefonoEntrega { get; set; } = null;
    public string? DireccionEntrega { get; set; } = null;
    public string? Ciudad { get; set; } = null;
    public string? Provincia { get; set; } = null;
    public string? CodigoPostal { get; set; } = null;
    public string? Observaciones { get; set; } = null;

}
