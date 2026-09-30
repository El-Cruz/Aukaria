using Aukaria.Domain.Models;

namespace Aukaria.Application.Interfaces;

public interface IDiagnosticoWordGeneratorService
{
    Task<byte[]> GenerarDiagnosticoWordAsync(DiagnosticoPredialModel model, CancellationToken cancellationToken = default);
    Task<byte[]> GenerarAnexoTractoWordAsync(DiagnosticoPredialModel model, CancellationToken cancellationToken = default);
}
