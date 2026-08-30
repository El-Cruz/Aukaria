using Aukaria.Application.DTOs;
using Aukaria.Application.DTOs.Responses;
using Aukaria.Domain.Enums;

namespace Aukaria.Application.Interfaces;

public interface IClaudeApiService
{
    Task<ClaudeAnalisisResultadoDto> AnalizarDocumentoAsync(
        string textoDocumento,
        PropositoAnalisis proposito,
        TipoDocumentoJuridico tipoDocumento,
        CancellationToken cancellationToken = default);

    Task<ClaudeAnalisisResultadoDto> AnalizarDocumentoStreamingAsync(
        string textoDocumento,
        PropositoAnalisis proposito,
        TipoDocumentoJuridico tipoDocumento,
        Func<ProgresoAnalisisDto, Task> onProgreso,
        CancellationToken cancellationToken = default);
}
