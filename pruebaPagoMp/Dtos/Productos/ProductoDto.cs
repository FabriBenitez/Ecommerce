namespace pruebaPagoMp.DTOs
{
    public class ProductoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public string? ImagenUrl { get; set; }
        public bool TienePromocionActiva { get; set; }
        public string? PromocionNombre { get; set; }
        public decimal? PorcentajeDescuento { get; set; }
        public decimal? MontoDescuento { get; set; }
        public decimal? PrecioFinal { get; set; }
    }
}
