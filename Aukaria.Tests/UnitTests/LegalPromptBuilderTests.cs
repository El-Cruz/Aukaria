using Aukaria.Domain.Enums;
using Aukaria.Infrastructure.Services;
using Xunit;

namespace Aukaria.Tests.UnitTests;

public sealed class LegalPromptBuilderTests
{
    [Fact]
    public void ConstruirSystemPrompt_EnIngles_IncluyeRequisitoIdioma()
    {
        string prompt = LegalPromptBuilder.ConstruirSystemPrompt(
            PropositoAnalisis.CompraVenta,
            TipoDocumentoJuridico.CTL,
            "en");

        Assert.Contains("LANGUAGE REQUIREMENT (MANDATORY)", prompt);
        Assert.Contains("ENGLISH", prompt);
    }

    [Fact]
    public void ConstruirSystemPrompt_EnEspanol_NoIncluyeRequisitoIngles()
    {
        string prompt = LegalPromptBuilder.ConstruirSystemPrompt(
            PropositoAnalisis.CompraVenta,
            TipoDocumentoJuridico.CTL,
            "es");

        Assert.DoesNotContain("LANGUAGE REQUIREMENT (MANDATORY)", prompt);
    }
}
