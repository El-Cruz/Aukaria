using Aukaria.Application.DTOs.Requests;
using Aukaria.Application.DTOs.Responses;

namespace Aukaria.Application.Interfaces;

public interface IAnalisisPredialService
{
    Task<PreAnalisisFmiResponseDto> PreAnalizarFmiAsync(Stream pdfStream, Guid empresaId, CancellationToken cancellationToken = default);
    Task<AnalisisPredialResponseDto> ProcesarAnalisisCtlAsync(SolicitudAnalisisRequestDto solicitud, Stream pdfStream, CancellationToken cancellationToken = default);
    Task<byte[]> DescargarReporteWordAsync(Guid analisisId, CancellationToken cancellationToken = default);
}