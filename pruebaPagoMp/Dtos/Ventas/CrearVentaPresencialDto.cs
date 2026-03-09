using System.ComponentModel.DataAnnotations;

namespace pruebaPagoMp.Dtos.Ventas;

public class CrearVentaPresencialDto
{
    [Required(ErrorMessage = "El DNI es obligatorio.")]
    [StringLength(20, ErrorMessage = "El DNI no puede superar 20 caracteres.")]
    public string? ClienteDni { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(200, ErrorMessage = "El nombre no puede superar 200 caracteres.")]
    public string? ClienteNombre { get; set; }

    [Required(ErrorMessage = "El telefono es obligatorio.")]
    [StringLength(50, ErrorMessage = "El telefono no puede superar 50 caracteres.")]
    public string? ClienteTelefono { get; set; }

    public string? Observaciones { get; set; }

    public List<CrearVentaPresencialItemDto> Items { get; set; } = new();
    public List<PagoItemDto> Pagos { get; set; } = new();

}

public class CrearVentaPresencialItemDto
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
}
