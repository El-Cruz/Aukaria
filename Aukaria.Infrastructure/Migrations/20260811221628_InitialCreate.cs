using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aukaria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalisisPrediales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatriculaFMI = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ORIP = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NombrePredio = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Proposito = table.Column<int>(type: "int", nullable: false),
                    Viabilidad = table.Column<int>(type: "int", nullable: false),
                    ResumenEjecutivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultadoJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaAnalisis = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsumoTokens = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalisisPrediales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BolsaCreditos = table.Column<int>(type: "int", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rol = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Empresas",
                columns: new[] { "Id", "Activo", "BolsaCreditos", "FechaRegistro", "Nit", "Nombre" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), true, 100, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "900123456-1", "Empresa Demo S.A.S." });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Activo", "Email", "EmpresaId", "Nombre", "PasswordHash", "Rol" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), true, "demo@aukaria.co", new Guid("11111111-1111-1111-1111-111111111111"), "Analista Predial Demo", "HashDemo123", 0 });

            migrationBuilder.CreateIndex(
                name: "IX_AnalisisPrediales_MatriculaFMI_EmpresaId",
                table: "AnalisisPrediales",
                columns: new[] { "MatriculaFMI", "EmpresaId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalisisPrediales");

            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
