using pruebaPagoMp.Models;
using pruebaPagoMp.Models.Bitacora;
using pruebaPagoMp.Models.Caja;
using pruebaPagoMp.Models.Compras;
using pruebaPagoMp.Models.Ventas;

namespace pruebaPagoMp.Security;

public class BackupPayload
{
    public string Version { get; set; } = "v1";
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public List<Usuario> Usuarios { get; set; } = new();
    public List<Rol> Roles { get; set; } = new();
    public List<UsuarioRol> UsuarioRoles { get; set; } = new();
    public List<Producto> Productos { get; set; } = new();
    public List<Proveedor> Proveedores { get; set; } = new();
    public List<Compra> Compras { get; set; } = new();
    public List<DetalleCompra> DetalleCompras { get; set; } = new();
    public List<FacturaProveedor> FacturasProveedor { get; set; } = new();
    public List<Venta> Ventas { get; set; } = new();
    public List<DetalleVenta> DetalleVentas { get; set; } = new();
    public List<VentaPago> VentaPagos { get; set; } = new();
    public List<Caja> Cajas { get; set; } = new();
    public List<MovimientoCaja> MovimientosCaja { get; set; } = new();
    public List<NotaCredito> NotasCredito { get; set; } = new();
    public List<BitacoraEntry> BitacoraEntries { get; set; } = new();
    public List<DigitoVerificador> DigitosVerificadores { get; set; } = new();
}
