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
            System = ConstruirSystemPrompt(proposito, tipoDocumento),
            Messages =
            [
                new AnthropicMessage { Role = "user", Content = textoDocumento }
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

    private static string ConstruirSystemPrompt(PropositoAnalisis proposito, TipoDocumentoJuridico tipoDoc)
    {
        string enfoque = proposito switch
        {
            PropositoAnalisis.CompraVenta =>
                "Detecta embargos, falsas tradiciones y afectaciones a vivienda familiar, y evalúa la seguridad del título para la compraventa.",
            PropositoAnalisis.ServidumbreLey1274 =>
                "Verifica el cumplimiento de la Ley 1274 de 2009, valida estrictamente los propietarios actuales, los linderos y la viabilidad de ocupación para proyectos de infraestructura o energía.",
            PropositoAnalisis.EstudioTitulosBancario =>
                "Evalúa el tracto sucesivo (cadena traditiva de al menos 20 años), aplica el rigor normativo exigido por las entidades financieras y verifica las garantías hipotecarias.",
            _ => "Realiza un análisis integral de la tradición y del estado del folio de matrícula inmobiliaria."
        };

        string instruccionesTipo = tipoDoc switch
        {
            TipoDocumentoJuridico.VUR =>
                "TIPO DE DOCUMENTO: INFORME DE LA VENTANILLA ÚNICA REGISTRAL (VUR).\n" +
                "- Verifica el estado registral reportado (vigencia del certificado, radicados y trámites asociados).\n" +
                "- Confirma los propietarios y titulares de dominio reportados en el VUR, contrastándolos con las anotaciones del folio cuando aplique.\n" +
                "- Señala alertas inmediatas: pendientes de registro, correcciones solicitadas por la ORIP, errores de digitación o inconsistencias entre reporte y folio.\n" +
                "- Pobla MatriculaFMI, ORIP, NombrePredio y PropietarioActual con los datos del reporte VUR; deja vacías las secciones que no apliquen.",
            TipoDocumentoJuridico.EscrituraPublica =>
                "TIPO DE DOCUMENTO: ESCRITURA PÚBLICA NOTARIAL.\n" +
                "- Extrae Notaría, Ciudad, Fecha de otorgamiento, Otorgantes (distingue Vendedores/Compradores u otorgantes según su rol), Cuantía, FMI citado y Linderos.\n" +
                "- Identifica la matrícula inmobiliaria citada en la escritura y evalúa si grava o libera el predio (compraventa, hipoteca, constitución de servidumbre).\n" +
                "- Señala riesgos: faltante de certificado de tradición anexo, diferencias de cabida, cláusulas ambiguas o gravámenes pendientes de cancelación.\n" +
                "- Pobla Otorgantes, Notaria, CiudadNotaria, FechaEscritura, Cuantia y Linderos con los valores extraídos.",
            TipoDocumentoJuridico.DocumentoInmobiliarioGeneral =>
                "TIPO DE DOCUMENTO: DOCUMENTO INMOBILIARIO GENERAL.\n" +
                "- Realiza un diagnóstico jurídico integral de la naturaleza y finalidad del documento (título, contrato, poder, cesión, paz y salvo, petición, etc.).\n" +
                "- Identifica la matrícula inmobiliaria o el predio involucrado si aparece en el texto y evalúa el impacto registral del documento.\n" +
                "- Lista alertas y recomendaciones de gestión según el contenido detectado.",
            _ =>
                "TIPO DE DOCUMENTO: CERTIFICADO DE TRADICIÓN Y LIBERTAD (CTL).\n" +
                "- Aplica el análisis clásico de tradición: cadena de anotaciones, gravámenes, embargos, medidas cautelares, hipotecas, servidumbres y tracto sucesivo.\n" +
                "- Verifica falsas tradiciones, afectaciones a vivienda familiar y la vigencia del folio, conforme a la Ley 1579 de 2012.\n" +
                "- Pobla MatriculaFMI, ORIP, departamento/municipio, área, propietario actual y estado del folio desde el certificado."
        };

        return """
               Actúa como un abogado colombiano especialista en Derecho Inmobiliario, Registral y Gestión Predial.
               {ENFOQUE}

               {TIPO_DOCUMENTO}

               INSTRUCCIONES DE SALIDA ESTRICTA:
               - Tu respuesta debe ser ÚNICAMENTE un objeto JSON válido acorde al esquema descrito abajo.
               - No incluyas texto introductorio, explicaciones ni comentarios.
               - No uses bloques de código Markdown (```json ... ```). Devuelve solo el JSON puro.
               - Usa cadena vacía ("") o arreglo vacío ([]) para los campos que no apliquen al tipo de documento.

               ESQUEMA JSON REQUERIDO:
               {
                 "MatriculaFMI": "string",
                 "ORIP": "string",
                 "Departamento": "string",
                 "Municipio": "string",
                 "Vereda": "string",
                 "NombrePredio": "string",
                 "AreaRegistrada": "string",
                 "PropietarioActual": "string",
                 "EstadoFolio": "string",
                 "Viabilidad": "Viable" | "RequiereRevision" | "AlertaCritica",
                 "ResumenEjecutivo": "Diagnóstico técnico en 2 párrafos",
                 "Anotaciones": [
                   {
                     "NumeroAnotacion": "string",
                     "Fecha": "string",
                     "Especificacion": "string",
                     "NaturalezaJuridica": "string",
                     "EsGravamen": true,
                     "EsMedidaCautelar": false
                   }
                 ],
                 "AlertasJuridicas": [
                   {
                     "Titulo": "string",
                     "Descripcion": "string",
                     "NivelRiesgo": "Bajo" | "Medio" | "Alto",
                     "Recomendacion": "string"
                   }
                 ],
                 "Observaciones": ["recomendaciones de gestión predial"],
                 "NumeroEscritura": "string (solo Escritura Pública)",
                 "Notaria": "string (solo Escritura Pública)",
                 "CiudadNotaria": "string (solo Escritura Pública)",
                 "FechaEscritura": "string (solo Escritura Pública)",
                 "Otorgantes": ["string de roles, p. ej. 'Comprador: ...' (solo Escritura Pública)"],
                 "Cuantia": "string (solo Escritura Pública)",
                 "Linderos": "string (solo Escritura Pública)"
               }
               """.Replace("{ENFOQUE}", enfoque, StringComparison.Ordinal)
               .Replace("{TIPO_DOCUMENTO}", instruccionesTipo, StringComparison.Ordinal);
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