using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pruebaPagoMp.Migrations
{
    /// <inheritdoc />
    public partial class AddNotasCreditoYCaja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nota",
                table: "MovimientosCaja");

            migrationBuilder.AlterColumn<int>(
                name: "MedioPago",
                table: "MovimientosCaja",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "NotasCredito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteDni = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SaldoDisponible = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasCredito", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_VentaId",
                table: "MovimientosCaja",
                column: "VentaId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosCaja_Ventas_VentaId",
                table: "MovimientosCaja",
                column: "VentaId",
                principalTable: "Ventas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosCaja_Ventas_VentaId",
                table: "MovimientosCaja");

            migrationBuilder.DropTable(
                name: "NotasCredito");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosCaja_VentaId",
                table: "MovimientosCaja");

            migrationBuilder.AlterColumn<int>(
                name: "MedioPago",
                table: "MovimientosCaja",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Nota",
                table: "MovimientosCaja",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
