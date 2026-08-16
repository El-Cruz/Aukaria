using System.Text.Json.Serialization;
using Aukaria.Application.Interfaces;
using Aukaria.Application.Services;
using Aukaria.Infrastructure;
using Aukaria.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AukariaProductionCors", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Configuration.AddEnvironmentVariables();

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

var app = builder.Build();

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

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();