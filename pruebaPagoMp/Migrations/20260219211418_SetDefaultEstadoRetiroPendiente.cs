using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pruebaPagoMp.Migrations
{
    /// <inheritdoc />
    public partial class SetDefaultEstadoRetiroPendiente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE V
SET V.EstadoRetiro = 1
FROM Ventas V
WHERE V.EstadoRetiro = 0;
");

            migrationBuilder.Sql(@"
DECLARE @ConstraintName nvarchar(200);
SELECT @ConstraintName = dc.name
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
INNER JOIN sys.tables t ON t.object_id = c.object_id
WHERE t.name = 'Ventas' AND c.name = 'EstadoRetiro';

IF @ConstraintName IS NOT NULL
    EXEC('ALTER TABLE [Ventas] DROP CONSTRAINT [' + @ConstraintName + ']');

ALTER TABLE [Ventas] ADD CONSTRAINT [DF_Ventas_EstadoRetiro] DEFAULT 1 FOR [EstadoRetiro];
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('DF_Ventas_EstadoRetiro', 'D') IS NOT NULL
    ALTER TABLE [Ventas] DROP CONSTRAINT [DF_Ventas_EstadoRetiro];
");
        }
    }
}
