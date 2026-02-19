using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pruebaPagoMp.Migrations
{
    /// <inheritdoc />
    public partial class Add_VentaPago_MovCaja_ClienteDni : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClienteDni",
                table: "Ventas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MovimientosCaja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Concepto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VentaId = table.Column<int>(type: "int", nullable: true),
                    MedioPago = table.Column<int>(type: "int", nullable: true),
                    Nota = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosCaja", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VentaPagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    MedioPago = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VentaPagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VentaPagos_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VentaPagos_VentaId",
                table: "VentaPagos",
                column: "VentaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimientosCaja");

            migrationBuilder.DropTable(
                name: "VentaPagos");

            migrationBuilder.DropColumn(
                name: "ClienteDni",
                table: "Ventas");
        }
    }
}
