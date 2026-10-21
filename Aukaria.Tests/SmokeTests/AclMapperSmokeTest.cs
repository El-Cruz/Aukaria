using Aukaria.Application.DTOs.JsonSchema;
using Aukaria.Application.Mappers;
using Aukaria.Domain.Enums;
using Aukaria.Domain.Models;
using Xunit;

namespace Aukaria.Tests.SmokeTests;

public class AclMapperSmokeTest
{
    [Fact]
    public void AnalisisPredialMapper_DebeMapearJsonDtoADominio_SinExcepcionesDeCasting()
    {
        // Arrange: DTO crudo con estructura completa de Claude
        var rawDto = new AnalisisResultadoJsonDto
        {
            MatriculaFMI = "50C-998877",
            ORIP = "MEDELLIN SUR",
            Departamento = "ANTIOQUIA",
            Municipio = "MEDELLIN",
            Vereda = "SAN CRISTOBAL",
            NombrePredio = "FINCA LA ILUSION",
            AreaRegistrada = "1500 m2",
            PropietarioActual = "JUAN CARLOS RESTREPO",
            EstadoFolio = "ACTIVO",
            Viabilidad = "Requiere Revision",
            ResumenEjecutivo = "Predio con limitación por servidumbre de acueducto.",
            Anotaciones = new List<AnotacionDto>
            {
                new()
                {
                    NumeroAnotacion = "001",
                    Fecha = "2018-05-12",
                    Especificacion = "COMPRAVENTA",
                    NaturalezaJuridica = "Dominio Pleno"
                },
                new()
                {
                    NumeroAnotacion = "002",
                    Fecha = "2021-11-04",
                    Especificacion = "SERVIDUMBRE",
                    NaturalezaJuridica = "Servidumbre Pasiva"
                }
            },
            AlertasJuridicas = new List<AlertaJuridicaDto>
            {
                new()
                {
                    NivelRiesgo = "Medio",
                    Descripcion = "Existe servidumbre no cancelada en anotación 002.",
                    Recomendacion = "Revisar escrituración y trazado de servidumbre."
                }
            },
            Observaciones = new List<string>
            {
                "Folio con tracto sucesivo continuo.",
                "No registra gravámenes hipotecarios vigentes."
            },
            ObservacionAmbiental = "No colinda con áreas protegidas RUNAP.",
            ExclusionResponsabilidad = "Auditoría circunscrita a los documentos aportados.",
            NumeroEscritura = "1234",
            Notaria = "Segunda de Medellín",
            CiudadNotaria = "Medellín",
            FechaEscritura = "2018-04-30",
            Otorgantes = new List<string> { "PEDRO PEREZ" },
            Cuantia = "$250.000.000 COP",
            Linderos = "Por el norte con predio matriz; por el sur con vía pública.",
            CedulaCatastral = "01-02-003-0004-000",
            Nupre = "998877665544332211",
            FolioMatriz = "50C-112233",
            FoliosDerivados = "50C-998878",
            FechaExpedicionCertificado = "2026-08-10",
            OrigenCabidaActual = "Partición de herencia",
            ConclusionPredial = "Inmueble viable condicionado al levantamiento o aceptación de servidumbre.",
            AreaSegunFmi = "1500.00 m2",
            AreaAdjudicacionInicial = "1500.00 m2",
            DesenglobesVentasParciales = "Ninguna",
            AreaRemanenteReal = "1500.00 m2",
            LinderosDescripcion = "Linderos técnicos consolidados en escritura 1234.",
            SoporteDocumentalLinderos = "Escritura 1234 Notaría 2 Medellín",
            Titulares = new List<TitularDto>
            {
                new()
                {
                    Nombre = "JUAN CARLOS RESTREPO",
                    Identificacion = "CC 70123456",
                    CondicionDominio = "PROPIEDAD TOTAL",
                    ParticipacionCuota = "100%"
                }
            },
            RegimenPropiedadAnalisis = "Común individual",
            TradicionActos = new List<TradicionActoDto>
            {
                new()
                {
                    NumeroAnotacion = "001",
                    Fecha = "2018-05-12",
                    CodigoSnr = "0125",
                    ActoJuridico = "Compraventa",
                    CadenaDeDominio = "De PEDRO PEREZ a JUAN RESTREPO",
                    AnalisisJuridico = "Título y modo legalmente consolidados."
                }
            },
            CertificacionTracto = "Tracto sucesivo ininterrumpido en los últimos 20 años.",
            DiagnosticoEjecutivo = "Riesgo moderado mitigable.",
            DocumentosAnalizados = new List<string> { "Certificado de Tradición y Libertad 50C-998877" }
        };

        // Act: Traducir a través del Mapper ACL
        DiagnosticoPredialModel model = rawDto.ToDomainModel();

        // Assert: Validaciones estructurales y de casting
        Assert.NotNull(model);
        Assert.Equal("50C-998877", model.MatriculaFMI);
        Assert.Equal("MEDELLIN SUR", model.ORIP);
        Assert.Equal("JUAN CARLOS RESTREPO", model.PropietarioActual);
        Assert.Equal(EstadoViabilidad.RequiereRevision, model.Viabilidad);
        Assert.Equal(2, model.Anotaciones.Count);
        Assert.Single(model.AlertasJuridicas);
        Assert.Equal("Medio", model.AlertasJuridicas[0].NivelRiesgo);
        Assert.Single(model.Titulares);
        Assert.Equal("100%", model.Titulares[0].Porcentaje);
        Assert.Single(model.TradicionActos);
        Assert.Equal(2, model.Observaciones.Count);

        // Act 2: Roundtrip a DTO
        AnalisisResultadoJsonDto roundtripDto = model.ToDto();
        Assert.NotNull(roundtripDto);
        Assert.Equal("50C-998877", roundtripDto.MatriculaFMI);
        Assert.Equal(EstadoViabilidad.RequiereRevision.ToString(), roundtripDto.Viabilidad);
    }

    [Theory]
    [InlineData("Viable", EstadoViabilidad.Viable)]
    [InlineData("VIABLE", EstadoViabilidad.Viable)]
    [InlineData("RequiereRevision", EstadoViabilidad.RequiereRevision)]
    [InlineData("AlertaCritica", EstadoViabilidad.AlertaCritica)]
    [InlineData("NoExiste_ValorInvalido", EstadoViabilidad.RequiereRevision)] // Fallback defensivo
    public void AnalisisPredialMapper_DebeMapearEnumsDefensivamente(string viabilidadString, EstadoViabilidad esperado)
    {
        var dto = new AnalisisResultadoJsonDto
        {
            MatriculaFMI = "50C-100",
            Viabilidad = viabilidadString
        };

        var model = dto.ToDomainModel();

        Assert.Equal(esperado, model.Viabilidad);
    }
}
