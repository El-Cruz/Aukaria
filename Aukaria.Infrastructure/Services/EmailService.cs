using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Aukaria.Application.Interfaces;
using Aukaria.Application.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aukaria.Infrastructure.Services;

public sealed class EmailService : IEmailService
{
    private const string ResendEndpoint = "https://api.resend.com/emails";
    private const string DefaultFrom = "Aukaria Notificaciones <notificaciones@mail.aukaria.com>";

    private static readonly HttpClient _defaultHttpClient = new();
    private readonly HttpClient _httpClient;
    private readonly IConfiguration? _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        ILogger<EmailService> logger,
        IConfiguration? configuration = null,
        IOptions<EmailOptions>? options = null,
        HttpClient? httpClient = null)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient ?? _defaultHttpClient;
    }

    public async Task<bool> EnviarOtpAsync(string destinatario, string codigoOtp, CancellationToken cancellationToken = default)
    {
        string subject = "Aukaria — Código de verificación";
        string html = $"<p>Tu código de verificación de Aukaria es: <strong>{codigoOtp}</strong></p><p>Este código expira en 5 minutos.</p>";

        return await EnviarViaResendAsync(destinatario, subject, html, null, null, cancellationToken);
    }

    public async Task<bool> EnviarReporteAsync(
        string destinatario,
        string nombreArchivo,
        byte[] adjunto,
        string fmi,
        CancellationToken cancellationToken = default)
    {
        string subject = $"Informe Jurídico Predial — FMI {fmi}";
        string html = $"<p>Adjunto encontrarás el informe jurídico predial correspondiente al FMI <strong>{fmi}</strong>.</p>";

        return await EnviarViaResendAsync(destinatario, subject, html, nombreArchivo, adjunto, cancellationToken);
    }

    private async Task<bool> EnviarViaResendAsync(
        string destinatario,
        string subject,
        string html,
        string? nombreArchivo,
        byte[]? adjunto,
        CancellationToken cancellationToken)
    {
        string apiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY")
            ?? _configuration?["RESEND_API_KEY"]
            ?? _configuration?["Resend:ApiKey"]
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("[RESEND ERROR] La variable de entorno RESEND_API_KEY no está configurada para el envío a {Destinatario}.", destinatario);
            throw new InvalidOperationException("La clave de API de Resend (RESEND_API_KEY) no está configurada en las variables de entorno.");
        }

        var payload = new Dictionary<string, object?>
        {
            ["from"] = DefaultFrom,
            ["to"] = new[] { destinatario },
            ["subject"] = subject,
            ["html"] = html
        };

        if (!string.IsNullOrWhiteSpace(nombreArchivo) && adjunto is { Length: > 0 })
        {
            payload["attachments"] = new[]
            {
                new
                {
                    filename = nombreArchivo,
                    content = Convert.ToBase64String(adjunto)
                }
            };
        }

        string json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, ResendEndpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

        _logger.LogInformation("[RESEND] Enviando correo a {Destinatario} con asunto '{Subject}'", destinatario, subject);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("[RESEND] El envío de correo a {Destinatario} fue cancelado por el cliente.", destinatario);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RESEND ERROR] Excepción al enviar correo a {Destinatario}: {Mensaje}", destinatario, ex.Message);
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            string errorDetalle = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("[RESEND ERROR] Falló el envío de correo a {Destinatario}. Status: {StatusCode}, Detalle: {Detalle}",
                destinatario, response.StatusCode, errorDetalle);
            throw new HttpRequestException($"Error de Resend API al enviar a {destinatario} [{response.StatusCode}]: {errorDetalle}");
        }

        _logger.LogInformation("[RESEND SUCCESS] Correo enviado exitosamente a {Destinatario}.", destinatario);
        return true;
    }
}
