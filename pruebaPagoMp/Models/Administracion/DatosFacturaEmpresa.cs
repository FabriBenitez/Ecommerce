namespace pruebaPagoMp.Models.Administracion;

public class DatosFacturaEmpresa
{
    public int Id { get; set; }
    public string NombreComercial { get; set; } = "LIBRERIA X";
    public string TituloComprobante { get; set; } = "Comprobante de venta";
    public string Direccion { get; set; } = "Calle Falsa 123 - CABA";
    public string Telefono { get; set; } = "+54 11 4567-8901";
    public string Email { get; set; } = "contacto@libreriax.com";
    public string MensajeAgradecimiento { get; set; } = "Gracias por su compra en Libreria X";
}
