using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }
}

