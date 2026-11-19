using System.Text.Json;
using Aukaria.Application.DTOs;
using Aukaria.Application.DTOs.JsonSchema;
using Aukaria.Application.DTOs.Requests;
using Aukaria.Application.DTOs.Responses;
using Aukaria.Application.Interfaces;
using Aukaria.Application.Mappers;
using Aukaria.Domain.Entities;
using Aukaria.Domain.Enums;
using Aukaria.Domain.Models;

namespace Aukaria.Application.Services;

public sealed class AnalisisPredialService : IAnalisisPredialService
{
    private readonly IAnalisisPredialRepository _repository;
    private readonly IPdfExtractorService _pdfExtractorService;
    private readonly IClaudeApiService _claudeApiService;
    private readonly IDocumentClassifierService _documentClassifierService;
    private readonly IDiagnosticoWordGeneratorService _diagnosticoWordGeneratorService;
    private readonly IEmailService _emailService;

    public AnalisisPredialService(
        IAnalisisPredialRepository repository,
        IPdfExtractorService pdfExtractorService,
        IClaudeApiService claudeApiService,
        IDocumentClassifierService documentClassifierService,
        IDiagnosticoWordGeneratorService diagnosticoWordGeneratorService,
        IEmailService emailService)
    {
        _repository = repository;
        _pdfExtractorService = pdfExtractorService;
        _claudeApiService = claudeApiService;
        _documentClassifierService = documentClassifierService;
        _diagnosticoWordGeneratorService = diagnosticoWordGeneratorService;
        _emailService = emailService;
    }

