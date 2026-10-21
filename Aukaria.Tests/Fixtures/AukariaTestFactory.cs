using Aukaria.Application.Interfaces;
using Aukaria.Domain.Entities;
using Aukaria.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aukaria.Tests.Fixtures;

public class AukariaTestFactory : WebApplicationFactory<Program>
{
    public bool NeonConectado { get; private set; }
    public string MensajeDiagnosticoNeon { get; private set; } = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Jwt:Secret"] = "DevTestSecret_AlMenos256BitsParaTesting_1234567890",
                ["Claude:ApiKey"] = "sk-ant-api03-test-mock-key-for-smoke-test-only",
                ["Cors:AllowedOrigins"] = "http://localhost:5173"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Verificamos si la base de datos real puede conectarse
            using var tempProvider = services.BuildServiceProvider();
            var config = tempProvider.GetRequiredService<IConfiguration>();
            var dbCadena = Aukaria.Infrastructure.DependencyInjection.ObtenerCadenaPostgresValida(config);

            bool conexionExitosa = false;
            if (!string.IsNullOrWhiteSpace(dbCadena))
            {
                try
                {
                    using var testDbContext = new AukariaDbContext(
                        new DbContextOptionsBuilder<AukariaDbContext>()
                            .UseNpgsql(dbCadena)
                            .Options);

                    if (testDbContext.Database.CanConnect())
                    {
                        conexionExitosa = true;
                        NeonConectado = true;
                        MensajeDiagnosticoNeon = "Conexión a Neon PostgreSQL verificada exitosamente.";
                    }
                }
                catch (Exception ex)
                {
                    MensajeDiagnosticoNeon = $"Cadena configurada pero fallo de handshake: {ex.Message}";
                }
            }
            else
            {
                MensajeDiagnosticoNeon = "Cadena de conexión Neon no configurada en entorno local (DATABASE_URL o ConnectionStrings:DefaultConnection).";
            }

            // Si no hay base de datos activa, reemplazamos el repositorio por un mock seguro para permitir smoke testing del pipeline
            if (!conexionExitosa)
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAnalisisPredialRepository));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddScoped<IAnalisisPredialRepository, InMemoryTestAnalisisPredialRepository>();
            }
        });
    }
}

public sealed class InMemoryTestAnalisisPredialRepository : IAnalisisPredialRepository
{
    private readonly List<AnalisisPredial> _storage = new();

    public Task<AnalisisPredial?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_storage.FirstOrDefault(a => a.Id == id));
    }

    public Task<AnalisisPredial?> ObtenerUltimoPorFmiAsync(string matriculaFmi, Guid empresaId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_storage.LastOrDefault(a => a.MatriculaFMI == matriculaFmi && a.EmpresaId == empresaId));
    }

    public Task<List<AnalisisPredial>> ObtenerHistorialPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_storage.Where(a => a.UsuarioId == usuarioId).ToList());
    }

    public Task AgregarAsync(AnalisisPredial analisis, CancellationToken cancellationToken = default)
    {
        _storage.Add(analisis);
        return Task.CompletedTask;
    }
}
