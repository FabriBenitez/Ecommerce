using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;
using pruebaPagoMp.DTOs;
using pruebaPagoMp.Models;

namespace pruebaPagoMp.Services
{
    public class PedidoService : IPedidoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public PedidoService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        public async Task<PagoRespuestaDto> CrearPedidoYPreferenciaAsync(List<PedidoItemDto> items, int? usuarioId)
        {
            if (items == null || !items.Any()) 
                throw new Exception("El carrito enviado desde el frontend está vacío.");

            // 1. Crear el encabezado del Pedido usando los items del parámetro
            var nuevoPedido = new Pedido {
                UsuarioId = usuarioId,
                MontoTotal = items.Sum(i => i.Cantidad * i.PrecioUnitario),
                Estado = "Pendiente",
                Moneda = "ARS",
                ReferenciaExterna = Guid.NewGuid().ToString().Substring(0, 8),
                FechaCreacion = DateTime.Now,
                FechaActualizacion = DateTime.Now,
                MercadoPagoPreferenceId = "", 
                MercadoPagoPaymentId = ""
            };

            _context.Pedidos.Add(nuevoPedido);
            await _context.SaveChangesAsync();

            // 2. Guardar los items del pedido en la BD
            foreach (var item in items)
            {
                var pedidoItem = new PedidoItem {
                    PedidoId = nuevoPedido.Id,
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario
                };
                _context.PedidoItems.Add(pedidoItem);
            }
            await _context.SaveChangesAsync();

            // 3. Preparar llamada a Mercado Pago
            var url = "https://api.mercadopago.com/checkout/preferences";
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", "APP_USR-3933496451262572-121020-7be6a0a87e83b92c14305d86521a6c0d-1949249001");

            var body = new {
                items = items.Select(i => new {
                    title = i.NombreProducto, // Usamos el nombre que viene del front
                    quantity = i.Cantidad,
                    unit_price = (double)i.PrecioUnitario,
                    currency_id = "ARS"
                }).ToList(),
                external_reference = nuevoPedido.Id.ToString(),
                back_urls = new {
                    success = "https://www.google.com.ar",
                    failure = "https://www.google.com.ar",
                    pending = "https://www.google.com.ar"
                },
                auto_return = "approved" 
            };

            // ... resto del código de PostAsync y JsonDocument igual ...
            var response = await _httpClient.PostAsync(url, new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
            // (Mantener la lógica de lectura de 'init_point' que ya tenías)
            
            // ... Código para extraer preferenceId e initPoint ...
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(jsonResponse);
            string preferenceId = doc.RootElement.GetProperty("id").GetString() ?? "";
            string initPoint = doc.RootElement.GetProperty("init_point").GetString() ?? "";

            nuevoPedido.MercadoPagoPreferenceId = preferenceId;
            await _context.SaveChangesAsync();

            return new PagoRespuestaDto { UrlPago = initPoint, PreferenciaId = preferenceId };
        }
    }
}