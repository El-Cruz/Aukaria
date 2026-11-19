using Aukaria.Application.DTOs;
using Aukaria.Application.DTOs.JsonSchema;
using Aukaria.Application.DTOs.Requests;
using Aukaria.Application.DTOs.Responses;
using Aukaria.Application.Interfaces;
using Aukaria.Application.Services;
using Aukaria.Domain.Entities;
using Aukaria.Domain.Enums;
using Aukaria.Domain.Models;
using Aukaria.Tests.Fixtures;
using Xunit;

namespace Aukaria.Tests.UnitTests;

public sealed class AnalisisPredialServiceDedupTests
{
    private static readonly Guid EmpresaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UsuarioId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private sealed class StubPdfExtractor : IPdfExtractorService
    {
        public Task<ExtraccionPreAnalisisDto> ExtraerPreAnalisisAsync(Stream pdfStream, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ExtraccionPreAnalisisDto { MatriculaFMI = "50N-20825378" });

        public Task<string> ExtraerTextoCompletoAsync(Stream pdfStream, CancellationToken cancellationToken = default) =>
            Task.FromResult("FMI 50N-20825378");
    }

    private sealed class StubClassifier : IDocumentClassifierService
    {
        public ClasificacionDocumentoDto Clasificar(string texto) => new() { TipoDocumento = TipoDocumentoJuridico.CTL };
    }

    private sealed class StubWordGen : IDiagnosticoWordGeneratorService
    {
        public Task<byte[]> GenerarDiagnosticoWordAsync(DiagnosticoPredialModel model, CancellationToken cancellationToken = default) =>
            Task.FromResult(Array.Empty<byte>());
        public Task<byte[]> GenerarAnexoTractoWordAsync(DiagnosticoPredialModel model, CancellationToken cancellationToken = default) =>
            Task.FromResult(Array.Empty<byte>());
    }

