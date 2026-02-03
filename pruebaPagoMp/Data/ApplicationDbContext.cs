using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Models;
using pruebaPagoMp.Models.Interfaces;
using pruebaPagoMp.Data.Configurations;
using pruebaPagoMp.Models.Ventas;

namespace pruebaPagoMp.Data;

public partial class ApplicationDbContext : DbContext
{

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {}

    public virtual DbSet<Carrito> Carritos { get; set; }
    public virtual DbSet<CarritoItem> CarritoItems { get; set; }
    public virtual DbSet<Pago> Pagos { get; set; }
    public virtual DbSet<Pedido> Pedidos { get; set; }
    public virtual DbSet<PedidoItem> PedidoItems { get; set; }
    public virtual DbSet<Producto> Productos { get; set; }
    public virtual DbSet<Usuario> Usuarios { get; set; }
    public virtual DbSet<WebhookLog> WebhookLogs { get; set; }
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();
    public DbSet<Bitacora> Bitacoras => Set<Bitacora>();
    public DbSet<DigitoVerificador> DigitosVerificadores => Set<DigitoVerificador>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Venta> Ventas { get; set; } = null!;
    public DbSet<DetalleVenta> DetalleVentas { get; set; } = null!;


    

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DigitoVerificador>(entity =>
        {
            entity.HasKey(d => d.Tabla);
        });

        modelBuilder.Entity<Carrito>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Carritos__3214EC072DF4D53C");

            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Carritos)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_Carritos_Usuarios");
        });

        modelBuilder.Entity<CarritoItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CarritoI__3214EC07C3AB00BA");

            entity.HasIndex(e => new { e.CarritoId, e.ProductoId }, "UX_CarritoItems_CarritoProducto").IsUnique();

            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Carrito).WithMany(p => p.CarritoItems)
                .HasForeignKey(d => d.CarritoId)
                .HasConstraintName("FK_CarritoItems_Carritos");

            entity.HasOne(d => d.Producto).WithMany(p => p.CarritoItems)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CarritoItems_Productos");
        });

        modelBuilder.Entity<Pago>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Pagos__3214EC07C2EF749E");

            entity.HasIndex(e => e.MercadoPagoPaymentId, "UQ_Pagos_MPPaymentId").IsUnique();

            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FechaRecepcion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.MercadoPagoPaymentId)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Monto).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TipoPago)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Pedido).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.PedidoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pagos_Pedidos");
        });

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Pedidos__3214EC075925E155");

            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FechaActualizacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.MercadoPagoPaymentId)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.MercadoPagoPreferenceId)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Moneda)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MontoTotal).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ReferenciaExterna)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Usuario).WithMany(p => p.Pedidos)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_Pedidos_Usuarios");
        });

        modelBuilder.Entity<PedidoItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PedidoIt__3214EC071CFA16DA");

            entity.HasIndex(e => new { e.PedidoId, e.ProductoId }, "UX_PedidoItems_PedidoProducto").IsUnique();

            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Pedido).WithMany(p => p.PedidoItems)
                .HasForeignKey(d => d.PedidoId)
                .HasConstraintName("FK_PedidoItems_Pedidos");

            entity.HasOne(d => d.Producto).WithMany(p => p.PedidoItems)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PedidoItems_Productos");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Producto__3214EC070E6620A2");

            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImagenUrl)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Precio).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usuarios__3214EC0717FCF778");

            entity.HasIndex(e => e.Email, "UX_Usuarios_Email").IsUnique();

            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NombreCompleto)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<UsuarioRol>(entity =>
        {
            entity.HasKey(ur => new { ur.UsuarioId, ur.RolId });
        });

        modelBuilder.Entity<WebhookLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__WebhookL__3214EC07425E907C");

            entity.Property(e => e.FechaRecepcion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Nota)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.TipoEvento)
                .HasMaxLength(100)
                .IsUnicode(false);
        });


        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new VentaConfiguration());
        modelBuilder.ApplyConfiguration(new DetalleVentaConfiguration());
    }




    public override int SaveChanges()
    {
        AplicarAuditoria();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AplicarAuditoria();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void AplicarAuditoria()
    {
        var ahora = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.FechaCreacion = ahora;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.FechaActualizacion = ahora;
            }
        }
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
