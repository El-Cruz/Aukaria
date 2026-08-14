using Aukaria.Application.DTOs.JsonSchema;

namespace Aukaria.Application.Interfaces;

public interface IReportGeneratorService
{
    Task<byte[]> GenerarReporteWordAsync(AnalisisResultadoJsonDto resultadoJson, CancellationToken cancellationToken = default);
}