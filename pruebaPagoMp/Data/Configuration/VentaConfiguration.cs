using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using pruebaPagoMp.Models.Ventas;

namespace pruebaPagoMp.Data.Configurations;

public class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> builder)
    {
        builder.ToTable("Ventas");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Total)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(v => v.EstadoVenta)
            .IsRequired();

        builder.Property(v => v.Canal)
            .IsRequired();

        builder.Property(v => v.Fecha)
            .IsRequired();

        builder.HasOne(v => v.Usuario)
            .WithMany() // si querés, podés agregar ICollection<Venta> en Usuario y cambiarlo a .WithMany(u => u.Ventas)
            .HasForeignKey(v => v.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(v => v.Detalles)
            .WithOne(d => d.Venta)
            .HasForeignKey(d => d.VentaId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Property(v => v.MercadoPagoPreferenceId)
            .HasMaxLength(200);

        builder.Property(v => v.MercadoPagoUrlPago)
            .HasMaxLength(500);

        builder.Property(v => v.MercadoPagoPaymentId)
            .HasMaxLength(200);

        builder.Property(v => v.MercadoPagoEstado)
            .HasMaxLength(50);

    }
}
