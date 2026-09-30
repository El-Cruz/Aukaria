using Aukaria.Domain.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Aukaria.Infrastructure.Services.Builders;

public static class TradicionSectionBuilder
{
    public static void Build(Body body, DiagnosticoPredialModel model)
    {
        CrearTituloSeccion(body, "A. TITULARIDAD REGISTRADA");

        var filas = new List<string[]>();
        foreach (var titular in model.Titulares)
        {
            filas.Add(new[]
            {
                CapitalizarPalabras(Valor(titular.Nombre)),
                Valor(titular.TipoDocumento),
                Valor(titular.NumeroDocumento),
                Valor(titular.Porcentaje)
            });
        }
        if (filas.Count == 0)
        {
            filas.Add(new[] { CapitalizarPalabras(Valor(model.PropietarioActual)), "No reportado", "No reportado", "No reportado" });
        }

        body.Append(CrearTablaBase(new[] { "NOMBRE", "IDENTIFICACIÓN", "CONDICIÓN DEL DOMINIO", "% PROPIEDAD" }, filas));
        body.Append(CrearParrafoSeparador());

        CrearTituloSeccion(body, "B. RÉGIMEN HISTÓRICO DE LA TRADICIÓN");

        var filasTradicion = new List<string[]>();
        foreach (var acto in model.TradicionActos)
        {
            filasTradicion.Add(new[]
            {
                Valor(acto.NumeroEscritura, "—"),
                Valor(acto.Fecha),
                Valor(acto.Otorgante),
                Valor(acto.Adquirente),
                Valor(acto.Cuantia)
            });
        }

        if (filasTradicion.Count == 0)
        {
            foreach (var anotacion in model.Anotaciones)
            {
                filasTradicion.Add(new[]
                {
                    Valor(anotacion.Numero, "—"),
                    Valor(anotacion.Fecha),
                    Valor(anotacion.Descripcion),
                    "—",
                    Valor(anotacion.Tipo)
                });
            }
        }

        if (filasTradicion.Count == 0)
        {
            CrearCuerpoJustificado(body, "Sin información de tracto sucesivo disponible en el folio analizado.");
            return;
        }

        body.Append(CrearTablaBase(new[] { "ANOTACIÓN N°", "FECHA", "ACTO JURÍDICO", "CADENA DE DOMINIO", "ANÁLISIS DE IMPACTO" }, filasTradicion));
        body.Append(CrearParrafoSeparador());
    }

    private static string Valor(string valor, string fallback = "")
    {
        return string.IsNullOrWhiteSpace(valor) ? fallback : valor;
    }

    private static string CapitalizarPalabras(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return texto;
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(texto.ToLowerInvariant());
    }

    private static void CrearTituloSeccion(Body body, string titulo)
    {
        var para = new Paragraph();
        var run = new Run();
        run.Append(new Text(titulo) { Space = SpaceProcessingModeValues.Preserve });
        run.RunProperties = new RunProperties
        {
            Bold = new Bold(),
            Color = new Color { Val = "0F3D2E" },
            FontSize = new FontSize { Val = "24" }
        };
        para.Append(run);
        body.Append(para);
    }

    private static Table CrearTablaBase(string[] headers, List<string[]> filas)
    {
        var tabla = new Table();
        tabla.Append(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableJustification { Val = TableRowAlignmentValues.Center },
            CrearBordesTablas()));

        var grid = new TableGrid();
        foreach (var _ in headers)
        {
            grid.Append(new GridColumn { Width = (9000 / headers.Length).ToString() });
        }
        tabla.Append(grid);

        var filaHeader = new TableRow();
        foreach (var header in headers)
        {
            filaHeader.Append(CrearCeldaDato(header, negrita: true, fondoHex: "F0FDF4"));
        }
        tabla.Append(filaHeader);

        foreach (var fila in filas)
        {
            var tableRow = new TableRow();
            foreach (var celda in fila)
            {
                tableRow.Append(CrearCeldaDato(celda));
            }
            tabla.Append(tableRow);
        }

        return tabla;
    }

    private static TableCell CrearCeldaDato(string texto, bool negrita = false, string fondoHex = null, int gridSpan = 1)
    {
        var cell = new TableCell();
        var para = new Paragraph();
        var run = new Run();
        run.Append(new Text(texto) { Space = SpaceProcessingModeValues.Preserve });

        var props = new RunProperties();
        if (negrita) props.Append(new Bold());
        props.Append(new Color { Val = "1F2937" });
        props.Append(new FontSize { Val = "20" });
        run.RunProperties = props;

        para.Append(run);

        var cellProps = new TableCellProperties();
        if (!string.IsNullOrEmpty(fondoHex))
        {
            cellProps.Append(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = fondoHex });
        }
        if (gridSpan > 1)
        {
            cellProps.Append(new GridSpan { Val = gridSpan });
        }
        cell.Append(cellProps);
        cell.Append(para);

        return cell;
    }

    private static TableBorders CrearBordesTablas()
    {
        return new TableBorders(
            new TopBorder { Val = BorderValues.Single, Size = 4, Color = "CBD5E1" },
            new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "CBD5E1" },
            new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "CBD5E1" },
            new RightBorder { Val = BorderValues.Single, Size = 4, Color = "CBD5E1" },
            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "CBD5E1" },
            new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "CBD5E1" });
    }

    private static Paragraph CrearParrafoSeparador()
    {
        var para = new Paragraph();
        var run = new Run();
        run.Append(new Text(" ") { Space = SpaceProcessingModeValues.Preserve });
        para.Append(run);
        return para;
    }

    private static void CrearCuerpoJustificado(Body body, string texto)
    {
        var para = new Paragraph();
        var run = new Run();
        run.Append(new Text(texto) { Space = SpaceProcessingModeValues.Preserve });
        run.RunProperties = new RunProperties
        {
            Color = new Color { Val = "1F2937" },
            FontSize = new FontSize { Val = "22" }
        };
        para.Append(run);
        para.ParagraphProperties = new ParagraphProperties
        {
            Justification = new Justification { Val = JustificationValues.Both }
        };
        body.Append(para);
    }
}
