using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pruebaPagoMp.Migrations
{
    /// <inheritdoc />
    public partial class BackfillProductoTemporalSenia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE p
SET p.EsTemporalSenia = 1
FROM Productos p
WHERE p.Stock = 0
  AND p.Descripcion = 'Item generado desde señas (a conseguir).'
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE p
SET p.EsTemporalSenia = 0
FROM Productos p
WHERE p.Stock = 0
  AND p.Descripcion = 'Item generado desde señas (a conseguir).'
");
        }
    }
}
