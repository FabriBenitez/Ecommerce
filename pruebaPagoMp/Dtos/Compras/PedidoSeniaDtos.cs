using pruebaPagoMp.Models.Ventas.Enums;

namespace pruebaPagoMp.Dtos.Compras;

public class PedidoSeniaListDto
{
    public int Id { get; set; }
    public int ReservaId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public EstadoPedidoSenia Estado { get; set; }
    public EstadoReserva EstadoReserva { get; set; }
    public string? ClienteDni { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteTelefono { get; set; }
    public decimal Total { get; set; }
    public decimal MontoSenia { get; set; }
    public decimal SaldoPendiente { get; set; }
    public int CantidadItems { get; set; }
    public string? ResumenItems { get; set; }
}

public class ActualizarEstadoPedidoSeniaDto
{
    public EstadoPedidoSenia Estado { get; set; }
}
