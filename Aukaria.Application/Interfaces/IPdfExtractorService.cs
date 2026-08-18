using Aukaria.Application.DTOs;

namespace Aukaria.Application.Interfaces;

public interface IPdfExtractorService
{
    Task<ExtraccionPreAnalisisDto> ExtraerPreAnalisisAsync(Stream pdfStream, CancellationToken cancellationToken = default);
    Task<string> ExtraerTextoCompletoAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}