using System.Security.Claims;
using System.Text.Json.Serialization;
using Aukaria.Api.Auth;
using Aukaria.Application.Interfaces;
using Aukaria.Application.Services;
using Aukaria.Domain.Entities;
using Aukaria.Infrastructure;
using Aukaria.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AukariaProductionCors", policy =>
    {
        var configOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var explicitOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "https://aukaria.com",
            "https://www.aukaria.com",
            "http://localhost:5173",
            "http://localhost:3000"
        };

        foreach (var origin in configOrigins)
        {
            explicitOrigins.Add(origin);
        }

        policy.WithOrigins([.. explicitOrigins, "https://*.vercel.app"])
              .SetIsOriginAllowedToAllowWildcardSubdomains()
              .SetIsOriginAllowed(origin =>
              {
                  if (string.IsNullOrWhiteSpace(origin)) return false;

                  if (explicitOrigins.Contains(origin)) return true;

                  if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                  {
                      // Permite dominios aukaria.com y cualquier subdominio
                      if (uri.Host.Equals("aukaria.com", StringComparison.OrdinalIgnoreCase) ||
                          uri.Host.EndsWith(".aukaria.com", StringComparison.OrdinalIgnoreCase))
                      {
                          return true;
                      }

                      // Permite cualquier despliegue o preview en vercel.app con HTTPS
                      if (uri.Host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase) &&
                          uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                      {
                          return true;
                      }

                      // Permite localhost en desarrollo
                      if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                          uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
                      {
                          return true;
                      }
                  }

                  return false;
              })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Configuration.AddEnvironmentVariables();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errores = context.ModelState
                .Where(kv => kv.Value?.Errors.Count > 0)
                .Select(kv => $"{kv.Key}: {kv.Value!.Errors[0].ErrorMessage}");
            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
                "Solicitud inválida. " + string.Join(" | ", errores));
        };
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPath
        | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestQuery
        | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponseStatusCode;
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IAnalisisPredialService, AnalisisPredialService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddAukariaAuth(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();

bool hayConexionBd = !string.IsNullOrWhiteSpace(
    Aukaria.Infrastructure.DependencyInjection.ObtenerCadenaPostgresValida(app.Configuration));
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AukariaDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
    if (!hayConexionBd)
    {
        logger.LogError("No se encontró conexión a la base de datos (ni DATABASE_URL ni ConnectionStrings:DefaultConnection). La base de datos no se inicializó.");
    }
    else
    {
        try
        {
            if (dbContext.Database.CanConnect())
            {
                dbContext.Database.Migrate();
                logger.LogInformation("Base de datos conectada y migraciones aplicadas correctamente.");
            }
            else
            {
                logger.LogError("La base de datos está configurada pero no se pudo conectar. Las migraciones no se aplicaron; la API continuará iniciándose.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al conectar o migrar la base de datos en el arranque. La API continuará iniciándose.");
        }
    }
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        context.Response.Headers.Append("Access-Control-Allow-Origin", "*");

        var exceptionHandlerPathFeature =
            context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var ex = exceptionHandlerPathFeature?.Error;

        string detalle = ex?.Message ?? string.Empty;
        var inner = ex?.InnerException;
        while (inner is not null)
        {
            detalle += " | " + inner.Message;
            inner = inner.InnerException;
        }

        var errorResponse = new
        {
            error = "Error interno al procesar el análisis predial.",
            detalle,
            tipo = ex?.GetType().Name
        };

        await context.Response.WriteAsJsonAsync(errorResponse);
    });
});

app.UseCors("AukariaProductionCors");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseHttpLogging();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Ok(new { status = "Healthy", service = "Aukaria API .NET 9", timestamp = DateTime.UtcNow }));

app.Run();