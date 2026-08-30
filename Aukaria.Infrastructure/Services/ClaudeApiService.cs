using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aukaria.Application.DTOs;
using Aukaria.Application.DTOs.JsonSchema;
using Aukaria.Application.DTOs.Responses;
using Aukaria.Application.Interfaces;
using Aukaria.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Aukaria.Infrastructure.Services;

public sealed class ClaudeApiService : IClaudeApiService
{
    private const string Endpoint = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";
    private const int MaxTokensDefault = 20000;
    private const int MaxProgresoEstimado = 75;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClaudeApiService> _logger;
    private readonly ResiliencePipeline<string> _resiliencePipeline;

    public ClaudeApiService(HttpClient httpClient, IConfiguration configuration, ILogger<ClaudeApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _resiliencePipeline = CrearPipelineResiliencia();
    }

    public async Task<ClaudeAnalisisResultadoDto> AnalizarDocumentoAsync(
        string textoDocumento,
        PropositoAnalisis proposito,
        TipoDocumentoJuridico tipoDocumento,
        CancellationToken cancellationToken = default)
    {
        (string apiKey, string requestJson) = ConstruirSolicitud(textoDocumento, proposito, tipoDocumento);

        var stopwatch = Stopwatch.StartNew();

        string responseBody = await _resiliencePipeline.ExecuteAsync(
            async token => await EjecutarLlamadaAsync(apiKey, requestJson, token),
            cancellationToken);

        stopwatch.Stop();

        return DeserializarRespuestaCompleta(responseBody, stopwatch.ElapsedMilliseconds);
    }

