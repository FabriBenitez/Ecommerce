using System;
using System.Collections.Generic;

namespace pruebaPagoMp.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string NombreCompleto { get; set; } = null!;

    public string? Telefono { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<Carrito> Carritos { get; set; } = new List<Carrito>();

    public virtual ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
}
