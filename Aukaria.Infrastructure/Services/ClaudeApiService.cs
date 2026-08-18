using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aukaria.Application.DTOs.JsonSchema;
using Aukaria.Application.Interfaces;
using Aukaria.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aukaria.Infrastructure.Services;

public sealed class ClaudeApiService : IClaudeApiService
{
    private const string Endpoint = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";
    private const int MaxTokens = 64000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClaudeApiService> _logger;

    public ClaudeApiService(HttpClient httpClient, IConfiguration configuration, ILogger<ClaudeApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AnalisisResultadoJsonDto> AnalizarDocumentoAsync(
        string textoDocumento,
        PropositoAnalisis proposito,
        TipoDocumentoJuridico tipoDocumento,
        CancellationToken cancellationToken = default)
    {
        string apiKey = _configuration["Anthropic:ApiKey"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("Clave de API de Anthropic ausente en la configuración ('Anthropic:ApiKey').");
            throw new InvalidOperationException("La clave de API de Anthropic no está configurada. Verifica la variable Anthropic__ApiKey en el servidor.");
        }

        _logger.LogInformation("Clave de API de Anthropic leída correctamente.");

        string modelo = _configuration["Anthropic:Model"] ?? "claude-sonnet-5";

        var request = new AnthropicRequest
        {
            Model = modelo,
            MaxTokens = MaxTokens,
            System = LegalPromptBuilder.ConstruirSystemPrompt(proposito, tipoDocumento),
            Messages =
            [
                new AnthropicMessage { Role = "user", Content = LegalPromptBuilder.ConstruirUserPrompt(textoDocumento) }
            ]
        };

        string requestJson = JsonSerializer.Serialize(request, JsonOptions);
        using var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = requestContent
        };
        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Headers.Add("anthropic-version", AnthropicVersion);
        httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        _logger.LogInformation("Iniciando llamada a la API de Anthropic (modelo: {Modelo}).", modelo);

        using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogInformation("Llamada a la API de Anthropic finalizada. Estado HTTP: {Estado}.", (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("La API de Anthropic devolvió el estado {Estado}. Detalle: {Detalle}.", (int)response.StatusCode, responseBody);
            throw new HttpRequestException(
                $"La API de Anthropic devolvió el estado {(int)response.StatusCode} ({response.ReasonPhrase}). Detalle: {responseBody}");
        }

        AnthropicResponse? parsed = JsonSerializer.Deserialize<AnthropicResponse>(responseBody, JsonOptions);
        string contenido = parsed?.Content.FirstOrDefault(bloque =>
                string.Equals(bloque.Type, "text", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(bloque.Text))
            ?.Text
            ?? throw new JsonException("La respuesta de la API de Anthropic no contenía un bloque de texto válido (content[].text).");

        if (parsed.StopReason == "max_tokens")
        {
            throw new JsonException(
                "La respuesta de la API de Anthropic fue truncada porque se alcanzó el límite de max_tokens. Aumenta el presupuesto de tokens o solicita una salida más concisa.");
        }

        string jsonLimpio = LimpiarFencesMarkdown(contenido);

        try
        {
            return JsonSerializer.Deserialize<AnalisisResultadoJsonDto>(jsonLimpio, JsonOptions)
                ?? throw new JsonException("La respuesta de la API de Anthropic no pudo deserializarse en el esquema esperado.");
        }
        catch (JsonException ex)
        {
            throw new JsonException(
                $"La API de Anthropic devolvió un JSON que no cumple el esquema AnalisisResultadoJsonDto. Detalle: {ex.Message}", ex);
        }
    }

    private static string LimpiarFencesMarkdown(string contenido)
    {
        string limpio = contenido.Trim();
        if (limpio.StartsWith("```", StringComparison.Ordinal))
        {
            int inicio = limpio.IndexOf('\n');
            limpio = inicio > 0 ? limpio[(inicio + 1)..] : limpio;
        }
        if (limpio.EndsWith("```", StringComparison.Ordinal))
        {
            int fin = limpio.LastIndexOf("```", StringComparison.Ordinal);
            limpio = limpio[..fin];
        }
        return limpio.Trim();
    }

    private sealed class AnthropicRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("system")]
        public string System { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<AnthropicMessage> Messages { get; set; } = [];
    }

    private sealed class AnthropicMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class AnthropicResponse
    {
        [JsonPropertyName("stop_reason")]
        public string StopReason { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public List<AnthropicContentBlock> Content { get; set; } = [];
    }

    private sealed class AnthropicContentBlock
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}