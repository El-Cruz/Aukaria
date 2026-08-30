using Aukaria.Application.DTOs.JsonSchema;

namespace Aukaria.Application.DTOs;

public class ClaudeAnalisisResultadoDto
{
    public AnalisisResultadoJsonDto Resultado { get; set; } = new();

    public int InputTokens { get; set; }

    public int OutputTokens { get; set; }

    public int TotalTokens => InputTokens + OutputTokens;
}
