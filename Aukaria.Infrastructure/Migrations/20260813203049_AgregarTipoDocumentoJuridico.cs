using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aukaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTipoDocumentoJuridico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TipoDocumento",
                table: "AnalisisPrediales",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "CTL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoDocumento",
                table: "AnalisisPrediales");
        }
    }
}
