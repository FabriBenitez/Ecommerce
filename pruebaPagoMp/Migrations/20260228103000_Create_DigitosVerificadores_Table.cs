using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pruebaPagoMp.Migrations
{
    public partial class Create_DigitosVerificadores_Table : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[DigitosVerificadores]', N'U') IS NULL
BEGIN
    CREATE TABLE [DigitosVerificadores]
    (
        [Tabla] nvarchar(450) NOT NULL,
        [Valor] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_DigitosVerificadores] PRIMARY KEY ([Tabla])
    );
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[DigitosVerificadores]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [DigitosVerificadores];
END
");
        }
    }
}
