using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pruebaPagoMp.Migrations
{
    /// <inheritdoc />
    public partial class Add_PedidosSenia_Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClienteTelefono",
                table: "Reservas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PedidosSenia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservaId = table.Column<int>(type: "int", nullable: false),
                    ClienteDni = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    ClienteNombre = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    ClienteTelefono = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosSenia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidosSenia_Reservas_ReservaId",
                        column: x => x.ReservaId,
                        principalTable: "Reservas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PedidosSenia_ReservaId",
                table: "PedidosSenia",
                column: "ReservaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PedidosSenia");

            migrationBuilder.DropColumn(
                name: "ClienteTelefono",
                table: "Reservas");
        }
    }
}
