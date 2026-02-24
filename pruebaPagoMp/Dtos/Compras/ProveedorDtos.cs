namespace pruebaPagoMp.Dtos.Compras;

public class ProveedorCreateDto
{
    public string RazonSocial { get; set; } = null!;
    public string CUIT { get; set; } = null!;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public bool Activo { get; set; } = true;
}

public class ProveedorListDto
{
    public int Id { get; set; }
    public string RazonSocial { get; set; } = null!;
    public string CUIT { get; set; } = null!;
    public bool Activo { get; set; }
}