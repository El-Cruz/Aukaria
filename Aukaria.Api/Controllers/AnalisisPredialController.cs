using System.Security.Claims;
using Aukaria.Api.Auth;
using Aukaria.Application.DTOs.Requests;
using Aukaria.Application.DTOs.Responses;
using Aukaria.Application.Exceptions;
using Aukaria.Application.Interfaces;
using Aukaria.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace Aukaria.Api.Controllers;

[ApiController]
[Route("api/AnalisisPredial")]
public sealed class AnalisisPredialController : ControllerBase
{
    private static readonly JsonSerializerOptions SseJsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    private readonly IAnalisisPredialService _analisisService;
    private readonly ILogger<AnalisisPredialController> _logger;

    public AnalisisPredialController(IAnalisisPredialService analisisService, ILogger<AnalisisPredialController> logger)
    {
        _analisisService = analisisService;
        _logger = logger;
    }

    [HttpPost("pre-analisis")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> PreAnalizar(
        IFormFile archivoPdf,
        [FromForm] Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        if (archivoPdf is null || archivoPdf.Length == 0)
        {
            return BadRequest("Debe adjuntar un archivo PDF válido.");
        }

        if (!Path.GetExtension(archivoPdf.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("El archivo debe estar en formato PDF.");
        }

        using var stream = archivoPdf.OpenReadStream();
        try
        {
            var resultado = await _analisisService.PreAnalizarFmiAsync(stream, ObtenerEmpresaId(empresaId), cancellationToken);
            return Ok(resultado);
        }
        catch (PdfInvalidoException)
        {
            return BadRequest("El archivo adjuntado no es un PDF válido o está dañado.");
        }
    }

    [HttpPost("procesar")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ProcesarAnalisis(
        IFormFile archivoPdf,
        [FromForm] string? matriculaFmi,
        [FromForm] PropositoAnalisis proposito,
        [FromForm] TipoDocumentoJuridico tipoDocumento,
        [FromForm] Guid empresaId,
        [FromForm] Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (archivoPdf is null || archivoPdf.Length == 0)
        {
            return BadRequest("Debe adjuntar un archivo PDF válido.");
        }

        if (!Path.GetExtension(archivoPdf.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("El archivo debe estar en formato PDF.");
        }

        var (empresaReal, usuarioReal) = ObtenerContexto(empresaId, usuarioId);

        var solicitud = new SolicitudAnalisisRequestDto
        {
            MatriculaFMI = matriculaFmi ?? string.Empty,
            Proposito = proposito,
            TipoDocumento = tipoDocumento,
            EmpresaId = empresaReal,
            UsuarioId = usuarioReal
        };

        using var stream = archivoPdf.OpenReadStream();
        try
        {
            var resultado = await _analisisService.ProcesarAnalisisCtlAsync(solicitud, stream, cancellationToken);
            return Ok(resultado);
        }
        catch (PdfInvalidoException)
        {
            return BadRequest("El archivo adjuntado no es un PDF válido o está dañado.");
        }
    }

    [HttpPost("procesar-sse")]
    [Consumes("multipart/form-data")]
    public async Task ProcesarAnalisisStreaming(
        IFormFile archivoPdf,
        [FromForm] string? matriculaFmi,
        [FromForm] PropositoAnalisis proposito,
        [FromForm] TipoDocumentoJuridico tipoDocumento,
        [FromForm] Guid empresaId,
        [FromForm] Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        Response.StatusCode = 200;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        if (archivoPdf is null || archivoPdf.Length == 0)
        {
            await EnviarErrorAsync("Debe adjuntar un archivo PDF válido.");
            return;
        }

        if (!Path.GetExtension(archivoPdf.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            await EnviarErrorAsync("El archivo debe estar en formato PDF.");
            return;
        }

        var (empresaReal, usuarioReal) = ObtenerContexto(empresaId, usuarioId);

        var solicitud = new SolicitudAnalisisRequestDto
        {
            MatriculaFMI = matriculaFmi ?? string.Empty,
            Proposito = proposito,
            TipoDocumento = tipoDocumento,
            EmpresaId = empresaReal,
            UsuarioId = usuarioReal
        };

        using var stream = archivoPdf.OpenReadStream();
        try
        {
            var resultado = await _analisisService.ProcesarAnalisisCtlStreamingAsync(
                solicitud,
                stream,
                progreso => EnviarEventoAsync("progreso", progreso),
                cancellationToken);

            await EnviarEventoAsync("resultado", resultado);
        }
        catch (PdfInvalidoException)
        {
            await EnviarErrorAsync("El archivo adjuntado no es un PDF válido o está dañado.");
        }
        catch (OperationCanceledException)
        {
            await EnviarErrorAsync("La solicitud fue cancelada por el cliente.");
        }
        catch (Exception ex)
        {
            await EnviarErrorAsync(ex.Message);
        }
    }

    private async Task EnviarEventoAsync(string evento, object data)
    {
        byte[] bytes = ConstruirBloqueSse(evento, data);
        await Response.Body.WriteAsync(bytes);
        await Response.Body.FlushAsync();
    }

    private static byte[] ConstruirBloqueSse(string evento, object data)
    {
        string json = JsonSerializer.Serialize(data, SseJsonOptions);
        string bloque = $"event: {evento}\ndata: {json}\n\n";
        return Encoding.UTF8.GetBytes(bloque);
    }

    private async Task EnviarErrorAsync(string detalle)
    {
        await EnviarEventoAsync("error", new { detalle });
    }

    [HttpGet("descargar-word/{id:guid}")]
    public async Task<IActionResult> DescargarReporteWord(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            ReporteWordDto reporte = await _analisisService.DescargarReporteWordAsync(id, cancellationToken);
            return File(
                reporte.Archivo,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                reporte.NombreArchivo);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("El análisis predial especificado no fue encontrado.");
        }
    }

    [Authorize]
    [HttpPost("envia-word/{id:guid}")]
    public async Task<IActionResult> EnviarReporteWord(Guid id, CancellationToken cancellationToken = default)
    {
        string? email = User.FindFirstValue(ClaimTypes.Email);
        _logger.LogInformation("Recibida solicitud de envío de reporte por correo. AnalisisId={AnalisisId}, Email={Email}", id, email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized("No hay un correo asociado a la sesión.");
        }

        try
        {
            await _analisisService.EnviarReportePorCorreoAsync(id, email, cancellationToken);
            return Ok(new { mensaje = $"Reporte enviado a {email}." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound("El análisis predial especificado no fue encontrado.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private (Guid empresaId, Guid usuarioId) ObtenerContexto(Guid empresaFallback, Guid usuarioFallback)
    {
        Guid empresaId = ObtenerEmpresaId(empresaFallback);
        Guid usuarioId = ObtenerUsuarioId(usuarioFallback);
        return (empresaId, usuarioId);
    }

    private Guid ObtenerEmpresaId(Guid fallback)
    {
        string? empresaClaim = User.FindFirstValue(AukariaAuthExtensions.ClaimEmpresaId);
        return Guid.TryParse(empresaClaim, out Guid id) ? id : fallback;
    }

    private Guid ObtenerUsuarioId(Guid fallback)
    {
        string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out Guid id) ? id : fallback;
    }
}