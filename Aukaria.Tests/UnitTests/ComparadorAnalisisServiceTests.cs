using Aukaria.Application.DTOs.JsonSchema;
using Aukaria.Application.Services;
using Xunit;

namespace Aukaria.Tests.UnitTests;

public sealed class ComparadorAnalisisServiceTests
{
    private static AnalisisResultadoJsonDto CrearBase(string propietario = "JUAN PEREZ", string viabilidad = "Viable") => new()
    {
        MatriculaFMI = "50N-20825378",
        PropietarioActual = propietario,
        EstadoFolio = "Activo",
        Viabilidad = viabilidad,
        AreaRegistrada = "1000 m2",
        FechaExpedicionCertificado = "15/03/2026",
        Anotaciones = new List<AnotacionDto>
        {
            new() { NumeroAnotacion = "001", Fecha = "10/01/2020", CodigoSnr = "0125", Especificacion = "COMPRAVENTA" }
        },
        Observaciones = new List<string> { "Observación registral relevante sobre lindero norte." }
    };

    [Fact]
    public void CompararCoherencia_ResultadosIguales_EsCoherente()
    {
        var a1 = CrearBase();
        var a2 = CrearBase();

        var resultado = ComparadorAnalisisService.CompararCoherencia(a1, new[] { a2 }, "es");

        Assert.True(resultado.EsCoherente);
        Assert.Empty(resultado.Discrepancias);
    }

    [Fact]
    public void CompararCoherencia_PropietarioYViabilidadDistintos_DetectaDiscrepancias()
    {
        var nuevo = CrearBase(propietario: "MARIA GOMEZ", viabilidad: "RequiereRevision");
        var previo = CrearBase(propietario: "JUAN PEREZ", viabilidad: "Viable");

        var resultado = ComparadorAnalisisService.CompararCoherencia(nuevo, new[] { previo }, "es");

        Assert.False(resultado.EsCoherente);
        Assert.Contains(resultado.Discrepancias, d => d.Contains("Propietario", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(resultado.Discrepancias, d => d.Contains("Viabilidad", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ConstruirObservacionesCambio_GravamenNuevoYNuevaAnotacion_ResumeCambiosYTrasladaObservacion()
    {
        var previo = CrearBase();
        var nuevo = CrearBase();
        nuevo.FechaExpedicionCertificado = "20/05/2026";
        nuevo.Anotaciones.Add(new AnotacionDto
        {
            NumeroAnotacion = "002",
            Fecha = "01/05/2026",
            CodigoSnr = "0427",
            Especificacion = "EMBARGO",
            EsMedidaCautelar = true
        });
        nuevo.Observaciones = new List<string>();

        var obs = ComparadorAnalisisService.ConstruirObservacionesCambio(
            nuevo, previo, new DateOnly(2026, 3, 15), new DateOnly(2026, 5, 20), "es");

        Assert.Contains(obs, o => o.Contains("1 anotación(es) nueva(s)", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(obs, o => o.Contains("gravamen(es) o medida(s)", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(obs, o => o.Contains("Observación registral relevante sobre lindero norte.", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParsearFechaTexto_SoportaFormatosColombianos()
    {
        Assert.Equal(new DateOnly(2026, 3, 15), ComparadorAnalisisService.ParsearFechaTexto("15/03/2026 14:22:00"));
        Assert.Equal(new DateOnly(2026, 3, 15), ComparadorAnalisisService.ParsearFechaTexto("15 de marzo de 2026"));
        Assert.Equal(new DateOnly(2026, 3, 15), ComparadorAnalisisService.ParsearFechaTexto("2026-03-15"));
    }
}
