using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aukaria.Application.DTOs.Responses;
using Aukaria.Domain.Enums;
using Aukaria.Tests.Fixtures;
using Aukaria.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Aukaria.Tests.SmokeTests;

public class PreAnalisisEndpointSmokeTest : IClassFixture<AukariaTestFactory>
{
    private readonly AukariaTestFactory _factory;
    private readonly ITestOutputHelper _output;

    public PreAnalisisEndpointSmokeTest(AukariaTestFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task PostPreAnalisis_ConPdfCtlMinimo_DebeRetornar200YMetadatosExtraidos()
    {
        // Arrange: Crear cliente HTTP desde WebApplicationFactory
        var client = _factory.CreateClient();

        byte[] pdfBytes = PdfTestHelper.GenerarPdfMinimoCtl(
            fmi: "50C-123456",
            orip: "BOGOTA",
            predio: "HACIENDA EL ENSUEÑO");

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "archivoPdf", "test-ctl-san-jose.pdf");

        var empresaId = Guid.NewGuid();
        content.Add(new StringContent(empresaId.ToString()), "empresaId");

        // Act: Enviar solicitud al endpoint de pre-análisis
        var response = await client.PostAsync("/api/AnalisisPredial/pre-analisis", content);

        string responseJson = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Respuesta HTTP {(int)response.StatusCode}: {responseJson}");

        // Assert: Validaciones de respuesta
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        jsonOptions.Converters.Add(new JsonStringEnumConverter());

        var resultado = JsonSerializer.Deserialize<PreAnalisisFmiResponseDto>(responseJson, jsonOptions);

        Assert.NotNull(resultado);
        Assert.Equal("50C-123456", resultado.MatriculaFMI);
        Assert.Contains("BOGOTA", resultado.Orip);
        Assert.False(resultado.ExistePrevio);
        Assert.NotNull(resultado.ClasificacionDocumento);
        Assert.Equal(TipoDocumentoJuridico.CTL, resultado.ClasificacionDocumento.TipoDocumento);
        _output.WriteLine($"FMI detectado: {resultado.MatriculaFMI}, ORIP: {resultado.Orip}, Tipo: {resultado.ClasificacionDocumento.NombreTipoDocumento}");
    }

    [Fact]
    public async Task PostPreAnalisis_ConArchivoInvalido_DebeRetornarBadRequest()
    {
        var client = _factory.CreateClient();

        using var content = new MultipartFormDataContent();
        var fakeFile = new ByteArrayContent(new byte[] { 0x00, 0x01, 0x02, 0x03 });
        fakeFile.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fakeFile, "archivoPdf", "danado.pdf");
        content.Add(new StringContent(Guid.NewGuid().ToString()), "empresaId");

        var response = await client.PostAsync("/api/AnalisisPredial/pre-analisis", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
