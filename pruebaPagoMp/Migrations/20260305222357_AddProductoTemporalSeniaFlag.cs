using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pruebaPagoMp.Migrations
{
    /// <inheritdoc />
    public partial class AddProductoTemporalSeniaFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsTemporalSenia",
                table: "Productos",
                type: "bit",
                nullable: false,
                defaultValue: false);

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
            migrationBuilder.DropColumn(
                name: "EsTemporalSenia",
                table: "Productos");
        }
    }
}
