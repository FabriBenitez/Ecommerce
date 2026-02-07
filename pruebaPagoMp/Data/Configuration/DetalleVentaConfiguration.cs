using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using pruebaPagoMp.Models.Ventas;

namespace pruebaPagoMp.Data.Configurations
{
    public class DetalleVentaConfiguration : IEntityTypeConfiguration<DetalleVenta>
    {
        public void Configure(EntityTypeBuilder<DetalleVenta> entity)
        {
            entity.HasKey(d => d.Id);

            entity.Property(d => d.PrecioUnitario).HasColumnType("decimal(10, 2)");
            entity.Property(d => d.Subtotal).HasColumnType("decimal(10, 2)");

            /*entity.HasOne(d => d.Venta)
                .WithMany(v => v.Detalles)
                .HasForeignKey(d => d.VentaId)
                .OnDelete(DeleteBehavior.Cascade);*/

            entity.HasOne(d => d.Producto)
                .WithMany(p => p.DetallesVenta)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

