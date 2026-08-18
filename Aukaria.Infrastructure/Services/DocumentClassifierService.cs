using System.Text.RegularExpressions;
using Aukaria.Application.DTOs;
using Aukaria.Application.Interfaces;
using Aukaria.Domain.Enums;

namespace Aukaria.Infrastructure.Services;

public sealed class DocumentClassifierService : IDocumentClassifierService
{
    private const int LimiteCaracteres = 2000;

    private static readonly Regex[] PatronesCtl =
    [
        new(@"CERTIFICADO\s+DE\s+TRADICION", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"MATRICULA\s+INMOBILIARIA", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"OFICINA\s+DE\s+REGISTRO", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
    ];

    private static readonly Regex[] PatronesVur =
    [
        new(@"VENTANILLA\s+UNICA\s+REGISTRAL", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"INFORMACION\s+REGISTRAL", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"\bSNC\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
    ];

    private static readonly Regex[] PatronesEscritura =
    [
        new(@"ESCRITURA\s+PUBLICA", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"NOTARIA", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"OTORGANTE", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"CUANTIA", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"COMPRAVENTA", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
    ];

    private static readonly Regex[] PatronesResolucion =
    [
        new(@"RESOLUCION(?:\s+ADMINISTRATIVA)?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"ACTO\s+ADMINISTRATIVO", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"EXPEDIENTE\s+(?:SANCIONATORIO|ADMINISTRATIVO)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"SUPERINTENDENCIA\s+DE\s+NOTARIADO\s+Y\s+REGISTRO", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
    ];

    private static readonly Regex PatronFmi =
        new(@"(?<![\d-])\b[A-Z0-9]{1,3}\s*-\s*\d{4,8}(?![\d-])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex PatronConsultaVur =
        new(@"(?:CONSULTA|REPORTE|VUR)[^\d]{0,12}([\dA-Z-]{4,20})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex PatronEscrituraNumero =
        new(@"ESCRITURA(?:\s+N[°º]\s*|\s+NUMERO\s+|\s*N[°º]\s*)?(\d{1,10})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex PatronNotaria =
        new(@"NOTARIA(?:\s+N[°º]?\s*|\s*N[°º]\s*)?(\d{1,3})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public ClasificacionDocumentoDto Clasificar(string textoExtraido)
    {
        if (string.IsNullOrWhiteSpace(textoExtraido))
        {
            return CrearGeneral("No fue posible extraer texto del documento para clasificarlo.");
        }

        string texto = textoExtraido.Length > LimiteCaracteres
            ? textoExtraido[..LimiteCaracteres]
            : textoExtraido;

        List<string> hitsCtl = Coincidencias(texto, PatronesCtl);
        List<string> hitsVur = Coincidencias(texto, PatronesVur);
        List<string> hitsEscritura = Coincidencias(texto, PatronesEscritura);
        List<string> hitsResolucion = Coincidencias(texto, PatronesResolucion);

        if (EsVur(hitsVur) || EsVurUnicoFuerte(hitsVur))
        {
            string consulta = ExtraerPrimera(texto, PatronConsultaVur) ?? string.Empty;
            string fmi = ExtraerPrimera(texto, PatronFmi) ?? string.Empty;
            string identificador = !string.IsNullOrWhiteSpace(consulta)
                ? $"Consulta {consulta}"
                : !string.IsNullOrWhiteSpace(fmi)
                    ? fmi
                    : "Reporte Registral";

            return new ClasificacionDocumentoDto
            {
                TipoDocumento = TipoDocumentoJuridico.VUR,
                NombreTipoDocumento = TipoDocumentoJuridico.VUR.ObtenerNombre(),
                IdentificadorExtraido = identificador,
                ResumenDeteccion = ResumenCon("VUR", hitsVur)
            };
        }

        if (EsResolucion(hitsResolucion))
        {
            return new ClasificacionDocumentoDto
            {
                TipoDocumento = TipoDocumentoJuridico.ResolucionActoAdministrativo,
                NombreTipoDocumento = TipoDocumentoJuridico.ResolucionActoAdministrativo.ObtenerNombre(),
                IdentificadorExtraido = string.Empty,
                ResumenDeteccion = ResumenCon("Resolución / Acto Administrativo", hitsResolucion)
            };
        }

        if (EsCtl(hitsCtl) ||
            (hitsCtl.Count == 1 && PatronFmi.IsMatch(texto) && !EsVur(hitsVur) && !EsEscritura(hitsEscritura) && !EsResolucion(hitsResolucion)))
        {
            string fmi = ExtraerPrimera(texto, PatronFmi) ?? string.Empty;
            return new ClasificacionDocumentoDto
            {
                TipoDocumento = TipoDocumentoJuridico.CTL,
                NombreTipoDocumento = TipoDocumentoJuridico.CTL.ObtenerNombre(),
                IdentificadorExtraido = string.IsNullOrWhiteSpace(fmi) ? "CTL SNR" : fmi,
                ResumenDeteccion = ResumenCon("CTL", hitsCtl)
            };
        }

        if (EsEscritura(hitsEscritura))
        {
            string escritura = ExtraerPrimera(texto, PatronEscrituraNumero) ?? string.Empty;
            string notaria = ExtraerPrimera(texto, PatronNotaria) ?? string.Empty;

            string identificador = !string.IsNullOrWhiteSpace(notaria)
                ? !string.IsNullOrWhiteSpace(escritura)
                    ? $"Escritura {escritura} · Notaría {notaria}"
                    : $"Notaría {notaria}"
                : !string.IsNullOrWhiteSpace(escritura)
                    ? $"Escritura {escritura}"
                    : "Escritura Pública";

            return new ClasificacionDocumentoDto
            {
                TipoDocumento = TipoDocumentoJuridico.EscrituraPublica,
                NombreTipoDocumento = TipoDocumentoJuridico.EscrituraPublica.ObtenerNombre(),
                IdentificadorExtraido = identificador,
                ResumenDeteccion = ResumenCon("Escritura Pública", hitsEscritura)
            };
        }

        string resumen = hitsCtl.Count + hitsVur.Count + hitsEscritura.Count + hitsResolucion.Count == 0
            ? "No se identificaron indicios de CTL, VUR, Escritura Pública o Resolución. Se asigna documento inmobiliario general."
            : $"Indicios insuficientes ({hitsCtl.Count + hitsVur.Count + hitsEscritura.Count + hitsResolucion.Count} de 2+ por tipo). Se asigna documento inmobiliario general.";

        return CrearGeneral(resumen);
    }

    private static bool EsCtl(List<string> hits) => hits.Count >= 2;

    private static bool EsVur(List<string> hits) => hits.Count >= 2;

    private static bool EsVurUnicoFuerte(List<string> hits) =>
        hits.Any(h => h.Equals("VENTANILLA UNICA REGISTRAL", StringComparison.OrdinalIgnoreCase));

    private static bool EsEscritura(List<string> hits) =>
        hits.Count >= 2 ||
        hits.Any(h => h.Equals("ESCRITURA PUBLICA", StringComparison.OrdinalIgnoreCase));

    private static bool EsResolucion(List<string> hits) => hits.Count >= 2;

    private static ClasificacionDocumentoDto CrearGeneral(string resumen)
    {
        return new ClasificacionDocumentoDto
        {
            TipoDocumento = TipoDocumentoJuridico.DocumentoInmobiliarioGeneral,
            NombreTipoDocumento = TipoDocumentoJuridico.DocumentoInmobiliarioGeneral.ObtenerNombre(),
            IdentificadorExtraido = string.Empty,
            ResumenDeteccion = resumen
        };
    }

    private static List<string> Coincidencias(string texto, Regex[] patrones)
    {
        var hits = new List<string>();
        foreach (Regex patron in patrones)
        {
            Match m = patron.Match(texto);
            if (m.Success)
            {
                string normalizado = Normalizar(m.Value);
                if (!hits.Contains(normalizado, StringComparer.OrdinalIgnoreCase))
                {
                    hits.Add(normalizado);
                }
            }
        }
        return hits;
    }

    private static string Normalizar(string valor)
    {
        return Regex.Replace(valor, @"\s+", " ").Trim().ToUpperInvariant();
    }

    private static string? ExtraerPrimera(string texto, Regex patron)
    {
        Match m = patron.Match(texto);
        return m.Success ? m.Value : null;
    }

    private static string ResumenCon(string tipo, List<string> hits)
    {
        string detalle = hits.Count > 0
            ? string.Join(", ", hits.Select(h => $"'{h}'"))
            : "patrones del tipo";
        return $"Clasificado como {tipo}. Indicios detectados: {detalle}.";
    }
}