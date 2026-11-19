using System.Globalization;
using System.Text.RegularExpressions;
using Aukaria.Application.DTOs.JsonSchema;

namespace Aukaria.Application.Services;

public sealed record ResultadoComparacionCoherencia(
    bool EsCoherente,
    List<string> Discrepancias);

public static class ComparadorAnalisisService
{
    private static readonly Dictionary<string, int> MesesEs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["enero"] = 1, ["febrero"] = 2, ["marzo"] = 3, ["abril"] = 4,
        ["mayo"] = 5, ["junio"] = 6, ["julio"] = 7, ["agosto"] = 8,
        ["septiembre"] = 9, ["setiembre"] = 9, ["octubre"] = 10, ["noviembre"] = 11, ["diciembre"] = 12
    };

    private static readonly Regex RegexFechaNumerica = new(
        @"\b(?<d>\d{1,2})[/\-](?<m>\d{1,2})[/\-](?<y>\d{4})\b|\b(?<y2>\d{4})[/\-](?<m2>\d{1,2})[/\-](?<d2>\d{1,2})\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex RegexFechaTextoEs = new(
        @"\b(?<d>\d{1,2})\s+de\s+(?<mes>[a-záéíóú]+)\s+(?:de|del)\s+(?<y>\d{4})\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static string NormalizarIdioma(string? idioma) =>
        !string.IsNullOrWhiteSpace(idioma) && idioma.Trim().StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? "en"
            : "es";

    public static DateOnly? ParsearFechaCertificado(AnalisisResultadoJsonDto? dto)
    {
        if (dto is null) return null;

        DateOnly? fechaCert = ParsearFechaTexto(dto.FechaExpedicionCertificado);
        if (fechaCert.HasValue) return fechaCert;

        DateOnly? maxAnotacion = null;
        foreach (var anot in dto.Anotaciones ?? Enumerable.Empty<AnotacionDto>())
        {
            DateOnly? f = ParsearFechaTexto(anot.Fecha);
            if (f.HasValue && (!maxAnotacion.HasValue || f.Value > maxAnotacion.Value))
            {
                maxAnotacion = f;
            }
        }

        return maxAnotacion;
    }

    public static DateOnly? ParsearFechaTexto(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;

        Match mNum = RegexFechaNumerica.Match(texto);
        if (mNum.Success)
        {
            int y, m, d;
            if (mNum.Groups["y"].Success)
            {
                d = int.Parse(mNum.Groups["d"].Value, CultureInfo.InvariantCulture);
                m = int.Parse(mNum.Groups["m"].Value, CultureInfo.InvariantCulture);
                y = int.Parse(mNum.Groups["y"].Value, CultureInfo.InvariantCulture);
            }
            else
            {
                y = int.Parse(mNum.Groups["y2"].Value, CultureInfo.InvariantCulture);
                m = int.Parse(mNum.Groups["m2"].Value, CultureInfo.InvariantCulture);
                d = int.Parse(mNum.Groups["d2"].Value, CultureInfo.InvariantCulture);
            }

            if (TryCrearFecha(y, m, d, out DateOnly fecha)) return fecha;
        }

        Match mEs = RegexFechaTextoEs.Match(texto);
        if (mEs.Success && MesesEs.TryGetValue(mEs.Groups["mes"].Value, out int mes))
        {
            int d = int.Parse(mEs.Groups["d"].Value, CultureInfo.InvariantCulture);
            int y = int.Parse(mEs.Groups["y"].Value, CultureInfo.InvariantCulture);
            if (TryCrearFecha(y, mes, d, out DateOnly fecha)) return fecha;
        }

        return null;
    }

    private static bool TryCrearFecha(int y, int m, int d, out DateOnly fecha)
    {
        if (y >= 1900 && y <= 2100 && m >= 1 && m <= 12 && d >= 1 && d <= DateTime.DaysInMonth(y, m))
        {
            fecha = new DateOnly(y, m, d);
            return true;
        }
        fecha = default;
        return false;
    }

    public static ResultadoComparacionCoherencia CompararCoherencia(
        AnalisisResultadoJsonDto nuevo,
        IReadOnlyList<AnalisisResultadoJsonDto> previos,
        string idioma = "es")
    {
        bool en = NormalizarIdioma(idioma) == "en";
        var discrepancias = new List<string>();

        for (int i = 0; i < previos.Count; i++)
        {
            AnalisisResultadoJsonDto previo = previos[i];
            string etiqueta = previos.Count > 1
                ? (en ? $"Analysis #{i + 1}" : $"Análisis #{i + 1}")
                : (en ? "Previous analysis" : "Análisis previo");

            if (!TextosEquivalentes(nuevo.Viabilidad, previo.Viabilidad))
            {
                discrepancias.Add(en
                    ? $"{etiqueta}: Viability differs ('{previo.Viabilidad}' vs '{nuevo.Viabilidad}')."
                    : $"{etiqueta}: Viabilidad difiere ('{previo.Viabilidad}' vs '{nuevo.Viabilidad}').");
            }

            if (!TextosEquivalentes(nuevo.EstadoFolio, previo.EstadoFolio))
            {
                discrepancias.Add(en
                    ? $"{etiqueta}: Folio status differs ('{previo.EstadoFolio}' vs '{nuevo.EstadoFolio}')."
                    : $"{etiqueta}: Estado del folio difiere ('{previo.EstadoFolio}' vs '{nuevo.EstadoFolio}').");
            }

            string propNuevo = NormalizarPropietarios(nuevo);
            string propPrevio = NormalizarPropietarios(previo);
            if (!string.Equals(propNuevo, propPrevio, StringComparison.Ordinal))
            {
                discrepancias.Add(en
                    ? $"{etiqueta}: Current owner(s) differ ('{previo.PropietarioActual}' vs '{nuevo.PropietarioActual}')."
                    : $"{etiqueta}: Propietario(s) actual(es) difieren ('{previo.PropietarioActual}' vs '{nuevo.PropietarioActual}').");
            }

            int countNuevo = nuevo.Anotaciones?.Count ?? 0;
            int countPrevio = previo.Anotaciones?.Count ?? 0;
            if (countNuevo != countPrevio)
            {
                discrepancias.Add(en
                    ? $"{etiqueta}: Annotation count differs ({countPrevio} vs {countNuevo})."
                    : $"{etiqueta}: Cantidad de anotaciones difiere ({countPrevio} vs {countNuevo}).");
            }

            int gravNuevo = ContarGravamenes(nuevo);
            int gravPrevio = ContarGravamenes(previo);
            if (gravNuevo != gravPrevio)
            {
                discrepancias.Add(en
                    ? $"{etiqueta}: Encumbrances/precautionary measures count differs ({gravPrevio} vs {gravNuevo})."
                    : $"{etiqueta}: Cantidad de gravámenes/medidas cautelares difiere ({gravPrevio} vs {gravNuevo}).");
            }

            string areaNuevo = !string.IsNullOrWhiteSpace(nuevo.AreaRemanenteReal) ? nuevo.AreaRemanenteReal : nuevo.AreaRegistrada;
            string areaPrevio = !string.IsNullOrWhiteSpace(previo.AreaRemanenteReal) ? previo.AreaRemanenteReal : previo.AreaRegistrada;
            if (!TextosEquivalentes(areaNuevo, areaPrevio))
            {
                discrepancias.Add(en
                    ? $"{etiqueta}: Registered/remaining area differs ('{areaPrevio}' vs '{areaNuevo}')."
                    : $"{etiqueta}: Área registrada/remanente difiere ('{areaPrevio}' vs '{areaNuevo}').");
            }
        }

        return new ResultadoComparacionCoherencia(discrepancias.Count == 0, discrepancias);
    }

    public static List<string> ConstruirObservacionesCambio(
        AnalisisResultadoJsonDto nuevo,
        AnalisisResultadoJsonDto previo,
        DateOnly? fechaPrevia,
        DateOnly? fechaNueva,
        string idioma = "es")
    {
        bool en = NormalizarIdioma(idioma) == "en";
        var observaciones = new List<string>();

        string fPrev = fechaPrevia?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? (en ? "previous version" : "versión anterior");
        string fNew = fechaNueva?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? (en ? "latest version" : "última versión");

        var prevKeys = (previo.Anotaciones ?? Enumerable.Empty<AnotacionDto>())
            .Select(ClaveAnotacion)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var nuevasAnotaciones = (nuevo.Anotaciones ?? Enumerable.Empty<AnotacionDto>())
            .Where(a => !prevKeys.Contains(ClaveAnotacion(a)))
            .ToList();

        if (nuevasAnotaciones.Count > 0)
        {
            string detalle = string.Join("; ", nuevasAnotaciones.Take(5).Select(a =>
                $"#{a.NumeroAnotacion} ({a.Fecha}) {a.Especificacion}".Trim()));
            observaciones.Add(en
                ? $"Changes from {fPrev} to {fNew}: {nuevasAnotaciones.Count} new annotation(s) detected: {detalle}."
                : $"Cambios desde {fPrev} hasta {fNew}: se detectaron {nuevasAnotaciones.Count} anotación(es) nueva(s): {detalle}.");
        }

        var prevGrav = (previo.Anotaciones ?? Enumerable.Empty<AnotacionDto>())
            .Where(a => a.EsGravamen || a.EsMedidaCautelar)
            .ToList();
        var newGrav = (nuevo.Anotaciones ?? Enumerable.Empty<AnotacionDto>())
            .Where(a => a.EsGravamen || a.EsMedidaCautelar)
            .ToList();

        var prevGravKeys = prevGrav.Select(ClaveAnotacion).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newGravKeys = newGrav.Select(ClaveAnotacion).ToHashSet(StringComparer.OrdinalIgnoreCase);

        int nuevosGrav = newGrav.Count(g => !prevGravKeys.Contains(ClaveAnotacion(g)));
        int levantados = prevGrav.Count(g => !newGravKeys.Contains(ClaveAnotacion(g)));

        if (nuevosGrav > 0)
        {
            observaciones.Add(en
                ? $"Update ({fPrev}): {nuevosGrav} new encumbrance(s) or precautionary measure(s) added."
                : $"Actualización ({fPrev}): se registraron {nuevosGrav} nuevo(s) gravamen(es) o medida(s) cautelar(es).");
        }

        if (levantados > 0)
        {
            observaciones.Add(en
                ? $"Update ({fPrev}): {levantados} previous encumbrance(s)/measure(s) no longer active."
                : $"Actualización ({fPrev}): {levantados} gravamen(es)/medida(s) de la versión anterior ya no figuran.");
        }

        if (!string.Equals(NormalizarPropietarios(nuevo), NormalizarPropietarios(previo), StringComparison.Ordinal))
        {
            observaciones.Add(en
                ? $"Owner change vs {fPrev}: '{previo.PropietarioActual}' → '{nuevo.PropietarioActual}'."
                : $"Cambio de titular respecto a {fPrev}: '{previo.PropietarioActual}' → '{nuevo.PropietarioActual}'.");
        }

        if (!TextosEquivalentes(nuevo.Viabilidad, previo.Viabilidad))
        {
            observaciones.Add(en
                ? $"Viability change vs {fPrev}: {previo.Viabilidad} → {nuevo.Viabilidad}."
                : $"Cambio de viabilidad respecto a {fPrev}: {previo.Viabilidad} → {nuevo.Viabilidad}.");
        }

        if (observaciones.Count == 0)
        {
            observaciones.Add(en
                ? $"Updated from certificate dated {fPrev} to {fNew}: no material changes in ownership, encumbrances, or viability."
                : $"Actualizado desde certificado del {fPrev} al {fNew}: sin cambios materiales en titularidad, gravámenes ni viabilidad.");
        }

        // Trasladar observaciones pertinentes de la versión anterior que no sean boilerplate repetido
        foreach (string obs in previo.Observaciones ?? Enumerable.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(obs)) continue;
            if (obs.Contains("RUNAP", StringComparison.OrdinalIgnoreCase) ||
                obs.Contains("OFAC", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!(nuevo.Observaciones ?? Enumerable.Empty<string>())
                .Any(n => string.Equals(n.Trim(), obs.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                observaciones.Add(obs.Trim());
            }
        }

        return observaciones;
    }

    private static string ClaveAnotacion(AnotacionDto a) =>
        $"{NormalizarTexto(a.NumeroAnotacion)}|{NormalizarTexto(a.CodigoSnr)}|{NormalizarTexto(a.Fecha)}";

    private static int ContarGravamenes(AnalisisResultadoJsonDto dto) =>
        (dto.Anotaciones ?? Enumerable.Empty<AnotacionDto>()).Count(a => a.EsGravamen || a.EsMedidaCautelar);

    private static string NormalizarPropietarios(AnalisisResultadoJsonDto dto)
    {
        if (dto.Titulares is { Count: > 0 })
        {
            return string.Join("|", dto.Titulares
                .Select(t => NormalizarTexto(t.Nombre))
                .Where(n => !string.IsNullOrEmpty(n))
                .OrderBy(n => n, StringComparer.Ordinal));
        }

        return NormalizarTexto(dto.PropietarioActual);
    }

    private static bool TextosEquivalentes(string? a, string? b) =>
        string.Equals(NormalizarTexto(a), NormalizarTexto(b), StringComparison.Ordinal);

    private static string NormalizarTexto(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        return Regex.Replace(texto.Trim().ToUpperInvariant(), @"\s+", " ");
    }
}
