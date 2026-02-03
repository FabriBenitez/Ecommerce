using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using pruebaPagoMp.Models.Ventas;

namespace pruebaPagoMp.Data.Configurations;

public class DetalleVentaConfiguration : IEntityTypeConfiguration<DetalleVenta>
{
    public void Configure(EntityTypeBuilder<DetalleVenta> builder)
    {
        builder.ToTable("DetalleVentas");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Cantidad)
            .IsRequired();

        builder.Property(d => d.PrecioUnitario)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(d => d.Subtotal)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasOne(d => d.Producto)
            .WithMany() // opcional: Producto.DetallesVenta
            .HasForeignKey(d => d.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
