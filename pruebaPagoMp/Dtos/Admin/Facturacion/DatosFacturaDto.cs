namespace pruebaPagoMp.Dtos.Admin.Facturacion;

public class DatosFacturaDto
{
    public string NombreComercial { get; set; } = null!;
    public string TituloComprobante { get; set; } = null!;
    public string Direccion { get; set; } = null!;
    public string Telefono { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string MensajeAgradecimiento { get; set; } = null!;
}
