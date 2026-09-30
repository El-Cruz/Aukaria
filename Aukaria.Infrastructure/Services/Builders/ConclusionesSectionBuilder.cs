using Aukaria.Domain.Enums;
using Aukaria.Domain.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Aukaria.Infrastructure.Services.Builders;

public static class ConclusionesSectionBuilder
{
    public static void Build(Body body, DiagnosticoPredialModel model)
    {
        CrearTituloSeccion(body, "D. DICTAMEN JURÍDICO Y VIABILIDAD");

        CrearViabilidadBadge(body, model.Viabilidad);

        if (!string.IsNullOrWhiteSpace(model.ConclusionPredial))
        {
            CrearCuerpoJustificado(body, model.ConclusionPredial);
            body.Append(CrearParrafoSeparador());
        }

        if (model.AlertasJuridicas.Count > 0)
        {
            CrearTituloSeccion(body, "ALERTAS JURÍDICAS");
            foreach (var alerta in model.AlertasJuridicas)
            {
                var color = ObtenerColorAlerta(alerta.NivelRiesgo);
                CrearAlertaParrafo(body, alerta, color);
            }
            body.Append(CrearParrafoSeparador());
        }

        if (!string.IsNullOrWhiteSpace(model.DiagnosticoEjecutivo))
        {
            CrearTituloSeccion(body, "DIAGNÓSTICO EJECUTIVO");
            CrearCuerpoJustificado(body, model.DiagnosticoEjecutivo);
            body.Append(CrearParrafoSeparador());
        }
    }

    private static void CrearViabilidadBadge(Body body, EstadoViabilidad viabilidad)
    {
        var (texto, colorFondo, colorBorde, colorTexto) = viabilidad switch
        {
            EstadoViabilidad.Viable => ("VIABLE", "ECFDF5", "10B981", "059669"),
            EstadoViabilidad.AlertaCritica => ("NO VIABLE", "FEF2F2", "EF4444", "DC2626"),
            _ => ("REQUIERE REVISIÓN", "F3F4F6", "9CA3AF", "6B7280")
        };

        var para = new Paragraph();
        var run = new Run();
        run.Append(new Text($"  {texto}  ") { Space = SpaceProcessingModeValues.Preserve });
        run.RunProperties = new RunProperties
        {
            Bold = new Bold(),
            Color = new Color { Val = colorTexto },
            FontSize = new FontSize { Val = "24" },
            Shading = new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = colorFondo }
        };
        para.Append(run);
        para.ParagraphProperties = new ParagraphProperties
        {
            Justification = new Justification { Val = JustificationValues.Center },
            SpacingBetweenLines = new SpacingBetweenLines { After = "200" }
        };
        body.Append(para);
    }

    private static void CrearAlertaParrafo(Body body, AlertaJuridicaModel alerta, string color)
    {
        var para = new Paragraph();
        var run = new Run();
        run.Append(new Text($"• [{alerta.NivelRiesgo}] {alerta.Descripcion}") { Space = SpaceProcessingModeValues.Preserve });
        run.RunProperties = new RunProperties
        {
            Color = new Color { Val = color },
            FontSize = new FontSize { Val = "22" }
        };
        para.Append(run);

        if (!string.IsNullOrWhiteSpace(alerta.Recomendacion))
        {
            var runRec = new Run();
            runRec.Append(new Text($" → {alerta.Recomendacion}") { Space = SpaceProcessingModeValues.Preserve });
            runRec.RunProperties = new RunProperties
            {
                Color = new Color { Val = "6B7280" },
                FontSize = new FontSize { Val = "20" },
                Italic = new Italic()
            };
            para.Append(runRec);
        }

        body.Append(para);
    }

    private static string ObtenerColorAlerta(string nivelRiesgo)
    {
        if (string.IsNullOrWhiteSpace(nivelRiesgo)) return "6B7280";
        var normalized = nivelRiesgo.ToLowerInvariant();
        return normalized switch
        {
            "alto" or "critico" or "crítico" => "DC2626",
            "medio" or "moderado" => "D97706",
            "bajo" => "059669",
            _ => "6B7280"
        };
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

    private static Paragraph CrearParrafoSeparador()
    {
        var para = new Paragraph();
        var run = new Run();
        run.Append(new Text(" ") { Space = SpaceProcessingModeValues.Preserve });
        para.Append(run);
        return para;
    }
}
