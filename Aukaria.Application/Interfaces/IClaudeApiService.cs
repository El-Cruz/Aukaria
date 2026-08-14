using Aukaria.Application.DTOs.JsonSchema;
using Aukaria.Domain.Enums;

namespace Aukaria.Application.Interfaces;

public interface IClaudeApiService
{
    Task<AnalisisResultadoJsonDto> AnalizarDocumentoAsync(
        string textoDocumento,
        PropositoAnalisis proposito,
        TipoDocumentoJuridico tipoDocumento,
        CancellationToken cancellationToken = default);
}