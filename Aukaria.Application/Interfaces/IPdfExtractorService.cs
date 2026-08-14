namespace Aukaria.Application.Interfaces;

public interface IPdfExtractorService
{
    Task<string> ExtraerTextoCompletoAsync(Stream pdfStream, CancellationToken cancellationToken = default);
    Task<string> ExtraerTextoRapidoAsync(Stream pdfStream, int limiteCaracteres = 2000, CancellationToken cancellationToken = default);
    Task<string?> ExtraerFmiRapidoAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}