using Aukaria.Application.DTOs;
using Aukaria.Domain.Enums;

namespace Aukaria.Application.Interfaces;

public interface IClaudeApiService
{
    Task<ClaudeAnalisisResultadoDto> AnalizarDocumentoAsync(
        string textoDocumento,
        PropositoAnalisis proposito,
        TipoDocumentoJuridico tipoDocumento,
        CancellationToken cancellationToken = default);
}
