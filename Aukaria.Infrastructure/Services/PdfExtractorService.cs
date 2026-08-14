using System.Text;
using System.Text.RegularExpressions;
using Aukaria.Application.Exceptions;
using Aukaria.Application.Interfaces;
using UglyToad.PdfPig;

namespace Aukaria.Infrastructure.Services;

public sealed class PdfExtractorService : IPdfExtractorService
{
    private static readonly Regex[] FmiPatterns =
    [
        new(@"Nro\s*Matrícula:\s*(\d{3}-\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"MATRICULA\s*INMOBILIARIA\s*:?\s*(\d{3}-\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"\b\d{3}-\d+\b", RegexOptions.CultureInvariant)
    ];

    public Task<string?> ExtraerFmiRapidoAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        if (pdfStream.CanSeek)
        {
            pdfStream.Position = 0;
        }

        return Task.Run(() => ExtraerFmiRapido(pdfStream), cancellationToken);
    }

    public Task<string> ExtraerTextoCompletoAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        if (pdfStream.CanSeek)
        {
            pdfStream.Position = 0;
        }

        return Task.Run(() => ExtraerTextoCompleto(pdfStream), cancellationToken);
    }

    public Task<string> ExtraerTextoRapidoAsync(Stream pdfStream, int limiteCaracteres = 2000, CancellationToken cancellationToken = default)
    {
        if (pdfStream.CanSeek)
        {
            pdfStream.Position = 0;
        }

        return Task.Run(() => ExtraerTextoRapido(pdfStream, limiteCaracteres), cancellationToken);
    }

    private static string ExtraerTextoRapido(Stream pdfStream, int limiteCaracteres)
    {
        using PdfDocument pdf = AbrirPdfSeguro(pdfStream);

        var sb = new StringBuilder();
        foreach (var page in pdf.GetPages())
        {
            sb.AppendLine(page.Text);
            if (sb.Length >= limiteCaracteres)
            {
                break;
            }
        }

        return sb.Length > limiteCaracteres
            ? sb.ToString()[..limiteCaracteres]
            : sb.ToString();
    }

    private static string? ExtraerFmiRapido(Stream pdfStream)
    {
        using PdfDocument pdf = AbrirPdfSeguro(pdfStream);
        string primeraPagina = pdf.GetPage(1).Text;

        foreach (Regex patron in FmiPatterns)
        {
            Match coincidencia = patron.Match(primeraPagina);
            if (coincidencia.Success)
            {
                return coincidencia.Groups[1].Success ? coincidencia.Groups[1].Value : coincidencia.Value;
            }
        }

        return null;
    }

    private static string ExtraerTextoCompleto(Stream pdfStream)
    {
        using PdfDocument pdf = AbrirPdfSeguro(pdfStream);

        var sb = new StringBuilder();
        foreach (var page in pdf.GetPages())
        {
            sb.AppendLine($"--- PAGE {page.Number} ---");
            sb.AppendLine(page.Text);
        }

        return sb.ToString();
    }

    private static PdfDocument AbrirPdfSeguro(Stream pdfStream)
    {
        try
        {
            return PdfDocument.Open(pdfStream);
        }
        catch (Exception ex) when (ex is not OperationCanceledException
                                       and not OutOfMemoryException
                                       and not StackOverflowException)
        {
            throw new PdfInvalidoException("El archivo adjuntado no es un PDF válido o está dañado.");
        }
    }
}