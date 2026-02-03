namespace pruebaPagoMp.DTOs
{
    public class AgregarItemDto
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public int? UsuarioId { get; set; } 
    }
}