    private sealed class StubEmail : IEmailService
    {
        public Task<bool> EnviarCorreoAsync(string d, string a, string c, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> EnviarReporteAsync(string d, string n, byte[] a, string f, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> EnviarOtpAsync(string d, string c, CancellationToken ct = default) => Task.FromResult(true);
    }

    private sealed class StubClaude : IClaudeApiService
    {
        private readonly Queue<AnalisisResultadoJsonDto> _respuestas;
        public StubClaude(params AnalisisResultadoJsonDto[] respuestas) => _respuestas = new Queue<AnalisisResultadoJsonDto>(respuestas);

        public Task<ClaudeAnalisisResultadoDto> AnalizarDocumentoAsync(
            string textoDocumento, PropositoAnalisis proposito, TipoDocumentoJuridico tipoDocumento, string idioma = "es", CancellationToken cancellationToken = default) =>
            Task.FromResult(new ClaudeAnalisisResultadoDto { Resultado = _respuestas.Dequeue(), InputTokens = 10, OutputTokens = 10 });

        public Task<ClaudeAnalisisResultadoDto> AnalizarDocumentoStreamingAsync(
            string textoDocumento, PropositoAnalisis proposito, TipoDocumentoJuridico tipoDocumento, Func<ProgresoAnalisisDto, Task> onProgreso, string idioma = "es", CancellationToken cancellationToken = default) =>
            AnalizarDocumentoAsync(textoDocumento, proposito, tipoDocumento, idioma, cancellationToken);
    }

    private static AnalisisPredialService CrearServicio(InMemoryTestAnalisisPredialRepository repo, params AnalisisResultadoJsonDto[] respuestas) =>
        new(repo, new StubPdfExtractor(), new StubClaude(respuestas), new StubClassifier(), new StubWordGen(), new StubEmail());

    private static AnalisisResultadoJsonDto CrearDto(string fechaCert, string propietario = "NICOLAS CRUZ", string viabilidad = "Viable", int anotaciones = 1)
    {
        var dto = new AnalisisResultadoJsonDto
        {
            MatriculaFMI = "50N-20825378",
            FechaExpedicionCertificado = fechaCert,
            PropietarioActual = propietario,
            EstadoFolio = "Activo",
            Viabilidad = viabilidad,
            AreaRegistrada = "500 m2",
            Observaciones = new List<string> { $"Obs del {fechaCert}" }
        };
        for (int i = 1; i <= anotaciones; i++)
        {
            dto.Anotaciones.Add(new AnotacionDto { NumeroAnotacion = $"00{i}", Fecha = "01/01/2024", CodigoSnr = "0125", Especificacion = "COMPRAVENTA" });
        }
        return dto;
    }

    [Fact]
    public async Task ProcesarMismaFecha_DetectaIncoherencia_YConservaElUltimo()
    {
        var repo = new InMemoryTestAnalisisPredialRepository();
        var svc = CrearServicio(repo,
            CrearDto("10/03/2026", propietario: "NICOLAS CRUZ", viabilidad: "Viable"),
            CrearDto("10/03/2026", propietario: "OTRO TITULAR", viabilidad: "RequiereRevision"));

        var req = new SolicitudAnalisisRequestDto { EmpresaId = EmpresaId, UsuarioId = UsuarioId, MatriculaFMI = "50N-20825378" };

        var r1 = await svc.ProcesarAnalisisCtlAsync(req, new MemoryStream());
        var r2 = await svc.ProcesarAnalisisCtlAsync(req, new MemoryStream());

        Assert.False(r2.CoherenciaVerificada);
        Assert.NotEmpty(r2.DiscrepanciasCoherencia);
        Assert.Contains(r2.Resultado.Observaciones, o => o.Contains("Inconsistencia de coherencia"));
        var vigentes = await repo.ListarVigentesPorFmiAsync("50N-20825378", EmpresaId);
        Assert.Single(vigentes);
        Assert.Equal(r2.Id, vigentes[0].Id);
        Assert.True((await repo.ObtenerPorIdAsync(r1.Id))!.Reemplazado);
    }

    [Fact]
    public async Task ProcesarFechaPosterior_ReemplazaAnteriorYTrasladaObservaciones()
    {
        var repo = new InMemoryTestAnalisisPredialRepository();
        var svc = CrearServicio(repo,
            CrearDto("10/03/2026", anotaciones: 1),
            CrearDto("25/04/2026", anotaciones: 2));

        var req = new SolicitudAnalisisRequestDto { EmpresaId = EmpresaId, UsuarioId = UsuarioId, MatriculaFMI = "50N-20825378" };

        var r1 = await svc.ProcesarAnalisisCtlAsync(req, new MemoryStream());
        var r2 = await svc.ProcesarAnalisisCtlAsync(req, new MemoryStream());

        Assert.False(r2.Reemplazado);
        Assert.True((await repo.ObtenerPorIdAsync(r1.Id))!.Reemplazado);
        Assert.Contains(r2.Resultado.Observaciones, o => o.Contains("Obs del 10/03/2026"));
        Assert.Contains(r2.Resultado.Observaciones, o => o.Contains("anotación(es) nueva(s)"));
    }

    [Fact]
    public async Task ProcesarFechaAnterior_ArchivaVersionAntiguaSinReemplazarVigente()
    {
        var repo = new InMemoryTestAnalisisPredialRepository();
        var svc = CrearServicio(repo,
            CrearDto("25/04/2026", anotaciones: 2),
            CrearDto("10/03/2026", anotaciones: 1));

        var req = new SolicitudAnalisisRequestDto { EmpresaId = EmpresaId, UsuarioId = UsuarioId, MatriculaFMI = "50N-20825378" };

        var r1 = await svc.ProcesarAnalisisCtlAsync(req, new MemoryStream());
        var r2 = await svc.ProcesarAnalisisCtlAsync(req, new MemoryStream());

        Assert.True(r2.Reemplazado);
        var vigentes = await repo.ListarVigentesPorFmiAsync("50N-20825378", EmpresaId);
        Assert.Single(vigentes);
        Assert.Equal(r1.Id, vigentes[0].Id);
    }
}
