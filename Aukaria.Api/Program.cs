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

bool hayConexionBd = !string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString("DefaultConnection"));
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AukariaDbContext>();
    if (hayConexionBd)
    {
        dbContext.Database.Migrate();
    }
    else
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
        logger.LogError("ConnectionStrings:DefaultConnection no configurada. La base de datos no se inicializó.");
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

        var errorResponse = new
        {
            error = "Error interno al procesar el análisis predial.",
            detalle = ex?.Message,
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