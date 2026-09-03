using Aukaria.Application.DTOs.JsonSchema;

namespace Aukaria.Application.Interfaces;

public interface IDiagnosticoWordGeneratorService
{
    Task<byte[]> GenerarDiagnosticoWordAsync(AnalisisResultadoJsonDto resultadoJson, CancellationToken cancellationToken = default);
    Task<byte[]> GenerarAnexoTractoWordAsync(AnalisisResultadoJsonDto resultadoJson, CancellationToken cancellationToken = default);
}
