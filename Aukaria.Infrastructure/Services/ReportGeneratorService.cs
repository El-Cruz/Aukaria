using System.Text.RegularExpressions;
using Aukaria.Application.DTOs.JsonSchema;
using Aukaria.Application.Interfaces;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Aukaria.Infrastructure.Services;

public sealed class ReportGeneratorService : IReportGeneratorService
{
    private const string FuenteArial = "Arial";
    private const string AzulMarino = "002D62";
    private const string GrisCarbon = "333333";
    private const string GrisClaro = "D3D3D3";
    private const string FondoZebra = "F9FBFD";
    private const string Blanco = "FFFFFF";

    private const string TextoCeroRiesgos =
        "No se identifican riesgos jurídicos. El inmueble cuenta con cadena registral y tracto sucesivo saneados, y dominio consolidado en el titular según el Folio de Matrícula Inmobiliaria, encontrándose libre de gravámenes, medidas cautelares, falsa tradición o limitaciones que afecten la viabilidad de la servidumbre transitoria bajo la Ley 1274 de 2009.";

    public Task<byte[]> GenerarReporteWordAsync(
        AnalisisResultadoJsonDto resultadoJson,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => GenerarReporte(resultadoJson), cancellationToken);
    }

    private static byte[] GenerarReporte(AnalisisResultadoJsonDto resultadoJson)
    {
        using var ms = new MemoryStream();
        using (WordprocessingDocument documento = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = documento.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = mainPart.Document.AppendChild(new Body());

            CrearTituloPrincipal(body, resultadoJson);
            CrearCapitulo1(body, resultadoJson);
            CrearCapitulo2(body, resultadoJson);
            CrearCapitulo3(body, resultadoJson);
            CrearCapitulo4(body, resultadoJson);
            CrearCapitulo5(body, resultadoJson);
            CrearCapitulo6(body, resultadoJson);
            CrearCapitulo7(body, resultadoJson);
            CrearCapitulo8(body, resultadoJson);
            CrearCapitulo9(body, resultadoJson);
            CrearCapitulo10(body, resultadoJson);

            mainPart.Document.Save();
        }

        return ms.ToArray();
    }

    private static void CrearTituloPrincipal(Body body, AnalisisResultadoJsonDto resultadoJson)
    {
        string predio = Valor(resultadoJson.NombrePredio, "PREDIO SIN IDENTIFICAR").ToUpperInvariant();
        string fmi = Valor(resultadoJson.MatriculaFMI, "SIN FMI").ToUpperInvariant();

        body.Append(CrearTituloLinea("INFORME JURÍDICO PARA GESTIÓN PREDIAL"));
        body.Append(CrearTituloLinea("PROYECTO DE EXPLORACIÓN SÍSMICA"));
        body.Append(CrearTituloLinea($"PREDIO {predio} - FMI {fmi}"));
    }

    private static void CrearCapitulo1(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloCapitulo(body, "1. INFORMACIÓN GENERAL DEL PREDIO");

        string catastralNupre = string.Join(" / ", new[] { r.CedulaCatastral, r.Nupre }.Where(NoVacio));

        var pares = new (string Clave, string Valor)[]
        {
            ("Nombre del Predio", Valor(r.NombrePredio)),
            ("Vereda", Valor(r.Vereda)),
            ("Municipio", Valor(r.Municipio)),
            ("Departamento", Valor(r.Departamento)),
            ("Folio de Matrícula Inmobiliaria (FMI)", Valor(r.MatriculaFMI)),
            ("Cédula Catastral / NUPRE", NoVacio(catastralNupre) ? catastralNupre : "No reportado"),
            ("Oficina de Registro (ORIP)", Valor(r.ORIP)),
            ("Estado del Folio", Valor(r.EstadoFolio)),
            ("Folio Matriz", Valor(r.FolioMatriz)),
            ("Folios Derivados", Valor(r.FoliosDerivados)),
            ("Fecha de Expedición del Certificado", Valor(r.FechaExpedicionCertificado))
        };

        var filas = pares.Select(p => new[] { p.Clave, p.Valor }).ToArray();
        body.Append(CrearTabla(new[] { "CAMPO", "DETALLE" }, filas));
        body.Append(CrearParrafoSeparador());
    }

    private static void CrearCapitulo2(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloCapitulo(body, "2. ÁREAS Y CABIDAS REGISTRALES");

        CrearSubtitulo(body, "Origen y Cabida Actual");
        CrearCuerpo(body, NoVacio(r.OrigenCabidaActual)
            ? r.OrigenCabidaActual
            : $"Área registrada según el folio de matrícula inmobiliaria: {Valor(r.AreaRegistrada)}.");

        CrearSubtitulo(body, "Conclusión Predial");
        CrearCuerpo(body, NoVacio(r.ConclusionPredial)
            ? r.ConclusionPredial
            : "No se cuenta con información registrada para emitir la conclusión predial.");

        CrearSubtitulo(body, "Resumen Estructurado de Áreas");
        var filas = new[]
        {
            new[] { "Área Según FMI", Valor(r.AreaSegunFmi) },
            new[] { "Área en Adjudicación Inicial", Valor(r.AreaAdjudicacionInicial) },
            new[] { "Desenglobes / Ventas Parciales", Valor(r.DesenglobesVentasParciales) },
            new[] { "Área Remanente Real", Valor(r.AreaRemanenteReal) }
        };
        body.Append(CrearTabla(new[] { "CONCEPTO", "VALOR" }, filas));
        body.Append(CrearParrafoSeparador());
    }

    private static void CrearCapitulo3(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloCapitulo(body, "3. DESCRIPCIÓN FÍSICA Y LINDEROS");

        CrearSubtitulo(body, "Linderos Según Folio");
        CrearCuerpo(body, NoVacio(r.LinderosDescripcion) ? r.LinderosDescripcion : Valor(r.Linderos));

        CrearSubtitulo(body, "Soporte Documental");
        CrearCuerpo(body, Valor(r.SoporteDocumentalLinderos));
    }

    private static void CrearCapitulo4(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloCapitulo(body, "4. TITULARIDAD DEL INMUEBLE");

        CrearSubtitulo(body, "Titulares Registrados");

        var filas = new List<string[]>();
        foreach (TitularDto titular in r.Titulares)
        {
            filas.Add(new[] { Valor(titular.Nombre), Valor(titular.Identificacion), Valor(titular.ParticipacionCuota) });
        }

        if (filas.Count == 0)
        {
            filas.Add(new[] { Valor(r.PropietarioActual), "No reportado", "No reportado" });
        }

        body.Append(CrearTabla(new[] { "NOMBRE", "IDENTIFICACIÓN", "% PARTICIPACIÓN / CUOTA" }, filas));
        body.Append(CrearParrafoSeparador());

        CrearSubtitulo(body, "Soporte de Adquisición y Análisis del Régimen de Propiedad");
        CrearCuerpo(body, NoVacio(r.RegimenPropiedadAnalisis)
            ? r.RegimenPropiedadAnalisis
            : $"Titular reportado en el folio: {Valor(r.PropietarioActual)}.");
    }

    private static void CrearCapitulo5(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloCapitulo(body, "5. ANÁLISIS DE LA TRADICIÓN (TRACTO SUCESIVO)");

        CrearSubtitulo(body, "Tabla Cronológica de la Tradición");

        var filas = new List<string[]>();
        foreach (TradicionActoDto acto in r.TradicionActos)
        {
            filas.Add(new[] { Valor(acto.Anio), Valor(acto.ActoJuridico), Valor(acto.Anotacion), Valor(acto.AnalisisJuridico) });
        }

        if (filas.Count == 0)
        {
            foreach (AnotacionDto anotacion in r.Anotaciones)
            {
                filas.Add(new[]
                {
                    AnioDesdeFecha(anotacion.Fecha),
                    Valor(anotacion.Especificacion),
                    Valor(anotacion.NumeroAnotacion),
                    Valor(anotacion.NaturalezaJuridica)
                });
            }
        }

        if (filas.Count == 0)
        {
            CrearCuerpo(body, "Sin información de tracto sucesivo disponible en el folio analizado.");
        }
        else
        {
            body.Append(CrearTabla(new[] { "AÑO", "ACTO JURÍDICO", "ANOTACIÓN", "ANÁLISIS JURÍDICO" }, filas));
            body.Append(CrearParrafoSeparador());
        }

        CrearSubtitulo(body, "Certificación del Tracto");
        CrearCuerpo(body, Valor(r.CertificacionTracto));
    }

    private static void CrearCapitulo6(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloCapitulo(body, "6. ACTOS REGISTRALES RELEVANTES");

        if (r.Anotaciones.Count == 0)
        {
            CrearCuerpo(body, "No se registran actos relevantes en el folio de matrícula inmobiliaria.");
            return;
        }

        foreach (AnotacionDto anotacion in r.Anotaciones)
        {
            string numero = string.IsNullOrWhiteSpace(anotacion.NumeroAnotacion)
                ? "000"
                : anotacion.NumeroAnotacion.PadLeft(3, '0');

            string lineaAnotacion = $"Anotación N° {numero} ({Valor(anotacion.Fecha)}): {Valor(anotacion.Especificacion)}";
            if (NoVacio(anotacion.CodigoSnr))
            {
                lineaAnotacion += $" (Código {anotacion.CodigoSnr})";
            }

            body.Append(CrearParrafoDestacado(lineaAnotacion));

            string hecho = NoVacio(anotacion.HechoRegistral) ? anotacion.HechoRegistral : anotacion.NaturalezaJuridica;
            CrearCuerpo(body, $"Hecho Registral: {Valor(hecho)}");
            CrearCuerpo(body, $"Análisis Jurídico: {Valor(anotacion.AnalisisJuridico)}");
            body.Append(CrearParrafoSeparador());
        }
    }

    private static void CrearCapitulo7(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloCapitulo(body, "7. RIESGOS JURÍDICOS (MARCO LEY 1274 DE 2009)");

        if (r.AlertasJuridicas.Count == 0)
        {
            CrearCuerpo(body, TextoCeroRiesgos);
            return;
        }

        foreach (AlertaJuridicaDto alerta in r.AlertasJuridicas)
        {
            CrearCuerpo(body, $"[Riesgo: {Valor(alerta.NivelRiesgo)}] - {Valor(alerta.Titulo)}: {Valor(alerta.Descripcion)}");
            if (NoVacio(alerta.Recomendacion))
            {
                CrearCuerpo(body, $"Recomendación: {alerta.Recomendacion}", cursiva: true);
            }
            body.Append(CrearParrafoSeparador());
        }
    }

    private static void CrearCapitulo8(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloCapitulo(body, "8. DIAGNÓSTICO JURÍDICO EJECUTIVO");
        CrearCuerpo(body, NoVacio(r.DiagnosticoEjecutivo) ? r.DiagnosticoEjecutivo : Valor(r.ResumenEjecutivo));
    }

    private static void CrearCapitulo9(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloCapitulo(body, "9. OBSERVACIONES Y RECOMENDACIONES");

        if (r.Observaciones.Count == 0)
        {
            CrearCuerpo(body, "Sin observaciones adicionales.");
            return;
        }

        foreach (string observacion in r.Observaciones)
        {
            CrearVineta(body, observacion);
        }
    }

    private static void CrearCapitulo10(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloCapitulo(body, "10. DOCUMENTOS ANALIZADOS");

        if (r.DocumentosAnalizados.Count == 0)
        {
            CrearVineta(body, $"Certificado de Tradición y Libertad del FMI {Valor(r.MatriculaFMI)}.");
            return;
        }

        foreach (string documento in r.DocumentosAnalizados)
        {
            CrearVineta(body, documento);
        }
    }

    private static Paragraph CrearTituloLinea(string texto)
    {
        var parrafo = new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "480" },
                new Justification { Val = JustificationValues.Center }));
        parrafo.Append(CrearRun(texto, 15, AzulMarino, negrita: true));
        return parrafo;
    }

    private static void CrearTituloCapitulo(Body body, string titulo)
    {
        var parrafo = new Paragraph(
            new ParagraphProperties(
                new KeepNext(),
                new SpacingBetweenLines { Before = "360", After = "120" }));
        parrafo.Append(CrearRun(titulo, 13, AzulMarino, negrita: true));
        body.Append(parrafo);
    }

    private static void CrearSubtitulo(Body body, string texto)
    {
        var parrafo = new Paragraph(
            new ParagraphProperties(
                new KeepNext(),
                new SpacingBetweenLines { Before = "120", After = "120" }));
        parrafo.Append(CrearRun(texto, 11, GrisCarbon, negrita: true));
        body.Append(parrafo);
    }

    private static void CrearCuerpo(Body body, string texto, bool negrita = false, bool cursiva = false)
    {
        var parrafo = new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "120", Line = "276", LineRule = LineSpacingRuleValues.Auto },
                new Justification { Val = JustificationValues.Both }));
        parrafo.Append(CrearRun(texto, 11, GrisCarbon, negrita, cursiva));
        body.Append(parrafo);
    }

    private static void CrearVineta(Body body, string texto)
    {
        CrearCuerpo(body, $"• {texto}");
    }

    private static Paragraph CrearParrafoDestacado(string texto)
    {
        var parrafo = new Paragraph(
            new ParagraphProperties(
                new KeepNext(),
                new SpacingBetweenLines { Before = "0", After = "120" }));
        parrafo.Append(CrearRun(texto, 11, GrisCarbon, negrita: true));
        return parrafo;
    }

    private static Paragraph CrearParrafoSeparador()
    {
        return new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "120" }));
    }

    private static Run CrearRun(string texto, int tamanoPt, string colorHex, bool negrita = false, bool cursiva = false)
    {
        var propiedades = new RunProperties();
        propiedades.Append(new RunFonts { Ascii = FuenteArial, HighAnsi = FuenteArial, EastAsia = FuenteArial, ComplexScript = FuenteArial });

        if (negrita)
        {
            propiedades.Append(new Bold());
        }
        if (cursiva)
        {
            propiedades.Append(new Italic());
        }

        propiedades.Append(new Color { Val = colorHex });
        propiedades.Append(new FontSize { Val = (tamanoPt * 2).ToString() });
        propiedades.Append(new FontSizeComplexScript { Val = (tamanoPt * 2).ToString() });

        return new Run(propiedades, new Text(texto) { Space = SpaceProcessingModeValues.Preserve });
    }

    private static Table CrearTabla(string[] encabezados, IReadOnlyList<string[]> filas)
    {
        var tabla = new Table();
        tabla.Append(new TableProperties(
            new TableWidth { Width = "0", Type = TableWidthUnitValues.Auto },
            new TableJustification { Val = TableRowAlignmentValues.Center },
            new TableBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4, Color = GrisClaro },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Nil) },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4, Color = GrisClaro },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Nil) },
                new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4, Color = GrisClaro },
                new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Nil) })));

        var grid = new TableGrid();
        for (int columna = 0; columna < encabezados.Length; columna++)
        {
            int longitudMaxima = encabezados[columna].Length;
            foreach (string[] fila in filas)
            {
                longitudMaxima = Math.Max(longitudMaxima, fila[columna].Length);
            }
            grid.Append(new GridColumn { Width = Math.Clamp(longitudMaxima * 110, 1200, 4800).ToString() });
        }
        tabla.Append(grid);

        var filaEncabezado = new TableRow(new TableRowProperties(new TableHeader()));
        foreach (string encabezado in encabezados)
        {
            filaEncabezado.Append(CrearCelda(encabezado, esEncabezado: true, zebra: false));
        }
        tabla.Append(filaEncabezado);

        for (int i = 0; i < filas.Count; i++)
        {
            var fila = new TableRow();
            bool zebra = i % 2 == 1;
            foreach (string valor in filas[i])
            {
                fila.Append(CrearCelda(valor, esEncabezado: false, zebra: zebra));
            }
            tabla.Append(fila);
        }

        return tabla;
    }

    private static TableCell CrearCelda(string texto, bool esEncabezado, bool zebra)
    {
        var celda = new TableCell();
        var propiedades = new TableCellProperties();

        if (esEncabezado)
        {
            propiedades.Append(new Shading { Val = ShadingPatternValues.Clear, Fill = AzulMarino });
        }
        else if (zebra)
        {
            propiedades.Append(new Shading { Val = ShadingPatternValues.Clear, Fill = FondoZebra });
        }

        propiedades.Append(new TableCellMargin(
            new TopMargin { Width = "100", Type = TableWidthUnitValues.Dxa },
            new LeftMargin { Width = "150", Type = TableWidthUnitValues.Dxa },
            new BottomMargin { Width = "100", Type = TableWidthUnitValues.Dxa },
            new RightMargin { Width = "150", Type = TableWidthUnitValues.Dxa }));

        celda.Append(propiedades);
        celda.Append(CrearParrafoCelda(texto, esEncabezado));
        return celda;
    }

    private static Paragraph CrearParrafoCelda(string texto, bool esEncabezado)
    {
        var parrafo = new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { Before = "0", After = "0" }));
        parrafo.Append(CrearRun(texto, 10, esEncabezado ? Blanco : GrisCarbon, negrita: esEncabezado));
        return parrafo;
    }

    private static bool NoVacio(string valor) => !string.IsNullOrWhiteSpace(valor);

    private static string Valor(string valor, string fallback = "No reportado")
    {
        return NoVacio(valor) ? valor.Trim() : fallback;
    }

    private static string AnioDesdeFecha(string fecha)
    {
        if (NoVacio(fecha))
        {
            Match coincidencia = Regex.Match(fecha, @"(?<!\d)(19|20)\d{2}(?!\d)");
            if (coincidencia.Success)
            {
                return coincidencia.Value;
            }
        }

        return "—";
    }
}