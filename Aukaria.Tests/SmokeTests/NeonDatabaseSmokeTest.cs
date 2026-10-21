using Aukaria.Infrastructure.Persistence;
using Aukaria.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Aukaria.Tests.SmokeTests;

public class NeonDatabaseSmokeTest : IClassFixture<AukariaTestFactory>
{
    private readonly AukariaTestFactory _factory;
    private readonly ITestOutputHelper _output;

    public NeonDatabaseSmokeTest(AukariaTestFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task VerificarConectividadNeonPostgreSQL_HandshakeSelect1()
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var dbCadena = Aukaria.Infrastructure.DependencyInjection.ObtenerCadenaPostgresValida(config);

        _output.WriteLine($"[AUDITORÍA DB] Cadena detectada: {(string.IsNullOrEmpty(dbCadena) ? "(No configurada / localdb descartada)" : "Configurada")}");
        _output.WriteLine($"[ESTADO NEON]: {_factory.MensajeDiagnosticoNeon}");

        if (string.IsNullOrWhiteSpace(dbCadena))
        {
            _output.WriteLine(">>> [ADVERTENCIA QA] No hay conexión activa a Neon en este entorno local.");
            _output.WriteLine(">>> Configure 'DATABASE_URL' o 'ConnectionStrings:DefaultConnection' con 'SSL Mode=Require'.");
            // No fallamos el pipeline completo si no hay credenciales locales, pero registramos la auditoría de estado
            Assert.True(true);
            return;
        }

        // Si la cadena está presente, ejecutar handshake estricto SELECT 1;
        var options = new DbContextOptionsBuilder<AukariaDbContext>()
            .UseNpgsql(dbCadena)
            .Options;

        using var dbContext = new AukariaDbContext(options);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        bool canConnect = await dbContext.Database.CanConnectAsync();
        stopwatch.Stop();

        _output.WriteLine($"Handshake CanConnectAsync: {canConnect} ({stopwatch.ElapsedMilliseconds} ms)");
        Assert.True(canConnect, "Error de handshake contra Neon PostgreSQL.");

        var resultado = await dbContext.Database.SqlQueryRaw<int>("SELECT 1;").ToListAsync();
        Assert.Single(resultado);
        Assert.Equal(1, resultado[0]);

        _output.WriteLine("Handshake 'SELECT 1;' ejecutado exitosamente contra PostgreSQL Neon.");
    }
}
