using Aukaria.Application.DTOs.JsonSchema;
using Aukaria.Application.Interfaces;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Aukaria.Infrastructure.Services;

public sealed class ReportGeneratorService : IReportGeneratorService
{
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

            CrearEncabezado(body, resultadoJson);
            CrearTablaInfoGeneral(body, resultadoJson);
            CrearSeccionResumenEjecutivo(body, resultadoJson);
            CrearSeccionAlertasJuridicas(body, resultadoJson);
            CrearSeccionAnotaciones(body, resultadoJson);
            CrearSeccionObservaciones(body, resultadoJson);

            mainPart.Document.Save();
        }

        return ms.ToArray();
    }

    private static void CrearEncabezado(Body body, AnalisisResultadoJsonDto resultadoJson)
    {
        Paragraph titulo = CrearParrafo("AUKARIA - INFORME JURÍDICO PARA GESTIÓN PREDIAL", negrita: true, tamaño: 20, centrado: true);
        Paragraph subtitulo = CrearParrafo(
            $"Predio: {resultadoJson.NombrePredio} | FMI: {resultadoJson.MatriculaFMI} | Estado Viabilidad: {resultadoJson.Viabilidad}",
            negrita: false, tamaño: 12, centrado: true);

        body.Append(titulo, subtitulo);
    }

    private static void CrearTablaInfoGeneral(Body body, AnalisisResultadoJsonDto resultadoJson)
    {
        body.Append(CrearTituloSeccion("1. Información General"));

        string[,] pares =
        {
            { "FMI", resultadoJson.MatriculaFMI },
            { "ORIP", resultadoJson.ORIP },
            { "Municipio", resultadoJson.Municipio },
            { "Departamento", resultadoJson.Departamento },
            { "Propietario Actual", resultadoJson.PropietarioActual },
            { "Área Registrada", resultadoJson.AreaRegistrada },
            { "Estado del Folio", resultadoJson.EstadoFolio },
            { "Vereda", resultadoJson.Vereda }
        };

        var tabla = new Table();
        var propiedadesTabla = new TableProperties(
            new TableBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 }));
        tabla.Append(propiedadesTabla);

        for (int i = 0; i < pares.GetLength(0); i++)
        {
            var fila = new TableRow();
            fila.Append(
                CrearCeldaTabla(pares[i, 0], negrita: true),
                CrearCeldaTabla(pares[i, 1], negrita: false));
            tabla.Append(fila);
        }

        body.Append(tabla);
        body.Append(CrearParrafo(string.Empty));
    }

    private static void CrearSeccionResumenEjecutivo(Body body, AnalisisResultadoJsonDto resultadoJson)
    {
        body.Append(CrearTituloSeccion("2. Resumen Ejecutivo"));
        body.Append(CrearParrafo(resultadoJson.ResumenEjecutivo, justificado: true));
    }

    private static void CrearSeccionAlertasJuridicas(Body body, AnalisisResultadoJsonDto resultadoJson)
    {
        body.Append(CrearTituloSeccion("3. Alertas Jurídicas Detectadas"));

        if (resultadoJson.AlertasJuridicas.Count == 0)
        {
            body.Append(CrearParrafo("No se detectaron alertas jurídicas.", cursiva: true));
            return;
        }

        foreach (AlertaJuridicaDto alerta in resultadoJson.AlertasJuridicas)
        {
            body.Append(CrearParrafo($"• {alerta.Titulo} - Riesgo: {alerta.NivelRiesgo}", negrita: true));
            body.Append(CrearParrafo($"Descripción: {alerta.Descripcion}"));
            body.Append(CrearParrafo($"Recomendación: {alerta.Recomendacion}", cursiva: true));
            body.Append(CrearParrafo(string.Empty));
        }
    }

    private static void CrearSeccionAnotaciones(Body body, AnalisisResultadoJsonDto resultadoJson)
    {
        body.Append(CrearTituloSeccion("4. Matriz de Anotaciones"));

        var tabla = new Table();
        tabla.Append(new TableProperties(
            new TableBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 })));

        var encabezados = new TableRow();
        string[] columnas = { "# Anotación", "Fecha", "Especificación", "Naturaleza", "Gravamen" };
        foreach (string columna in columnas)
        {
            encabezados.Append(CrearCeldaTabla(columna, negrita: true));
        }
        tabla.Append(encabezados);

        foreach (AnotacionDto anotacion in resultadoJson.Anotaciones)
        {
            var fila = new TableRow();
            fila.Append(
                CrearCeldaTabla(anotacion.NumeroAnotacion),
                CrearCeldaTabla(anotacion.Fecha),
                CrearCeldaTabla(anotacion.Especificacion),
                CrearCeldaTabla(anotacion.NaturalezaJuridica),
                CrearCeldaTabla(anotacion.EsGravamen ? "Sí" : "No"));
            tabla.Append(fila);
        }

        body.Append(tabla);
        body.Append(CrearParrafo(string.Empty));
    }

    private static void CrearSeccionObservaciones(Body body, AnalisisResultadoJsonDto resultadoJson)
    {
        body.Append(CrearTituloSeccion("5. Observaciones y Recomendaciones"));

        if (resultadoJson.Observaciones.Count == 0)
        {
            body.Append(CrearParrafo("Sin observaciones adicionales.", cursiva: true));
            return;
        }

        foreach (string observacion in resultadoJson.Observaciones)
        {
            body.Append(CrearParrafo($"• {observacion}"));
        }
    }

    private static Paragraph CrearTituloSeccion(string texto)
    {
        var parrafo = new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { Before = "240", After = "120" }));
        parrafo.Append(CrearRun(texto, negrita: true, cursiva: false, tamaño: 16));
        return parrafo;
    }

    private static Paragraph CrearParrafo(string texto, bool negrita = false, bool cursiva = false, int? tamaño = null, bool centrado = false, bool justificado = false)
    {
        var parrafo = new Paragraph();
        var propiedades = new ParagraphProperties();

        if (centrado)
        {
            propiedades.Justification = new Justification { Val = JustificationValues.Center };
        }
        else if (justificado)
        {
            propiedades.Justification = new Justification { Val = JustificationValues.Both };
        }

        if (tamaño is not null)
        {
            propiedades.SpacingBetweenLines = new SpacingBetweenLines { After = "120" };
            parrafo.Append(propiedades);
        }

        parrafo.Append(CrearRun(texto, negrita, cursiva, tamaño));
        return parrafo;
    }

    private static Run CrearRun(string texto, bool negrita, bool cursiva, int? tamaño = null)
    {
        var run = new Run();
        var runProperties = new RunProperties();

        if (negrita)
        {
            runProperties.Append(new Bold());
        }
        if (cursiva)
        {
            runProperties.Append(new Italic());
        }
        if (tamaño is not null)
        {
            runProperties.Append(new FontSize { Val = (tamaño * 2).ToString() });
        }

        run.Append(runProperties);
        run.Append(new Text(texto));
        return run;
    }

    private static TableCell CrearCeldaTabla(string texto, bool negrita = false)
    {
        var celda = new TableCell();
        celda.Append(new TableCellProperties(
            new TableCellWidth { Width = "0", Type = TableWidthUnitValues.Auto }));
        celda.Append(CrearParrafo(texto, negrita: negrita));
        return celda;
    }
}