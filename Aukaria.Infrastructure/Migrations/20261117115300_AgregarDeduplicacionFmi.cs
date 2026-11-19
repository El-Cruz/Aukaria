using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aukaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDeduplicacionFmi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaExpedicionCertificado",
                table: "AnalisisPrediales",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Idioma",
                table: "AnalisisPrediales",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "es");

            migrationBuilder.AddColumn<bool>(
                name: "Reemplazado",
                table: "AnalisisPrediales",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ReemplazadoPorId",
                table: "AnalisisPrediales",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalisisPrediales_EmpresaId_MatriculaFMI_FechaExpedicionCer~",
                table: "AnalisisPrediales",
                columns: new[] { "EmpresaId", "MatriculaFMI", "FechaExpedicionCertificado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AnalisisPrediales_EmpresaId_MatriculaFMI_FechaExpedicionCer~",
                table: "AnalisisPrediales");

            migrationBuilder.DropColumn(
                name: "FechaExpedicionCertificado",
                table: "AnalisisPrediales");

            migrationBuilder.DropColumn(
                name: "Idioma",
                table: "AnalisisPrediales");

            migrationBuilder.DropColumn(
                name: "Reemplazado",
                table: "AnalisisPrediales");

            migrationBuilder.DropColumn(
                name: "ReemplazadoPorId",
                table: "AnalisisPrediales");
        }
    }
}
