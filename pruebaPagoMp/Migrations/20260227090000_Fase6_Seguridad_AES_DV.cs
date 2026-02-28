using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pruebaPagoMp.Migrations
{
    public partial class Fase6_Seguridad_AES_DV : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DniHash",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuitHash",
                table: "Proveedores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DigitoVerificador",
                table: "Proveedores",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DigitoVerificador",
                table: "Compras",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DigitoVerificador",
                table: "Ventas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DigitoVerificador",
                table: "MovimientosCaja",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DniHash", table: "Usuarios");
            migrationBuilder.DropColumn(name: "CuitHash", table: "Proveedores");
            migrationBuilder.DropColumn(name: "DigitoVerificador", table: "Proveedores");
            migrationBuilder.DropColumn(name: "DigitoVerificador", table: "Compras");
            migrationBuilder.DropColumn(name: "DigitoVerificador", table: "Ventas");
            migrationBuilder.DropColumn(name: "DigitoVerificador", table: "MovimientosCaja");
        }
    }
}
