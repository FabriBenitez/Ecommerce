using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pruebaPagoMp.Migrations
{
    /// <inheritdoc />
    public partial class AddDniToUsuario_ClienteDniToVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClienteNombre",
                table: "Ventas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Dni",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClienteNombre",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "Dni",
                table: "Usuarios");
        }
    }
}
