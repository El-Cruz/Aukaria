using Aukaria.Application.Interfaces;
using Aukaria.Infrastructure.Persistence;
using Aukaria.Infrastructure.Repositories;
using Aukaria.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aukaria.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = ObtenerCadenaPostgresValida(configuration);

        services.AddDbContext<AukariaDbContext>(options =>
            options.UseNpgsql(string.IsNullOrWhiteSpace(connectionString)
                ? "Host=localhost;Port=5432;Database=aukaria_dev;Username=postgres;Password=postgres"
                : connectionString));

        services.AddScoped<IPdfExtractorService, PdfExtractorService>();
        services.AddScoped<IDocumentClassifierService, DocumentClassifierService>();
        services.AddHttpClient<IClaudeApiService, ClaudeApiService>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(3);
        });
        services.AddScoped<IReportGeneratorService, ReportGeneratorService>();
        services.AddScoped<IAnalisisPredialRepository, AnalisisPredialRepository>();

        return services;
    }

    public static string ObtenerCadenaPostgresValida(IConfiguration configuration)
    {
        var rawUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
                     ?? Environment.GetEnvironmentVariable("DATABASE_PRIVATE_URL")
                     ?? Environment.GetEnvironmentVariable("DATABASE_PUBLIC_URL")
                     ?? Environment.GetEnvironmentVariable("POSTGRES_URL")
                     ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return string.Empty;
        }

        rawUrl = rawUrl.Trim();

        if (rawUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            rawUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(rawUrl);
                var userInfo = uri.UserInfo.Split(':');
                var user = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
                var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 5432;
                var db = uri.AbsolutePath.TrimStart('/');

                return $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Prefer;Trust Server Certificate=true;Include Error Detail=true;";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB ERROR] Error al parsear URI de PostgreSQL: {ex.Message}");
                return string.Empty;
            }
        }

        return rawUrl;
    }
}