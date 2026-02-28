using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace pruebaPagoMp.Models;

public class Usuario
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;
    public string NombreCompleto { get; set; } = null!;
    public string? Telefono { get; set; }
    public string? Dni { get; set; }
    public string? DniHash { get; set; }

    // Seguridad
    public string PasswordHash { get; set; } = null!;
    public bool Activo { get; set; } = true;

    // Auditoría
    public DateTime FechaCreacion { get; set; }
    public DateTime? UltimoLogin { get; set; }

    //  Integridad
    public string DigitoVerificador { get; set; } = null!;

    // Relaciones
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
    public ICollection<Carrito> Carritos { get; set; } = new List<Carrito>();
    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
}

