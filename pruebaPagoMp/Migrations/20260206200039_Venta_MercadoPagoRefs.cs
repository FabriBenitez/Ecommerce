using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pruebaPagoMp.Migrations
{
    /// <inheritdoc />
    public partial class Venta_MercadoPagoRefs : Migration
    {
        /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "MercadoPagoPreferenceId",
            table: "Ventas",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MercadoPagoUrlPago",
            table: "Ventas",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MercadoPagoPaymentId",
            table: "Ventas",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MercadoPagoEstado",
            table: "Ventas",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: true);

        // Opcional: índice para búsquedas rápidas por preference/payment
        migrationBuilder.CreateIndex(
            name: "IX_Ventas_MercadoPagoPreferenceId",
            table: "Ventas",
            column: "MercadoPagoPreferenceId");

        migrationBuilder.CreateIndex(
            name: "IX_Ventas_MercadoPagoPaymentId",
            table: "Ventas",
            column: "MercadoPagoPaymentId");
    }

        /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Ventas_MercadoPagoPreferenceId",
            table: "Ventas");

        migrationBuilder.DropIndex(
            name: "IX_Ventas_MercadoPagoPaymentId",
            table: "Ventas");

        migrationBuilder.DropColumn(
            name: "MercadoPagoPreferenceId",
            table: "Ventas");

        migrationBuilder.DropColumn(
            name: "MercadoPagoUrlPago",
            table: "Ventas");

        migrationBuilder.DropColumn(
            name: "MercadoPagoPaymentId",
            table: "Ventas");

        migrationBuilder.DropColumn(
            name: "MercadoPagoEstado",
            table: "Ventas");
    }
    }
}
