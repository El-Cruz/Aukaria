using System.Text.Json;
using Aukaria.Application.DTOs;
using Aukaria.Application.DTOs.JsonSchema;
using Aukaria.Application.DTOs.Requests;
using Aukaria.Application.DTOs.Responses;
using Aukaria.Application.Interfaces;
using Aukaria.Domain.Entities;
using Aukaria.Domain.Enums;

namespace Aukaria.Application.Services;

public sealed class AnalisisPredialService : IAnalisisPredialService
{
    private readonly IAnalisisPredialRepository _repository;
    private readonly IPdfExtractorService _pdfExtractorService;
    private readonly IClaudeApiService _claudeApiService;
    private readonly IDocumentClassifierService _documentClassifierService;
    private readonly IReportGeneratorService _reportGeneratorService;

    public AnalisisPredialService(
        IAnalisisPredialRepository repository,
        IPdfExtractorService pdfExtractorService,
        IClaudeApiService claudeApiService,
        IDocumentClassifierService documentClassifierService,
        IReportGeneratorService reportGeneratorService)
    {
        _repository = repository;
        _pdfExtractorService = pdfExtractorService;
        _claudeApiService = claudeApiService;
        _documentClassifierService = documentClassifierService;
        _reportGeneratorService = reportGeneratorService;
    }

    public async Task<PreAnalisisFmiResponseDto> PreAnalizarFmiAsync(
        Stream pdfStream,
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        ExtraccionPreAnalisisDto extraccion = await _pdfExtractorService.ExtraerPreAnalisisAsync(pdfStream, cancellationToken);
        ClasificacionDocumentoDto clasificacion = _documentClassifierService.Clasificar(extraccion.TextoExtraido);

        AnalisisPredial? analisisPrevio = string.IsNullOrWhiteSpace(extraccion.MatriculaFMI)
            ? null
            : await _repository.ObtenerUltimoPorFmiAsync(extraccion.MatriculaFMI, empresaId, cancellationToken);

        return new PreAnalisisFmiResponseDto
        {
            MatriculaFMI = extraccion.MatriculaFMI,
            ExistePrevio = analisisPrevio is not null,
            FechaUltimoAnalisis = analisisPrevio?.FechaAnalisis,
            AnalisisIdPrevio = analisisPrevio?.Id,
            ClasificacionDocumento = clasificacion,
            CedulaCatastral = extraccion.CedulaCatastral,
            Nupre = extraccion.Nupre,
            Orip = extraccion.Orip,
            NombrePredio = extraccion.NombrePredio,
            Municipio = extraccion.Municipio
        };
    }

    public async Task<AnalisisPredialResponseDto> ProcesarAnalisisCtlAsync(
        SolicitudAnalisisRequestDto solicitud,
        Stream pdfStream,
        CancellationToken cancellationToken = default)
    {
        string textoCtl = await _pdfExtractorService.ExtraerTextoCompletoAsync(pdfStream, cancellationToken);
        ClaudeAnalisisResultadoDto resultadoClaude = await _claudeApiService.AnalizarDocumentoAsync(
            textoCtl,
            solicitud.Proposito,
            solicitud.TipoDocumento,
            cancellationToken);

        return await PersistirYConstruirRespuestaAsync(solicitud, resultadoClaude.Resultado, resultadoClaude.TotalTokens, cancellationToken);
    }

    public async Task<AnalisisPredialResponseDto> ProcesarAnalisisCtlStreamingAsync(
        SolicitudAnalisisRequestDto solicitud,
        Stream pdfStream,
        Action<ProgresoAnalisisDto> onProgreso,
        CancellationToken cancellationToken = default)
    {
        onProgreso(new ProgresoAnalisisDto { Etapa = "extracting", Porcentaje = 1, Mensaje = "Extrayendo texto del documento..." });

        string textoCtl = await _pdfExtractorService.ExtraerTextoCompletoAsync(pdfStream, cancellationToken);
        int ultimoProgreso = -1;

        void ReportarProgresoClaude(ProgresoAnalisisDto progreso)
        {
            progreso.Porcentaje = Math.Clamp(progreso.Porcentaje, 25, 90);
            if (progreso.Porcentaje > ultimoProgreso)
            {
                ultimoProgreso = progreso.Porcentaje;
                onProgreso(progreso);
            }
        }

        ClaudeAnalisisResultadoDto resultadoClaude = await _claudeApiService.AnalizarDocumentoStreamingAsync(
            textoCtl,
            solicitud.Proposito,
            solicitud.TipoDocumento,
            ReportarProgresoClaude,
            cancellationToken);

        onProgreso(new ProgresoAnalisisDto { Etapa = "saving", Porcentaje = 95, Mensaje = "Guardando el análisis..." });

        AnalisisPredialResponseDto respuesta = await PersistirYConstruirRespuestaAsync(
            solicitud,
            resultadoClaude.Resultado,
            resultadoClaude.TotalTokens,
            cancellationToken);

        onProgreso(new ProgresoAnalisisDto { Etapa = "done", Porcentaje = 100, Mensaje = "Análisis completado." });

        return respuesta;
    }

