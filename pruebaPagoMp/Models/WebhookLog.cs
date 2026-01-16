using System;
using System.Collections.Generic;

namespace pruebaPagoMp.Models;

public partial class WebhookLog
{
    public int Id { get; set; }

    public string TipoEvento { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public DateTime FechaRecepcion { get; set; }

    public bool Procesado { get; set; }

    public string? Nota { get; set; }
}
