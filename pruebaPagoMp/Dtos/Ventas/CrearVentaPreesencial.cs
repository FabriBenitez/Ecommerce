namespace pruebaPagoMp.Dtos.Ventas;

public class CrearVentaPresencialDto
{
    public int UsuarioId { get; set; } // siempre lo guardás según tu decisión

    public string MetodoPago { get; set; } = "Efectivo"; // manual (Efectivo / Transferencia / Tarjeta / etc)
    public List<ItemVentaPresencialDto> Items { get; set; } = new();
}

public class ItemVentaPresencialDto
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
}
