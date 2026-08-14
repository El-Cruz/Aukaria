using System.Text.Json.Serialization;
using Aukaria.Application.Interfaces;
using Aukaria.Application.Services;
using Aukaria.Infrastructure;

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

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPath
        | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestQuery
        | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponseStatusCode;
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IAnalisisPredialService, AnalisisPredialService>();

const string AukariaCorsPolicy = "AukariaCorsPolicy";

string[] origenesLocalesDesarrollo = ["http://localhost:5173", "http://localhost:3000"];

bool esModoDemoOStaging =
    builder.Environment.IsEnvironment("Demo") ||
    builder.Environment.IsEnvironment("Staging");

string? origenesProduccion = builder.Configuration["AllowedOrigins"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(AukariaCorsPolicy, policy =>
    {
        if (esModoDemoOStaging || string.IsNullOrWhiteSpace(origenesProduccion))
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            string[] origenesPermitidos = origenesProduccion
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Concat(origenesLocalesDesarrollo)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            policy.WithOrigins(origenesPermitidos)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

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

app.UseCors(AukariaCorsPolicy);

app.UseCors("AukariaProductionCors");

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();