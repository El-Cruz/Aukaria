using System.Text;
using System.Text.RegularExpressions;
using Aukaria.Application.DTOs;
using Aukaria.Application.Exceptions;
using Aukaria.Application.Interfaces;
using UglyToad.PdfPig;

namespace Aukaria.Infrastructure.Services;

public sealed class PdfExtractorService : IPdfExtractorService
{
    private const int MaxPaginasPreAnalisis = 5;

    private static readonly Regex PatronFmiEtiquetado = new(
        @"(?:MATR[ÍI]CULA(?:\s+INMOBILIARIA)?|NRO\.?\s*MATR[ÍI]CULA|FMI|FOLIO(?:\s+DE\s+MATR[ÍI]CULA)?)[\s:]*[N°º#]?\s*[-–]?\s*([A-Z0-9]{1,3}\s*[-–]\s*\d{4,8})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex PatronFmiSucio = new(
        @"\b([A-Z0-9]{3}\s*[-–]\s*\d{4,8})\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex PatronFmiPartes = new(
        @"(?<prefijo>[A-Z0-9]{1,3})[\s–-]*[-–][\s–-]*(?<digitos>\d{4,8})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex PatronSenalFuerteClasificacion = new(
        @"(?:CERTIFICADO\s+DE\s+TRADICION|VENTANILLA\s+UNICA\s+REGISTRAL|ESCRITURA\s+PUBLICA|RESOLUCION|ACTO\s+ADMINISTRATIVO|OFICINA\s+DE\s+REGISTRO)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex PatronCedulaCatastral = new(
        @"C[ÉE]DULA\s+CATASTRAL[\s:]*[N°º#]?[\s:]*([\d\s.\-]{8,30})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex PatronNupre = new(
        @"\bNUPRE[\s:]*(\d{15,20})\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex PatronOrip = new(
        @"(?:ORIP|C[ÍI]RCULO\s+REGISTRAL)[\s:]*[N°º#]?[\s:]*(?:DE\s+)?([A-ZÁÉÍÓÚÑ][A-ZÁÉÍÓÚÑ0-9\.\-][A-ZÁÉÍÓÚÑ0-9\s\.\-]{2,40})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex PatronNombrePredio = new(
        @"NOMBRE(?:\s+DEL)?\s+PREDIO[\s:]*([^\n]{3,80})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex PatronMunicipio = new(
        @"MUNICIPIO(?:\s+O\s+VEREDA)?[\s:]*[N°º#]?[\s:]*(?:DE\s+)?([A-ZÁÉÍÓÚÑ][A-ZÁÉÍÓÚÑ\.\-][A-ZÁÉÍÓÚÑ0-9\s\.\-]{2,40})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex PatronEtiquetaSiguiente = new(
        @"(?:C[ÉE]DULA\s+CATASTRAL|MATR[ÍI]CULA\s+INMOBILIARIA|C[ÍI]RCULO\s+REGISTRAL|NOMBRE(?:\s+DEL)?\s+PREDIO|\bORIP\b|\bMUNICIPIO\b|\bANOTACION\b|\bNUPRE\b|\bCODIGO\b)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public Task<ExtraccionPreAnalisisDto> ExtraerPreAnalisisAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        if (pdfStream.CanSeek)
        {
            pdfStream.Position = 0;
        }

        return Task.Run(() => ExtraerPreAnalisis(pdfStream), cancellationToken);
    }

    public Task<string> ExtraerTextoCompletoAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        if (pdfStream.CanSeek)
        {
            pdfStream.Position = 0;
        }

        return Task.Run(() => ExtraerTextoCompleto(pdfStream), cancellationToken);
    }

    private static ExtraccionPreAnalisisDto ExtraerPreAnalisis(Stream pdfStream)
    {
        using PdfDocument pdf = AbrirPdfSeguro(pdfStream);

        int totalPaginas = pdf.NumberOfPages;
        int maxPaginas = Math.Min(totalPaginas, MaxPaginasPreAnalisis);

        var textoAcumulado = new StringBuilder();
        string? fmi = null;

        for (int i = 1; i <= maxPaginas; i++)
        {
            string textoPagina = pdf.GetPage(i).Text;
            textoAcumulado.AppendLine(textoPagina);

            if (fmi is null)
            {
                fmi = ExtraerFmiNormalizado(textoPagina);
            }

            if (fmi is not null && PatronSenalFuerteClasificacion.IsMatch(textoAcumulado.ToString()))
            {
                break;
            }
        }

        string texto = textoAcumulado.ToString();

        if (fmi is null)
        {
            fmi = ExtraerFmiNormalizado(texto);
        }

        return new ExtraccionPreAnalisisDto
        {
            MatriculaFMI = fmi ?? string.Empty,
            CedulaCatastral = NormalizarCatastral(ExtraerPrimera(texto, PatronCedulaCatastral)),
            Nupre = NormalizarMetadato(ExtraerPrimera(texto, PatronNupre)),
            Orip = NormalizarMetadato(ExtraerPrimera(texto, PatronOrip)),
            NombrePredio = NormalizarMetadato(ExtraerPrimera(texto, PatronNombrePredio)),
            Municipio = NormalizarMetadato(ExtraerPrimera(texto, PatronMunicipio)),
            TextoExtraido = texto
        };
    }

    private static string? ExtraerFmiNormalizado(string texto)
    {
        string? captura = ExtraerPrimera(texto, PatronFmiEtiquetado)
            ?? ExtraerPrimera(texto, PatronFmiSucio);
        if (captura is null)
        {
            return null;
        }

        Match partes = PatronFmiPartes.Match(captura);
        return partes.Success
            ? $"{partes.Groups["prefijo"].Value.ToUpperInvariant()}-{partes.Groups["digitos"].Value}"
            : null;
    }

    private static string? ExtraerPrimera(string texto, Regex patron)
    {
        Match m = patron.Match(texto);
        return m.Success ? (m.Groups[1].Success ? m.Groups[1].Value : m.Value) : null;
    }

    private static string NormalizarCatastral(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        return Regex.Replace(Regex.Replace(valor.Trim(), @"\s+", ""), @"\.", "-");
    }

    private static string NormalizarMetadato(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        valor = RecortarEnSiguienteEtiqueta(valor);
        return Regex.Replace(valor.Trim().TrimEnd(':'), @"\s+", " ").Trim().ToUpperInvariant();
    }

    private static string RecortarEnSiguienteEtiqueta(string valor)
    {
        Match etiqueta = PatronEtiquetaSiguiente.Match(valor);
        if (etiqueta.Success && etiqueta.Index > 0)
        {
            return valor[..etiqueta.Index];
        }

        return valor;
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