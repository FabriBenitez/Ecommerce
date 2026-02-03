using System;
using System.Collections.Generic;

namespace pruebaPagoMp.Models;

public partial class Pago
{
    public int Id { get; set; }

    public int PedidoId { get; set; }

    public string MercadoPagoPaymentId { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public string? TipoPago { get; set; }

    public decimal Monto { get; set; }

    public string? RespuestaCompletaJson { get; set; }

    public DateTime FechaRecepcion { get; set; }

    public virtual Pedido Pedido { get; set; } = null!;
}
