namespace pruebaPagoMp.DTOs
{
    public class PedidoDetalleDto
    {
        public int Id { get; set; }
        public decimal MontoTotal { get; set; }
        public string Estado { get; set; } = string.Empty;
        public List<PedidoItemDto> Items { get; set; } = new();
    }
}