IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260120184554_InitialBaseline', N'7.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

EXEC sp_rename N'[RefreshTokens].[Revocado]', N'IsRevoked', N'COLUMN';
GO

EXEC sp_rename N'[RefreshTokens].[FechaExpiracion]', N'ExpiresAt', N'COLUMN';
GO

ALTER TABLE [RefreshTokens] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Pedidos]') AND [c].[name] = N'FechaActualizacion');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Pedidos] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Pedidos] ALTER COLUMN [FechaActualizacion] datetime NULL;
ALTER TABLE [Pedidos] ADD DEFAULT ((getdate())) FOR [FechaActualizacion];
GO

ALTER TABLE [Pedidos] ADD [ActualizadoPorUsuarioId] int NULL;
GO

ALTER TABLE [Pedidos] ADD [CreadoPorUsuarioId] int NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260202044818_AuditoriaBasica', N'7.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Ventas] (
    [Id] int NOT NULL IDENTITY,
    [Fecha] datetime2 NOT NULL,
    [UsuarioId] int NOT NULL,
    [Total] decimal(18,2) NOT NULL,
    [EstadoVenta] int NOT NULL,
    [Canal] int NOT NULL,
    [Observaciones] nvarchar(max) NULL,
    CONSTRAINT [PK_Ventas] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Ventas_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [DetalleVentas] (
    [Id] int NOT NULL IDENTITY,
    [VentaId] int NOT NULL,
    [ProductoId] int NOT NULL,
    [Cantidad] int NOT NULL,
    [PrecioUnitario] decimal(18,2) NOT NULL,
    [Subtotal] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_DetalleVentas] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DetalleVentas_Productos_ProductoId] FOREIGN KEY ([ProductoId]) REFERENCES [Productos] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DetalleVentas_Ventas_VentaId] FOREIGN KEY ([VentaId]) REFERENCES [Ventas] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_DetalleVentas_ProductoId] ON [DetalleVentas] ([ProductoId]);
GO

CREATE INDEX [IX_DetalleVentas_VentaId] ON [DetalleVentas] ([VentaId]);
GO

CREATE INDEX [IX_Ventas_UsuarioId] ON [Ventas] ([UsuarioId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260202201203_Fase2_Ventas_BloqueA', N'7.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260206200039_Venta_MercadoPagoRefs', N'7.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DetalleVentas]') AND [c].[name] = N'Subtotal');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [DetalleVentas] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [DetalleVentas] ALTER COLUMN [Subtotal] decimal(10,2) NOT NULL;
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DetalleVentas]') AND [c].[name] = N'PrecioUnitario');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [DetalleVentas] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [DetalleVentas] ALTER COLUMN [PrecioUnitario] decimal(10,2) NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260206201213_Fix_DetalleVenta_ProductoFK', N'7.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260206201858_Fix_DetalleVenta_ProductoId1', N'7.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260206203008_Fix_DetalleVenta_FK_Producto', N'7.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260206205038_Fix_DetalleVentaFK', N'7.0.17');
GO

COMMIT;
GO

