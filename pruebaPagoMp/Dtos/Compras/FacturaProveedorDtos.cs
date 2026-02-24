namespace pruebaPagoMp.Dtos.Compras;

public class RegistrarFacturaProveedorDto
{
    public string Numero { get; set; } = null!;
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
}