using Aukaria.Application.DTOs.Requests;
using Aukaria.Application.Exceptions;
using Aukaria.Application.Interfaces;
using Aukaria.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Aukaria.Api.Controllers;

[ApiController]
[Route("api/analisis")]
public sealed class AnalisisPredialController : ControllerBase
{
    private readonly IAnalisisPredialService _analisisService;

    public AnalisisPredialController(IAnalisisPredialService analisisService)
    {
        _analisisService = analisisService;
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
            var resultado = await _analisisService.PreAnalizarFmiAsync(stream, empresaId, cancellationToken);
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

        var solicitud = new SolicitudAnalisisRequestDto
        {
            MatriculaFMI = matriculaFmi ?? string.Empty,
            Proposito = proposito,
            TipoDocumento = tipoDocumento,
            EmpresaId = empresaId,
            UsuarioId = usuarioId
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

    [HttpGet("descargar-word/{id:guid}")]
    public async Task<IActionResult> DescargarReporteWord(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            byte[] bytes = await _analisisService.DescargarReporteWordAsync(id, cancellationToken);
            return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"Informe_Juridico_Predial_{id}.docx");
        }
        catch (KeyNotFoundException)
        {
            return NotFound("El análisis predial especificado no fue encontrado.");
        }
    }
}