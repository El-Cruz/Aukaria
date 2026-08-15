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
        string? connectionString = configuration.GetConnectionString("DefaultConnection");

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
}