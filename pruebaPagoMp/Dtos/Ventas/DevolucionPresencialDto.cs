namespace pruebaPagoMp.Dtos.Ventas;

public class DevolucionPresencialDto
{
    public string ClienteDni { get; set; } = null!;
    public string? ClienteNombre { get; set; } 
    public decimal MontoDevolver { get; set; }
    public bool GenerarNotaCredito { get; set; } = true;
    public string? Motivo { get; set; }  
    public List<DevolucionItemDto> Items { get; set; } = new();
}

public class DevolucionItemDto
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
}

