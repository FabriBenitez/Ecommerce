using System;
using System.Collections.Generic;
using pruebaPagoMp.Models.Interfaces;


namespace pruebaPagoMp.Models;

public partial class Pedido : IAuditable
{
    public int Id { get; set; }
    public int? UsuarioId { get; set; }
    public string ReferenciaExterna { get; set; } = null!;
    public decimal MontoTotal { get; set; }
    public string Moneda { get; set; } = null!;
    public string Estado { get; set; } = null!;
    public string? MercadoPagoPreferenceId { get; set; }
    public string? MercadoPagoPaymentId { get; set; }
    
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public int? CreadoPorUsuarioId { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }

    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    public virtual ICollection<PedidoItem> PedidoItems { get; set; } = new List<PedidoItem>();
    public virtual Usuario? Usuario { get; set; }
}