    public async Task<ClaudeAnalisisResultadoDto> AnalizarDocumentoStreamingAsync(
        string textoDocumento,
        PropositoAnalisis proposito,
        TipoDocumentoJuridico tipoDocumento,
        Action<ProgresoAnalisisDto> onProgreso,
        CancellationToken cancellationToken = default)
    {
        (string apiKey, string requestJsonBase) = ConstruirSolicitud(textoDocumento, proposito, tipoDocumento);

        string requestJson = InsertarStreamTrue(requestJsonBase);

        var stopwatch = Stopwatch.StartNew();
        var textoAcumulado = new StringBuilder();
        int inputTokens = 0;
        int outputTokens = 0;

        using var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = requestContent
        };
        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Headers.Add("anthropic-version", AnthropicVersion);
        httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        using HttpResponseMessage response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        onProgreso(new ProgresoAnalisisDto
        {
            Etapa = "analyzing",
            Porcentaje = 2,
            Mensaje = "Razonando sobre el documento..."
        });

        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("La API de Anthropic devolvió el estado {Estado}. Detalle: {Detalle}.", (int)response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"La API de Anthropic devolvió el estado {(int)response.StatusCode} ({response.ReasonPhrase}). Detalle: {errorBody}");
        }

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        string? stopReason = null;
        string? line;
        long caracteresTexto = 0;
        int progresoReportado = -1;

        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                continue;
            }

            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            string data = line["data:".Length..].Trim();
            if (data == "[DONE]")
            {
                break;
            }

            DatosEventoStreaming dataParcial = ProcesarDataEvento(data);

            if (dataParcial.Evento == "message_start" && dataParcial.Message?.Usage?.InputTokens is int it)
            {
                inputTokens = it;
            }
            else if (dataParcial.Evento == "content_block_delta")
            {
                if (dataParcial.Delta?.Tipo == "text_delta" && dataParcial.Delta.Texto is not null)
                {
                    textoAcumulado.Append(dataParcial.Delta.Texto);
                    caracteresTexto += dataParcial.Delta.Texto.Length;
                }
            }
            else if (dataParcial.Evento == "message_delta")
            {
                stopReason = dataParcial.Delta?.StopReason ?? stopReason;
                if (dataParcial.Uso?.OutputTokens is int ot)
                {
                    outputTokens = ot;
                }
            }

            int progreso = CalcularProgreso(caracteresTexto);
            if (progreso > progresoReportado)
            {
                progresoReportado = progreso;
                onProgreso(new ProgresoAnalisisDto
                {
                    Etapa = "analyzing",
                    Porcentaje = progreso,
                    Mensaje = "Generando el informe jurídico..."
                });
            }
        }

        stopwatch.Stop();

        string contenido = textoAcumulado.ToString();
        if (string.IsNullOrWhiteSpace(contenido))
        {
            throw new JsonException("La respuesta de la API de Anthropic no contenía un bloque de texto válido.");
        }

        if (stopReason == "max_tokens")
        {
            throw new JsonException(
                "La respuesta de la API de Anthropic fue truncada porque se alcanzó el límite de max_tokens. Aumenta el presupuesto de tokens o solicita una salida más concisa.");
        }

        _logger.LogInformation(
            "Análisis streaming completado en {DuracionMs} ms. Tokens: entrada {InputTokens}, salida {OutputTokens}.",
            stopwatch.ElapsedMilliseconds,
            inputTokens,
            outputTokens);

        AnalisisResultadoJsonDto resultado = DeserializarJson(contenido);

        return new ClaudeAnalisisResultadoDto
        {
            Resultado = resultado,
            InputTokens = inputTokens,
            OutputTokens = outputTokens
        };
    }

    private (string apiKey, string requestJson) ConstruirSolicitud(
        string textoDocumento,
        PropositoAnalisis proposito,
        TipoDocumentoJuridico tipoDocumento)
    {
        string apiKey = _configuration["Anthropic:ApiKey"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("Clave de API de Anthropic ausente en la configuración ('Anthropic:ApiKey').");
            throw new InvalidOperationException("La clave de API de Anthropic no está configurada. Verifica la variable Anthropic__ApiKey en el servidor.");
        }

        string modelo = _configuration["Anthropic:Model"] ?? "claude-sonnet-5";
        int maxTokens = ObtenerMaxTokens();

        var request = new AnthropicRequest
        {
            Model = modelo,
            MaxTokens = maxTokens,
            System = LegalPromptBuilder.ConstruirSystemPrompt(proposito, tipoDocumento),
            Messages =
            [
                new AnthropicMessage { Role = "user", Content = LegalPromptBuilder.ConstruirUserPrompt(textoDocumento) }
            ]
        };

        _logger.LogInformation("Iniciando llamada a la API de Anthropic (modelo: {Modelo}, max_tokens: {MaxTokens}).", modelo, maxTokens);

        return (apiKey, JsonSerializer.Serialize(request, JsonOptions));
    }

    private static string InsertarStreamTrue(string requestJson)
    {
        using var doc = JsonDocument.Parse(requestJson);
        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms))
        {
            writer.WriteStartObject();
            foreach (JsonProperty property in doc.RootElement.EnumerateObject())
            {
                property.WriteTo(writer);
            }
            writer.WriteBoolean("stream", true);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private async Task<string> EjecutarLlamadaAsync(string apiKey, string requestJson, CancellationToken cancellationToken)
    {
        using var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = requestContent
        };
        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Headers.Add("anthropic-version", AnthropicVersion);
        httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var intentoStopwatch = Stopwatch.StartNew();
        using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        intentoStopwatch.Stop();

        _logger.LogInformation(
            "Llamada a la API de Anthropic finalizada. Estado HTTP: {Estado}. Duración: {DuracionMs} ms.",
            (int)response.StatusCode,
            intentoStopwatch.ElapsedMilliseconds);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("La API de Anthropic devolvió el estado {Estado}. Detalle: {Detalle}.", (int)response.StatusCode, responseBody);
            throw new HttpRequestException(
                $"La API de Anthropic devolvió el estado {(int)response.StatusCode} ({response.ReasonPhrase}). Detalle: {responseBody}");
        }

        return responseBody;
    }

    private ClaudeAnalisisResultadoDto DeserializarRespuestaCompleta(string responseBody, long duracionMs)
    {
        AnthropicResponse parsed = JsonSerializer.Deserialize<AnthropicResponse>(responseBody, JsonOptions)
            ?? throw new JsonException("La respuesta de la API de Anthropic no pudo deserializarse.");

        AnalisisResultadoJsonDto resultado = DeserializarResultado(parsed);

        _logger.LogInformation(
            "Análisis de Anthropic completado en {DuracionMs} ms. Tokens: entrada {InputTokens}, salida {OutputTokens}.",
            duracionMs,
            parsed.Usage?.InputTokens ?? 0,
            parsed.Usage?.OutputTokens ?? 0);

        return new ClaudeAnalisisResultadoDto
        {
            Resultado = resultado,
            InputTokens = parsed.Usage?.InputTokens ?? 0,
            OutputTokens = parsed.Usage?.OutputTokens ?? 0
        };
    }

    private static AnalisisResultadoJsonDto DeserializarResultado(AnthropicResponse parsed)
    {
        string contenido = parsed.Content.FirstOrDefault(bloque =>
                string.Equals(bloque.Type, "text", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(bloque.Text))
            ?.Text
            ?? throw new JsonException("La respuesta de la API de Anthropic no contenía un bloque de texto válido (content[].text).");

        if (parsed.StopReason == "max_tokens")
        {
            throw new JsonException(
                "La respuesta de la API de Anthropic fue truncada porque se alcanzó el límite de max_tokens. Aumenta el presupuesto de tokens o solicita una salida más concisa.");
        }

        return DeserializarJson(contenido);
    }

    private static AnalisisResultadoJsonDto DeserializarJson(string contenido)
    {
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

    private static DatosEventoStreaming ProcesarDataEvento(string data)
    {
        try
        {
            return JsonSerializer.Deserialize<DatosEventoStreaming>(data, JsonOptions) ?? new DatosEventoStreaming();
        }
        catch (JsonException ex)
        {
            throw new JsonException($"No se pudo procesar un evento del streaming de Anthropic. Detalle: {ex.Message}", ex);
        }
    }

    private static int CalcularProgreso(long caracteresTexto)
    {
        int progreso = (int)(caracteresTexto / 35);
        return Math.Clamp(progreso, 0, MaxProgresoEstimado);
    }

    private int ObtenerMaxTokens()
    {
        string? valor = _configuration["Anthropic:MaxTokens"];
        if (int.TryParse(valor, out int result) && result > 0)
        {
            return result;
        }
        return MaxTokensDefault;
    }

    private ResiliencePipeline<string> CrearPipelineResiliencia()
    {
        var retryOptions = new RetryStrategyOptions<string>
        {
            ShouldHandle = new PredicateBuilder<string>()
                .Handle<HttpRequestException>()
                .Handle<TaskCanceledException>(ex => !ex.CancellationToken.IsCancellationRequested),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        };

        return new ResiliencePipelineBuilder<string>()
            .AddRetry(retryOptions)
            .Build();
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

        [JsonPropertyName("usage")]
        public AnthropicUsage? Usage { get; set; }
    }

    private sealed class AnthropicContentBlock
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private sealed class AnthropicUsage
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
    }

    private sealed class DatosEventoStreaming
    {
        [JsonPropertyName("type")]
        public string? Evento { get; set; }

        [JsonPropertyName("delta")]
        public DeltaStreaming? Delta { get; set; }

        [JsonPropertyName("message")]
        public MensajeStreaming? Message { get; set; }

        [JsonPropertyName("usage")]
        public UsoStreaming? Uso { get; set; }
    }

    private sealed class DeltaStreaming
    {
        [JsonPropertyName("type")]
        public string? Tipo { get; set; }

        [JsonPropertyName("text")]
        public string? Texto { get; set; }

        [JsonPropertyName("stop_reason")]
        public string? StopReason { get; set; }
    }

    private sealed class MensajeStreaming
    {
        [JsonPropertyName("usage")]
        public UsoStreaming? Usage { get; set; }
    }

    private sealed class UsoStreaming
    {
        [JsonPropertyName("input_tokens")]
        public int? InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int? OutputTokens { get; set; }
    }
}