    private async Task<AnalisisPredialResponseDto> PersistirYConstruirRespuestaAsync(
        SolicitudAnalisisRequestDto solicitud,
        AnalisisResultadoJsonDto resultado,
        int consumoTokens,
        CancellationToken cancellationToken)
    {
        var analisis = new AnalisisPredial
        {
            Id = Guid.NewGuid(),
            EmpresaId = solicitud.EmpresaId,
            UsuarioId = solicitud.UsuarioId,
            MatriculaFMI = resultado.MatriculaFMI,
            ORIP = resultado.ORIP,
            NombrePredio = resultado.NombrePredio,
            Proposito = solicitud.Proposito,
            TipoDocumento = solicitud.TipoDocumento,
            Viabilidad = MapearViabilidad(resultado.Viabilidad),
            ResumenEjecutivo = resultado.ResumenEjecutivo,
            ResultadoJson = JsonSerializer.Serialize(resultado),
            FechaAnalisis = DateTime.UtcNow,
            ConsumoTokens = consumoTokens
        };

        await _repository.AgregarAsync(analisis, cancellationToken);

        return new AnalisisPredialResponseDto
        {
            Id = analisis.Id,
            MatriculaFMI = analisis.MatriculaFMI,
            NombrePredio = analisis.NombrePredio,
            ORIP = resultado.ORIP,
            Departamento = resultado.Departamento,
            Municipio = resultado.Municipio,
            CedulaCatastral = resultado.CedulaCatastral,
            Proposito = analisis.Proposito,
            TipoDocumento = analisis.TipoDocumento,
            NombreTipoDocumento = analisis.TipoDocumento.ObtenerNombre(),
            Viabilidad = analisis.Viabilidad,
            ResumenEjecutivo = analisis.ResumenEjecutivo,
            Resultado = resultado,
            FechaAnalisis = analisis.FechaAnalisis
        };
    }

    public async Task<ReporteWordDto> DescargarReporteWordAsync(
        Guid analisisId,
        CancellationToken cancellationToken = default)
    {
        AnalisisPredial? analisis = await _repository.ObtenerPorIdAsync(analisisId, cancellationToken);

        if (analisis is null)
        {
            throw new KeyNotFoundException("El análisis predial solicitado no existe.");
        }

        AnalisisResultadoJsonDto resultadoDto = JsonSerializer.Deserialize<AnalisisResultadoJsonDto>(analisis.ResultadoJson)
            ?? new AnalisisResultadoJsonDto();

        byte[] bytes = await _reportGeneratorService.GenerarReporteWordAsync(resultadoDto, cancellationToken);

        return new ReporteWordDto
        {
            Archivo = bytes,
            NombreArchivo = ConstruirNombreArchivo(resultadoDto.MatriculaFMI, analisisId)
        };
    }

    private static string ConstruirNombreArchivo(string matriculaFmi, Guid analisisId)
    {
        string fmi = new string((matriculaFmi ?? string.Empty).Where(c => !System.IO.Path.GetInvalidFileNameChars().Contains(c)).ToArray()).Trim();

        return string.IsNullOrWhiteSpace(fmi)
            ? $"Informe_Juridico_Predial_{analisisId}.docx"
            : $"Informe_Juridico_Predial_FMI_{fmi}.docx";
    }

    private static EstadoViabilidad MapearViabilidad(string viabilidad)
    {
        return Enum.TryParse<EstadoViabilidad>(viabilidad, ignoreCase: true, out EstadoViabilidad estado)
            ? estado
            : EstadoViabilidad.RequiereRevision;
    }
}