    public async Task<PreAnalisisFmiResponseDto> PreAnalizarFmiAsync(
        Stream pdfStream,
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        ExtraccionPreAnalisisDto extraccion = await _pdfExtractorService.ExtraerPreAnalisisAsync(pdfStream, cancellationToken);
        ClasificacionDocumentoDto clasificacion = _documentClassifierService.Clasificar(extraccion.TextoExtraido);

        List<AnalisisPredial> vigentes = string.IsNullOrWhiteSpace(extraccion.MatriculaFMI)
            ? new List<AnalisisPredial>()
            : await _repository.ListarVigentesPorFmiAsync(extraccion.MatriculaFMI, empresaId, cancellationToken);

        AnalisisPredial? analisisPrevio = vigentes.FirstOrDefault();

        string accionSugerida = "Nuevo";
        if (vigentes.Count > 0)
        {
            if (extraccion.FechaExpedicionCertificado.HasValue && analisisPrevio?.FechaExpedicionCertificado.HasValue == true)
            {
                if (extraccion.FechaExpedicionCertificado.Value == analisisPrevio.FechaExpedicionCertificado.Value)
                    accionSugerida = "MismaFecha";
                else if (extraccion.FechaExpedicionCertificado.Value > analisisPrevio.FechaExpedicionCertificado.Value)
                    accionSugerida = "Actualizacion";
                else
                    accionSugerida = "VersionAntigua";
            }
            else
            {
                accionSugerida = "Actualizacion";
            }
        }

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
            Municipio = extraccion.Municipio,
            FechaExpedicionCertificado = extraccion.FechaExpedicionCertificado,
            FechaExpedicionPrevio = analisisPrevio?.FechaExpedicionCertificado,
            AccionSugerida = accionSugerida,
            CantidadVigentes = vigentes.Count
        };
    }

    public async Task<AnalisisPredialResponseDto> ProcesarAnalisisCtlAsync(
        SolicitudAnalisisRequestDto solicitud,
        Stream pdfStream,
        CancellationToken cancellationToken = default)
    {
        string idioma = ComparadorAnalisisService.NormalizarIdioma(solicitud.Idioma);
        string textoCtl = await _pdfExtractorService.ExtraerTextoCompletoAsync(pdfStream, cancellationToken);
        ClaudeAnalisisResultadoDto resultadoClaude = await _claudeApiService.AnalizarDocumentoAsync(
            textoCtl,
            solicitud.Proposito,
            solicitud.TipoDocumento,
            idioma,
            cancellationToken);

        return await PersistirYConstruirRespuestaAsync(solicitud, resultadoClaude.Resultado, resultadoClaude.TotalTokens, cancellationToken);
    }

    public async Task<AnalisisPredialResponseDto> ProcesarAnalisisCtlStreamingAsync(
        SolicitudAnalisisRequestDto solicitud,
        Stream pdfStream,
        Func<ProgresoAnalisisDto, Task> onProgreso,
        CancellationToken cancellationToken = default)
    {
        string idioma = ComparadorAnalisisService.NormalizarIdioma(solicitud.Idioma);
        bool en = idioma == "en";

        await onProgreso(new ProgresoAnalisisDto
        {
            Etapa = "extracting",
            Porcentaje = 1,
            Mensaje = en ? "Extracting text from document..." : "Extrayendo texto del documento..."
        });

        string textoCtl = await _pdfExtractorService.ExtraerTextoCompletoAsync(pdfStream, cancellationToken);
        int ultimoProgreso = -1;

        async Task ReportarProgresoClaude(ProgresoAnalisisDto progreso)
        {
            progreso.Porcentaje = Math.Clamp(progreso.Porcentaje, 25, 90);
            if (progreso.Porcentaje > ultimoProgreso)
            {
                ultimoProgreso = progreso.Porcentaje;
                await onProgreso(progreso);
            }
        }

        ClaudeAnalisisResultadoDto resultadoClaude = await _claudeApiService.AnalizarDocumentoStreamingAsync(
            textoCtl,
            solicitud.Proposito,
            solicitud.TipoDocumento,
            ReportarProgresoClaude,
            idioma,
            cancellationToken);

        await onProgreso(new ProgresoAnalisisDto
        {
            Etapa = "saving",
            Porcentaje = 95,
            Mensaje = en ? "Saving analysis..." : "Guardando el análisis..."
        });

        AnalisisPredialResponseDto respuesta = await PersistirYConstruirRespuestaAsync(
            solicitud,
            resultadoClaude.Resultado,
            resultadoClaude.TotalTokens,
            cancellationToken);

        await onProgreso(new ProgresoAnalisisDto
        {
            Etapa = "done",
            Porcentaje = 100,
            Mensaje = en ? "Analysis completed." : "Análisis completado."
        });

        return respuesta;
    }

    private async Task<AnalisisPredialResponseDto> PersistirYConstruirRespuestaAsync(
        SolicitudAnalisisRequestDto solicitud,
        AnalisisResultadoJsonDto resultado,
        int consumoTokens,
        CancellationToken cancellationToken)
    {
        string idioma = ComparadorAnalisisService.NormalizarIdioma(solicitud.Idioma);
        bool en = idioma == "en";

        if (string.IsNullOrWhiteSpace(resultado.MatriculaFMI) && !string.IsNullOrWhiteSpace(solicitud.MatriculaFMI))
        {
            resultado.MatriculaFMI = solicitud.MatriculaFMI.Trim();
        }

        DateOnly? fechaCertificado = ComparadorAnalisisService.ParsearFechaCertificado(resultado);
        Guid nuevoId = Guid.NewGuid();
        var idsAReemplazar = new List<Guid>();
        bool nuevoEsReemplazado = false;
        Guid? nuevoReemplazadoPorId = null;
        string? resumenDeduplicacion = null;
        bool? coherenciaVerificada = null;
        var discrepanciasCoherencia = new List<string>();

        if (!string.IsNullOrWhiteSpace(resultado.MatriculaFMI))
        {
            List<AnalisisPredial> vigentes = await _repository.ListarVigentesPorFmiAsync(
                resultado.MatriculaFMI, solicitud.EmpresaId, cancellationToken);

            if (vigentes.Count > 0)
            {
                var mismaFecha = new List<AnalisisPredial>();
                var anteriores = new List<AnalisisPredial>();
                var posteriores = new List<AnalisisPredial>();

                foreach (var v in vigentes)
                {
                    if (fechaCertificado.HasValue && v.FechaExpedicionCertificado.HasValue)
                    {
                        if (v.FechaExpedicionCertificado.Value == fechaCertificado.Value)
                            mismaFecha.Add(v);
                        else if (v.FechaExpedicionCertificado.Value < fechaCertificado.Value)
                            anteriores.Add(v);
                        else
                            posteriores.Add(v);
                    }
                    else
                    {
                        anteriores.Add(v);
                    }
                }

                if (posteriores.Count > 0)
                {
                    nuevoEsReemplazado = true;
                    nuevoReemplazadoPorId = posteriores[0].Id;
                    resumenDeduplicacion = en
                        ? $"A newer analysis for FMI {resultado.MatriculaFMI} already exists ({posteriores[0].FechaExpedicionCertificado:yyyy-MM-dd}). This older version was archived."
                        : $"Ya existe un análisis más reciente para el FMI {resultado.MatriculaFMI} ({posteriores[0].FechaExpedicionCertificado:yyyy-MM-dd}). Esta versión anterior quedó archivada.";
                }
                else
                {
                    if (mismaFecha.Count > 0)
                    {
                        var dtosMismaFecha = mismaFecha
                            .Select(m => JsonSerializer.Deserialize<AnalisisResultadoJsonDto>(m.ResultadoJson) ?? new AnalisisResultadoJsonDto())
                            .ToList();

                        ResultadoComparacionCoherencia comp = ComparadorAnalisisService.CompararCoherencia(resultado, dtosMismaFecha, idioma);
                        coherenciaVerificada = comp.EsCoherente;
                        discrepanciasCoherencia = comp.Discrepancias;

                        if (!comp.EsCoherente)
                        {
                            string fechaTxt = fechaCertificado?.ToString("yyyy-MM-dd") ?? string.Empty;
                            string resumenDisc = string.Join(" ", comp.Discrepancias);
                            resultado.Observaciones.Add(en
                                ? $"⚠ Coherence discrepancy vs analysis dated {fechaTxt}: {resumenDisc}"
                                : $"⚠ Inconsistencia de coherencia vs análisis del {fechaTxt}: {resumenDisc}");

                            resultado.AlertasJuridicas.Add(new AlertaJuridicaDto
                            {
                                NivelRiesgo = en ? "Medium" : "Medio",
                                Descripcion = en
                                    ? $"[Coherence] Discrepancies detected across {mismaFecha.Count + 1} analyses of FMI {resultado.MatriculaFMI} with the same date ({fechaTxt}): {resumenDisc}"
                                    : $"[Coherencia] Se detectaron discrepancias entre {mismaFecha.Count + 1} análisis del FMI {resultado.MatriculaFMI} con la misma fecha ({fechaTxt}): {resumenDisc}",
                                Recomendacion = en
                                    ? "Review the conflicting fields against the original SNR certificate."
                                    : "Revisar los campos con discrepancia directamente contra el certificado SNR original."
                            });
                        }

                        idsAReemplazar.AddRange(mismaFecha.Select(m => m.Id));
                        resumenDeduplicacion = comp.EsCoherente
                            ? (en
                                ? $"Coherence verified across {mismaFecha.Count + 1} files for FMI {resultado.MatriculaFMI} (same date): results are consistent."
                                : $"Coherencia verificada entre {mismaFecha.Count + 1} archivos del FMI {resultado.MatriculaFMI} (misma fecha): los resultados son coherentes.")
                            : (en
                                ? $"Coherence check across {mismaFecha.Count + 1} files for FMI {resultado.MatriculaFMI} found {comp.Discrepancias.Count} discrepancy(ies)."
                                : $"Verificación de coherencia entre {mismaFecha.Count + 1} archivos del FMI {resultado.MatriculaFMI} detectó {comp.Discrepancias.Count} discrepancia(s).");
                    }

                    if (anteriores.Count > 0)
                    {
                        var anteriorReciente = anteriores[0];
                        var dtoAnterior = JsonSerializer.Deserialize<AnalisisResultadoJsonDto>(anteriorReciente.ResultadoJson)
                            ?? new AnalisisResultadoJsonDto();

                        var obsCambio = ComparadorAnalisisService.ConstruirObservacionesCambio(
                            resultado, dtoAnterior, anteriorReciente.FechaExpedicionCertificado, fechaCertificado, idioma);

                        foreach (var obs in obsCambio)
                        {
                            if (!resultado.Observaciones.Any(o => string.Equals(o.Trim(), obs.Trim(), StringComparison.OrdinalIgnoreCase)))
                            {
                                resultado.Observaciones.Add(obs);
                            }
                        }

                        idsAReemplazar.AddRange(anteriores.Select(a => a.Id));
                        if (string.IsNullOrWhiteSpace(resumenDeduplicacion))
                        {
                            resumenDeduplicacion = en
                                ? $"Replaced {anteriores.Count} previous version(s) of FMI {resultado.MatriculaFMI} and transferred pertinent observations to this latest analysis."
                                : $"Se reemplazó {anteriores.Count} versión(es) anterior(es) del FMI {resultado.MatriculaFMI} y se trasladaron las observaciones pertinentes al último análisis.";
                        }
                    }
                }
            }
        }

        var model = resultado.ToDomainModel();

        var analisis = new AnalisisPredial
        {
            Id = nuevoId,
            EmpresaId = solicitud.EmpresaId,
            UsuarioId = solicitud.UsuarioId,
            MatriculaFMI = model.MatriculaFMI,
            ORIP = model.ORIP,
            NombrePredio = model.NombrePredio,
            Proposito = solicitud.Proposito,
            TipoDocumento = solicitud.TipoDocumento,
            Viabilidad = model.Viabilidad,
            ResumenEjecutivo = model.ResumenEjecutivo,
            ResultadoJson = JsonSerializer.Serialize(resultado),
            FechaAnalisis = DateTime.UtcNow,
            FechaExpedicionCertificado = fechaCertificado,
            Reemplazado = nuevoEsReemplazado,
            ReemplazadoPorId = nuevoReemplazadoPorId,
            Idioma = idioma,
            ConsumoTokens = consumoTokens
        };

        await _repository.GuardarDeduplicacionAsync(analisis, idsAReemplazar, nuevoId, cancellationToken);

        return new AnalisisPredialResponseDto
        {
            Id = analisis.Id,
            MatriculaFMI = analisis.MatriculaFMI,
            NombrePredio = analisis.NombrePredio,
            ORIP = model.ORIP,
            Departamento = model.Departamento,
            Municipio = model.Municipio,
            CedulaCatastral = model.CedulaCatastral,
            Proposito = analisis.Proposito,
            TipoDocumento = analisis.TipoDocumento,
            NombreTipoDocumento = analisis.TipoDocumento.ObtenerNombre(),
            Viabilidad = analisis.Viabilidad,
            ResumenEjecutivo = analisis.ResumenEjecutivo,
            Resultado = resultado,
            FechaAnalisis = analisis.FechaAnalisis,
            Idioma = idioma,
            FechaExpedicionCertificado = fechaCertificado,
            Reemplazado = nuevoEsReemplazado,
            ResumenDeduplicacion = resumenDeduplicacion,
            CoherenciaVerificada = coherenciaVerificada,
            DiscrepanciasCoherencia = discrepanciasCoherencia
        };
    }

    public async Task<ReporteWordDto> DescargarReporteWordAsync(
        Guid analisisId,
        CancellationToken cancellationToken = default)
    {
        DiagnosticoPredialModel model = await ObtenerModeloDominioAsync(analisisId, cancellationToken);

        byte[] bytes = await _diagnosticoWordGeneratorService.GenerarDiagnosticoWordAsync(model, cancellationToken);

        return new ReporteWordDto
        {
            Archivo = bytes,
            NombreArchivo = ConstruirNombreArchivo(model.MatriculaFMI, analisisId)
        };
    }

    public async Task<ReporteWordDto> DescargarAnexoTractoWordAsync(
        Guid analisisId,
        CancellationToken cancellationToken = default)
    {
        DiagnosticoPredialModel model = await ObtenerModeloDominioAsync(analisisId, cancellationToken);

        byte[] bytes = await _diagnosticoWordGeneratorService.GenerarAnexoTractoWordAsync(model, cancellationToken);

        return new ReporteWordDto
        {
            Archivo = bytes,
            NombreArchivo = ConstruirNombreAnexo(model.MatriculaFMI, analisisId)
        };
    }

    public async Task<List<AnalisisPredial>> ObtenerHistorialAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return await _repository.ObtenerHistorialPorUsuarioAsync(usuarioId, cancellationToken);
    }

    public async Task EnviarReportePorCorreoAsync(Guid analisisId, string destinatario, CancellationToken cancellationToken = default)
    {
        ReporteWordDto reporte = await DescargarReporteWordAsync(analisisId, cancellationToken);

        AnalisisPredial? analisis = await _repository.ObtenerPorIdAsync(analisisId, cancellationToken);
        string fmi = analisis?.MatriculaFMI ?? string.Empty;

        bool enviado = await _emailService.EnviarReporteAsync(destinatario, reporte.NombreArchivo, reporte.Archivo, fmi, cancellationToken);
        if (!enviado)
        {
            throw new InvalidOperationException("No se pudo enviar el correo. Por favor verifica las credenciales de correo en el servidor o intenta más tarde.");
        }
    }

    private async Task<DiagnosticoPredialModel> ObtenerModeloDominioAsync(Guid analisisId, CancellationToken cancellationToken)
    {
        AnalisisPredial? analisis = await _repository.ObtenerPorIdAsync(analisisId, cancellationToken);

        if (analisis is null)
        {
            throw new KeyNotFoundException("El análisis predial solicitado no existe.");
        }

        var dto = JsonSerializer.Deserialize<AnalisisResultadoJsonDto>(analisis.ResultadoJson)
            ?? new AnalisisResultadoJsonDto();

        var model = dto.ToDomainModel();
        model.Idioma = ComparadorAnalisisService.NormalizarIdioma(analisis.Idioma);
        return model;
    }

    private static string ConstruirNombreArchivo(string matriculaFmi, Guid analisisId)
    {
        string fmi = new string((matriculaFmi ?? string.Empty).Where(c => !System.IO.Path.GetInvalidFileNameChars().Contains(c)).ToArray()).Trim();

        return string.IsNullOrWhiteSpace(fmi)
            ? $"Informe_Juridico_Predial_{analisisId}.docx"
            : $"Informe_Juridico_Predial_FMI_{fmi}.docx";
    }

    private static string ConstruirNombreAnexo(string matriculaFmi, Guid analisisId)
    {
        string fmi = new string((matriculaFmi ?? string.Empty).Where(c => !System.IO.Path.GetInvalidFileNameChars().Contains(c)).ToArray()).Trim();

        return string.IsNullOrWhiteSpace(fmi)
            ? $"Anexo_Tracto_Sucesivo_{analisisId}.docx"
            : $"Anexo_Tracto_Sucesivo_FMI_{fmi}.docx";
    }

    private static EstadoViabilidad MapearViabilidad(string viabilidad)
    {
        return Enum.TryParse<EstadoViabilidad>(viabilidad, ignoreCase: true, out EstadoViabilidad estado)
            ? estado
            : EstadoViabilidad.RequiereRevision;
    }
}