using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pruebaPagoMp.Migrations
{
    /// <inheritdoc />
    public partial class PresencialPagosCaja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Referencia",
                table: "MovimientosCaja",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Referencia",
                table: "MovimientosCaja");
        }
    }
}
