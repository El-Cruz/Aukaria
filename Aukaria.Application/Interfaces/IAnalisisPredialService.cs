using Aukaria.Application.DTOs.Requests;
using Aukaria.Application.DTOs.Responses;
using Aukaria.Domain.Entities;

namespace Aukaria.Application.Interfaces;

public interface IAnalisisPredialService
{
    Task<PreAnalisisFmiResponseDto> PreAnalizarFmiAsync(Stream pdfStream, Guid empresaId, CancellationToken cancellationToken = default);
    Task<AnalisisPredialResponseDto> ProcesarAnalisisCtlAsync(SolicitudAnalisisRequestDto solicitud, Stream pdfStream, CancellationToken cancellationToken = default);
    Task<AnalisisPredialResponseDto> ProcesarAnalisisCtlStreamingAsync(
        SolicitudAnalisisRequestDto solicitud,
        Stream pdfStream,
        Func<ProgresoAnalisisDto, Task> onProgreso,
        CancellationToken cancellationToken = default);
    Task<List<AnalisisPredial>> ObtenerHistorialAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<ReporteWordDto> DescargarReporteWordAsync(Guid analisisId, CancellationToken cancellationToken = default);
    Task<ReporteWordDto> DescargarAnexoTractoWordAsync(Guid analisisId, CancellationToken cancellationToken = default);
    Task EnviarReportePorCorreoAsync(Guid analisisId, string destinatario, CancellationToken cancellationToken = default);
}