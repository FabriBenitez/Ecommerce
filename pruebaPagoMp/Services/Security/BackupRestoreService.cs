using Microsoft.EntityFrameworkCore;
using pruebaPagoMp.Data;

namespace pruebaPagoMp.Security;

public class BackupRestoreService : IBackupRestoreService
{
    private readonly ApplicationDbContext _context;
    private readonly IDigitoVerificadorService _dvService;

    public BackupRestoreService(ApplicationDbContext context, IDigitoVerificadorService dvService)
    {
        _context = context;
        _dvService = dvService;
    }

    public async Task<BackupPayload> ExportAsync()
    {
        return new BackupPayload
        {
            GeneratedAtUtc = DateTime.UtcNow,
            Roles = await _context.Roles.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Usuarios = await _context.Usuarios.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            UsuarioRoles = await _context.UsuarioRoles.AsNoTracking().OrderBy(x => x.UsuarioId).ThenBy(x => x.RolId).ToListAsync(),
            Productos = await _context.Productos.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Proveedores = await _context.Proveedores.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Compras = await _context.Compras.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            DetalleCompras = await _context.DetalleCompras.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            FacturasProveedor = await _context.FacturasProveedor.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Ventas = await _context.Ventas.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            DetalleVentas = await _context.DetalleVentas.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            VentaPagos = await _context.VentaPagos.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Cajas = await _context.Cajas.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            MovimientosCaja = await _context.MovimientosCaja.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            NotasCredito = await _context.NotasCredito.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            BitacoraEntries = await _context.BitacoraEntries.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            DigitosVerificadores = await _context.DigitosVerificadores.AsNoTracking().OrderBy(x => x.Tabla).ToListAsync()
        };
    }

    public async Task ImportAsync(BackupPayload payload)
    {
        if (payload == null) throw new InvalidOperationException("Payload de backup vacío.");
        if (string.IsNullOrWhiteSpace(payload.Version)) throw new InvalidOperationException("Backup sin versión.");

        await using var tx = await _context.Database.BeginTransactionAsync();

        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [Pagos]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [PedidoItems]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [Pedidos]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [CarritoItems]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [Carritos]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [WebhookLogs]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [Promociones]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [UsuarioRoles]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [MovimientosCaja]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [DetalleVentas]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [VentaPagos]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [Ventas]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [DetalleCompras]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [FacturasProveedor]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [Compras]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [Cajas]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [NotasCredito]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [Proveedores]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [Productos]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [RefreshTokens]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [BitacoraEntries]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [DigitosVerificadores]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [Usuarios]");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [Roles]");

        await InsertWithIdentityAsync("Roles", payload.Roles);
        await InsertWithIdentityAsync("Usuarios", payload.Usuarios);
        if (payload.UsuarioRoles.Count > 0)
        {
            _context.UsuarioRoles.AddRange(payload.UsuarioRoles);
            await _context.SaveChangesAsync();
        }

        await InsertWithIdentityAsync("Productos", payload.Productos);
        await InsertWithIdentityAsync("Proveedores", payload.Proveedores);
        await InsertWithIdentityAsync("Compras", payload.Compras);
        await InsertWithIdentityAsync("DetalleCompras", payload.DetalleCompras);
        await InsertWithIdentityAsync("FacturasProveedor", payload.FacturasProveedor);
        await InsertWithIdentityAsync("Ventas", payload.Ventas);
        await InsertWithIdentityAsync("DetalleVentas", payload.DetalleVentas);
        await InsertWithIdentityAsync("VentaPagos", payload.VentaPagos);
        await InsertWithIdentityAsync("Cajas", payload.Cajas);
        await InsertWithIdentityAsync("MovimientosCaja", payload.MovimientosCaja);
        await InsertWithIdentityAsync("NotasCredito", payload.NotasCredito);
        await InsertWithIdentityAsync("BitacoraEntries", payload.BitacoraEntries);

        if (payload.DigitosVerificadores.Count > 0)
        {
            _context.DigitosVerificadores.AddRange(payload.DigitosVerificadores);
            await _context.SaveChangesAsync();
        }

        await tx.CommitAsync();

        await _dvService.RecalcularDVVAsync("Usuarios");
        await _dvService.RecalcularDVVAsync("Proveedores");
        await _dvService.RecalcularDVVAsync("Compras");
        await _dvService.RecalcularDVVAsync("Ventas");
        await _dvService.RecalcularDVVAsync("MovimientosCaja");
    }

    private async Task InsertWithIdentityAsync<T>(string tableName, List<T> rows) where T : class
    {
        if (rows.Count == 0) return;

        await _context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{tableName}] ON");
        _context.Set<T>().AddRange(rows);
        await _context.SaveChangesAsync();
        await _context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{tableName}] OFF");
    }
}
