using Aukaria.Application.DTOs.JsonSchema;
using Aukaria.Application.Interfaces;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Aukaria.Infrastructure.Services;

/// <summary>
/// Genera el documento oficial "DIAGNÓSTICO JURÍDICO CATASTRAL" de Aukaria.
/// Utiliza la paleta institucional FROST MONO (carbón/grafito + semáforos de viabilidad),
/// tipografía Arial y una estructura modular de 10 secciones de ficha técnica.
/// </summary>
public sealed class DiagnosticoWordGeneratorService : IDiagnosticoWordGeneratorService
{
    // --- Tipografía y paleta institucional AUKARIA (verde esmeralda / grafito) ---
    private const string FuenteArial = "Arial";

    private const string VerdeBosque = "0F3D2E"; // Encabezados de tablas y título principal
    private const string VerdeBosqueAlt = "14532D";
    private const string Blanco = "FFFFFF";      // Texto sobre encabezados
    private const string BordeTabla = "CBD5E1";  // Bordes de tablas (verde grisáceo sutil)
    private const string MentaFondo = "F0FDF4";  // Zebra striping (filas alternadas)
    private const string Carbono = "1F2937";     // Texto de cuerpo
    private const string GrisMuted = "6B7280";   // Subtítulo / identificador

    // --- Semáforos de viabilidad (dictamen categórico) ---
    private const string VerdeEsmeralda = "059669";
    private const string VerdeFondo = "ECFDF5";
    private const string VerdeBorde = "10B981";

    private const string Ambar = "D97706";
    private const string AmbarFondo = "FFFBEB";
    private const string AmbarBorde = "F59E0B";

    private const string RojoCarmesi = "DC2626";
    private const string RojoFondo = "FEF2F2";
    private const string RojoBorde = "EF4444";

    // --- Textos institucionales obligatorios ---
    private const string ObservacionAmbientalRuap =
        "Verificación en RUNAP: Se recomienda verificar la ubicación geográfica del predio en el portal oficial del " +
        "Registro Único Nacional de Áreas Protegidas (RUNAP) para constatar la inexistencia de afectaciones en zonas " +
        "de reserva o áreas protegidas.";

    private const string ClausulaExclusionResponsabilidad =
        "Aviso Legal: El presente diagnóstico se limita a la revisión documental y registral aportada. Aukaria no " +
        "realiza consultas en listas restrictivas (OFAC) ni antecedentes judiciales.";

    public Task<byte[]> GenerarDiagnosticoWordAsync(
        AnalisisResultadoJsonDto resultadoJson,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => GenerarDiagnostico(resultadoJson, cancellationToken), cancellationToken);
    }

    public Task<byte[]> GenerarAnexoTractoWordAsync(
        AnalisisResultadoJsonDto resultadoJson,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => GenerarAnexoTracto(resultadoJson, cancellationToken), cancellationToken);
    }

    private static byte[] GenerarAnexoTracto(AnalisisResultadoJsonDto r, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        using (WordprocessingDocument documento = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = documento.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = mainPart.Document.AppendChild(new Body());

            AplicarPaginaA4(body);
            CrearBannerAnexo(body, r);
            CrearAnexoTitularidad(body, r);
            CrearAnexoTradicion(body, r);
            CrearAnexoCertificacionTracto(body, r);

            mainPart.Document.Save();
        }

        ct.ThrowIfCancellationRequested();
        return ms.ToArray();
    }

    private static void CrearBannerAnexo(Body body, AnalisisResultadoJsonDto r)
    {
        string predio = Valor(r.NombrePredio, "PREDIO SIN IDENTIFICAR").ToUpperInvariant();
        string fmi = Valor(r.MatriculaFMI, "SIN FMI").ToUpperInvariant();

        body.Append(CrearTituloMayuscula("ANEXO DE TRADICIÓN Y TRACTO SUCESIVO", 14));
        body.Append(CrearParrafoCentral("AUKARIA - AUDITORÍA PREDIAL Y ESTUDIO DE TÍTULOS", 9, VerdeBosque, negrita: true));
        body.Append(CrearParrafoCentral($"{predio}  |  FMI {fmi}", 10, VerdeBosque, negrita: true));
        body.Append(CrearParrafoCentral("Anexo del Diagnóstico Jurídico Catastral · Instrumento de soporte", 9, GrisMuted, negrita: false));
        body.Append(CrearSeparadorLinea());
    }

    private static void CrearAnexoTitularidad(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloSeccion(body, "A. TITULARIDAD REGISTRADA");

        var filas = new List<string[]>();
        foreach (TitularDto titular in r.Titulares)
        {
            filas.Add(new[]
            {
                CapitalizarPalabras(Valor(titular.Nombre)),
                Valor(titular.Identificacion),
                Valor(titular.CondicionDominio),
                Valor(titular.ParticipacionCuota)
            });
        }
        if (filas.Count == 0)
        {
            filas.Add(new[] { CapitalizarPalabras(Valor(r.PropietarioActual)), "No reportado", "No reportado", "No reportado" });
        }

        body.Append(CrearTablaBase(new[] { "NOMBRE", "IDENTIFICACIÓN", "CONDICIÓN DEL DOMINIO", "% PROPIEDAD" }, filas));
        body.Append(CrearParrafoSeparador());
    }

    private static void CrearAnexoTradicion(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloSeccion(body, "B. RÉGIMEN HISTÓRICO DE LA TRADICIÓN");

        var filas = new List<string[]>();
        foreach (TradicionActoDto acto in r.TradicionActos)
        {
            string num = string.IsNullOrWhiteSpace(acto.NumeroAnotacion) ? "—" : acto.NumeroAnotacion;
            string fecha = NoVacio(acto.Fecha) ? acto.Fecha : Valor(acto.Anio);
            string actoDesc = NoVacio(acto.CodigoSnr)
                ? $"{Valor(acto.ActoJuridico)} (Código {acto.CodigoSnr})"
                : Valor(acto.ActoJuridico);

            filas.Add(new[]
            {
                num,
                fecha,
                actoDesc,
                Valor(acto.CadenaDeDominio),
                NoVacio(acto.AnalisisImpacto) ? acto.AnalisisImpacto : Valor(acto.AnalisisJuridico)
            });
        }

        if (filas.Count == 0)
        {
            foreach (AnotacionDto anotacion in r.Anotaciones)
            {
                filas.Add(new[]
                {
                    Valor(anotacion.NumeroAnotacion, "—"),
                    Valor(anotacion.Fecha),
                    Valor(anotacion.Especificacion),
                    "—",
                    Valor(anotacion.NaturalezaJuridica)
                });
            }
        }

        if (filas.Count == 0)
        {
            CrearCuerpoJustificado(body, "Sin información de tracto sucesivo disponible en el folio analizado.");
            return;
        }

        body.Append(CrearTablaBase(new[] { "ANOTACIÓN N°", "FECHA", "ACTO JURÍDICO (CÓDIGO SNR)", "CADENA DE DOMINIO", "ANÁLISIS DE IMPACTO" }, filas));
        body.Append(CrearParrafoSeparador());
    }

    private static void CrearAnexoCertificacionTracto(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloSeccion(body, "C. CERTIFICACIÓN DEL TRACTO");

        string certificacion = NoVacio(r.CertificacionTracto)
            ? r.CertificacionTracto
            : "El tracto sucesivo se reconstruyó a partir de las anotaciones del folio de matrícula inmobiliaria.";

        CrearCuerpoJustificado(body, certificacion);
        body.Append(CrearParrafoSeparador());
    }

    private static byte[] GenerarDiagnostico(AnalisisResultadoJsonDto r, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        using (WordprocessingDocument documento = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = documento.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = mainPart.Document.AppendChild(new Body());

            AplicarPaginaA4(body);
            CrearBannerBranding(body, r);
            CrearSeccionIdentificacionPredio(body, r);
            CrearSeccionLocalizacion(body, r);
            CrearSeccionInformacionComplementaria(body, r);
            CrearSeccionTitularidad(body, r);
            CrearSeccionModoAdquisicion(body, r);
            CrearSeccionConceptoCatastral(body, r);
            CrearSeccionConceptoJuridico(body, r);
            CrearSeccionObservaciones(body, r);
            CrearSeccionDocumentosConsultados(body, r);
            CrearCuadroControlFirmas(body);

            mainPart.Document.Save();
        }

        ct.ThrowIfCancellationRequested();
        return ms.ToArray();
    }

    // =====================================================================
    // 0. Configuración de página y encabezado / branding
    // =====================================================================
    private static void AplicarPaginaA4(Body body)
    {
        var sectPr = new SectionProperties();
        sectPr.Append(
            new PageSize { Width = 11906, Height = 16838 }, // A4
            new PageMargin { Top = 1134, Right = 1134, Bottom = 1134, Left = 1134, Header = 708, Footer = 708 });
        body.AppendChild(sectPr);
    }

    private static void CrearBannerBranding(Body body, AnalisisResultadoJsonDto r)
    {
        string predio = Valor(r.NombrePredio, "PREDIO SIN IDENTIFICAR").ToUpperInvariant();
        string fmi = Valor(r.MatriculaFMI, "SIN FMI").ToUpperInvariant();

        body.Append(CrearTituloMayuscula("DIAGNÓSTICO JURÍDICO CATASTRAL", 14));
        body.Append(CrearParrafoCentral("AUKARIA - AUDITORÍA PREDIAL Y ESTUDIO DE TÍTULOS", 9, VerdeBosque, negrita: true));
        body.Append(CrearParrafoCentral($"{predio}  |  FMI {fmi}", 10, VerdeBosque, negrita: false));
        body.Append(CrearSeparadorLinea());
    }

    // =====================================================================
    // 1. IDENTIFICACIÓN DEL PREDIO
    // =====================================================================
    private static void CrearSeccionIdentificacionPredio(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloSeccion(body, "1. IDENTIFICACIÓN DEL PREDIO");

        var tabla = new Table();
        tabla.Append(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableJustification { Val = TableRowAlignmentValues.Center },
            CrearBordesTablas()));

        var grid = new TableGrid();
        grid.Append(new GridColumn { Width = "2200" }); // etiqueta
        grid.Append(new GridColumn { Width = "3000" }); // valor 1
        grid.Append(new GridColumn { Width = "2200" }); // etiqueta 2
        grid.Append(new GridColumn { Width = "3000" }); // valor 2
        tabla.Append(grid);

        // Fila 1: [ID Predio / Aukaria] | [No. Folio Matrícula]
        var fila1 = new TableRow();
        fila1.Append(CrearCeldaDato("ID Predio / Aukaria", negrita: true, fondoHex: MentaFondo));
        fila1.Append(CrearCeldaDato(Valor(r.NombrePredio, "No reportado")));
        fila1.Append(CrearCeldaDato("No. Folio Matrícula", negrita: true, fondoHex: MentaFondo));
        fila1.Append(CrearCeldaDato(Valor(r.MatriculaFMI, "No reportado")));
        tabla.Append(fila1);

        // Fila 2: [Cédula Catastral] | [FMI Matriz]
        var fila2 = new TableRow();
        fila2.Append(CrearCeldaDato("Cédula Catastral", negrita: true, fondoHex: MentaFondo));
        fila2.Append(CrearCeldaDato(Valor(r.CedulaCatastral, "No reportado")));
        fila2.Append(CrearCeldaDato("FMI Matriz", negrita: true, fondoHex: MentaFondo));
        fila2.Append(CrearCeldaDato(Valor(r.FolioMatriz, "No reportado")));
        tabla.Append(fila2);

        // Fila 3: [FMI Segregada] (GridSpan 3)
        var fila3 = new TableRow();
        fila3.Append(CrearCeldaDato("FMI Segregada", negrita: true, fondoHex: MentaFondo));
        fila3.Append(CrearCeldaDato(Valor(r.FoliosDerivados, "No reportado"), gridSpan: 3));
        tabla.Append(fila3);

        // Fila 4: [Nombre Predio (FMI)] | [Nombre Predio (Campo)]
        var fila4 = new TableRow();
        fila4.Append(CrearCeldaDato("Nombre Predio (FMI)", negrita: true, fondoHex: MentaFondo));
        fila4.Append(CrearCeldaDato(Valor(r.NombrePredio, "No reportado")));
        fila4.Append(CrearCeldaDato("Nombre Predio (Campo)", negrita: true, fondoHex: MentaFondo));
        fila4.Append(CrearCeldaDato("No reportado"));
        tabla.Append(fila4);

        body.Append(tabla);
        body.Append(CrearParrafoSeparador());
    }

    // =====================================================================
    // 2. LOCALIZACIÓN DEL INMUEBLE
    // =====================================================================
    private static void CrearSeccionLocalizacion(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloSeccion(body, "2. LOCALIZACIÓN DEL INMUEBLE");

        var filas = new List<string[]>
        {
            new[] { "Vereda", Valor(r.Vereda), "No reportado" },
            new[] { "Municipio", Valor(r.Municipio), "No reportado" },
            new[] { "Departamento", Valor(r.Departamento), "No reportado" }
        };

        body.Append(CrearTablaBase(new[] { "JURISDICCIÓN / NIVEL", "SEGÚN FMI", "INSPECCIÓN EN CAMPO" }, filas));
        body.Append(CrearParrafoSeparador());
    }

    // =====================================================================
    // 3. INFORMACIÓN COMPLEMENTARIA DEL PREDIO
    // =====================================================================
    private static void CrearSeccionInformacionComplementaria(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloSeccion(body, "3. INFORMACIÓN COMPLEMENTARIA DEL PREDIO");

        var filas = new List<string[]>
        {
            new[] { "Área según Registro (FMI)", Valor(r.AreaSegunFmi) },
            new[] { "Área según Inspección de Campo", "No reportado" }
        };
        body.Append(CrearTablaBase(new[] { "ÁREA", "VALOR" }, filas));
        body.Append(CrearParrafoSeparador());

        CrearSubtitulo(body, "LINDEROS DEL PREDIO");
        string linderos = NoVacio(r.LinderosDescripcion) ? r.LinderosDescripcion : Valor(r.Linderos);
        CrearCuerpoJustificado(body, $"Según FMI: {linderos}");
        if (NoVacio(r.SoporteDocumentalLinderos))
        {
            CrearCuerpoJustificado(body, $"Acto de soporte: {r.SoporteDocumentalLinderos}");
        }
        body.Append(CrearParrafoSeparador());
    }

    // =====================================================================
    // 4. TITULARIDAD DEL PREDIO
    // =====================================================================
    private static void CrearSeccionTitularidad(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloSeccion(body, "4. TITULARIDAD DEL PREDIO");

        var filas = new List<string[]>();
        foreach (TitularDto titular in r.Titulares)
        {
            filas.Add(new[]
            {
                CapitalizarPalabras(Valor(titular.Nombre)),
                "Cédula de ciudadanía",
                Valor(titular.Identificacion),
                Valor(titular.CondicionDominio),
                Valor(titular.ParticipacionCuota)
            });
        }

        if (filas.Count == 0)
        {
            filas.Add(new[]
            {
                CapitalizarPalabras(Valor(r.PropietarioActual)),
                "No reportado",
                "No reportado",
                "No reportado",
                "No reportado"
            });
        }

        body.Append(CrearTablaBase(new[] { "NOMBRE PROPIETARIO LEGAL", "TIPO DOC.", "IDENTIFICACIÓN", "POSICIÓN FRENTE AL DOMINIO", "% PROPIEDAD" }, filas));
        body.Append(CrearParrafoSeparador());
    }

    // =====================================================================
    // 5. MODO DE ADQUISICIÓN
    // =====================================================================
    private static void CrearSeccionModoAdquisicion(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloSeccion(body, "5. MODO DE ADQUISICIÓN");

        string texto = ModoAdquisicionTexto(r);
        CrearCuerpoJustificado(body, texto);
        body.Append(CrearParrafoSeparador());
    }

    private static string ModoAdquisicionTexto(AnalisisResultadoJsonDto r)
    {
        string titular = Valor(r.PropietarioActual, "El titular registral");
        string soporte = $"{Valor(r.NumeroEscritura, "Información no reportada")} de fecha {Valor(r.FechaEscritura)}";
        if (NoVacio(r.Notaria) || NoVacio(r.CiudadNotaria))
        {
            soporte += $" otorgada en la {Valor(r.Notaria)} de {Valor(r.CiudadNotaria)}";
        }

        return $"El derecho de dominio sobre el predio se adquirió por {titular} mediante {soporte}. " +
               $"La fuente traslaticia corresponde a {Valor(r.OrigenCabidaActual, "la información registral consignada en el folio de matrícula inmobiliaria")}.";
    }

    // =====================================================================
    // 6. CONCEPTO CATASTRAL
    // =====================================================================
    private static void CrearSeccionConceptoCatastral(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloSeccion(body, "6. CONCEPTO CATASTRAL");

        string certificado = NoVacio(r.CedulaCatastral)
            ? $"Se constata la cédula catastral {r.CedulaCatastral}."
            : "No se reporta cédula catastral en el folio analizado.";
        string nupre = NoVacio(r.Nupre)
            ? $"El Número Predial Nacional (NUPRE) reportado es {r.Nupre}."
            : "No se reporta NUPRE en el folio analizado.";

        CrearCuerpoJustificado(body, string.Join(" ", certificado, nupre));

        string cabida = NoVacio(r.ConclusionPredial) ? r.ConclusionPredial : $"Área registrada: {Valor(r.AreaRegistrada)}.";
        CrearCuerpoJustificado(body, $"Consistencia de cabidas: {cabida}");
        body.Append(CrearParrafoSeparador());
    }

    // =====================================================================
    // 7. CONCEPTO JURÍDICO DEL PREDIO
    // =====================================================================
    private static void CrearSeccionConceptoJuridico(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloSeccion(body, "7. CONCEPTO JURÍDICO DEL PREDIO");

        string dictamen = NoVacio(r.DiagnosticoEjecutivo)
            ? r.DiagnosticoEjecutivo
            : Valor(r.ResumenEjecutivo, "Sin dictamen ejecutivo disponible.");

        CrearCuerpoJustificado(body, dictamen);
        CrearBadgeViabilidad(body, r.Viabilidad);

        foreach (AlertaJuridicaDto alerta in r.AlertasJuridicas)
        {
            if (NoVacio(alerta.Descripcion))
            {
                CrearCuerpoJustificado(body, $"• {Valor(alerta.Titulo)}: {alerta.Descripcion}", negrita: false);
            }
        }
        body.Append(CrearParrafoSeparador());
    }

    private static void CrearBadgeViabilidad(Body body, string viabilidad)
    {
        var (etiqueta, color, fondo, borde) = ResolverViabilidad(viabilidad);

        var tabla = new Table();
        tabla.Append(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableJustification { Val = TableRowAlignmentValues.Center },
            new TableBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6, Color = borde },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6, Color = borde },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6, Color = borde },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6, Color = borde })));

        var grid = new TableGrid();
        grid.Append(new GridColumn { Width = "5000" });
        tabla.Append(grid);

        var fila = new TableRow();
        var celda = new TableCell();
        var propiedades = new TableCellProperties();
        propiedades.Append(new Shading { Val = ShadingPatternValues.Clear, Fill = fondo });
        propiedades.Append(CrearMargenCelda());
        celda.Append(propiedades);

        var parrafo = new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "60", After = "60" },
                new Justification { Val = JustificationValues.Center }));
        parrafo.Append(CrearRun("DICTAMEN CATASTRAL: ", 11, Carbono, negrita: true));
        parrafo.Append(CrearRun(etiqueta, 11, color, negrita: true));
        celda.Append(parrafo);
        fila.Append(celda);
        tabla.Append(fila);

        body.Append(tabla);
        body.Append(CrearParrafoSeparador());
    }

    private static (string Etiqueta, string Color, string Fondo, string Borde) ResolverViabilidad(string viabilidad)
    {
        string normalizado = (viabilidad ?? string.Empty).ToLowerInvariant();

        if (normalizado.Contains("viable"))
        {
            return ("Viable", VerdeEsmeralda, VerdeFondo, VerdeBorde);
        }

        if (normalizado.Contains("critic") || normalizado.Contains("no viable") || normalizado.Contains("no-viable"))
        {
            return ("No Viable", RojoCarmesi, RojoFondo, RojoBorde);
        }

        return ("Viable Condicionado", Ambar, AmbarFondo, AmbarBorde);
    }

    // =====================================================================
    // 8. OBSERVACIONES Y/O RECOMENDACIONES DE SANEAMIENTO
    // =====================================================================
    private static void CrearSeccionObservaciones(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloSeccion(body, "8. OBSERVACIONES Y/O RECOMENDACIONES DE SANEAMIENTO");

        if (r.Observaciones.Count == 0 && !NoVacio(r.ObservacionAmbiental) && !NoVacio(r.ExclusionResponsabilidad))
        {
            CrearCuerpoJustificado(body, "Sin observaciones adicionales.");
        }

        foreach (string observacion in r.Observaciones)
        {
            if (NoVacio(observacion))
            {
                CrearVineta(body, observacion);
            }
        }

        string obsAmbiental = NoVacio(r.ObservacionAmbiental) ? r.ObservacionAmbiental.Trim() : ObservacionAmbientalRuap;
        CrearVineta(body, obsAmbiental);

        string exclusion = NoVacio(r.ExclusionResponsabilidad) ? r.ExclusionResponsabilidad.Trim() : ClausulaExclusionResponsabilidad;
        CrearVineta(body, exclusion);

        body.Append(CrearParrafoSeparador());
    }

    // =====================================================================
    // 9. DOCUMENTOS CONSULTADOS
    // =====================================================================
    private static void CrearSeccionDocumentosConsultados(Body body, AnalisisResultadoJsonDto r)
    {
        CrearTituloSeccion(body, "9. DOCUMENTOS CONSULTADOS");

        if (r.DocumentosAnalizados.Count == 0)
        {
            string fmi = Valor(r.MatriculaFMI, "SIN FMI");
            CrearVineta(body, $"Certificado de Tradición y Libertad (CTL) del FMI {fmi}, expedido por la ORIP de {Valor(r.ORIP)}.");
            return;
        }

        foreach (string documento in r.DocumentosAnalizados)
        {
            if (NoVacio(documento))
            {
                CrearVineta(body, documento);
            }
        }
        body.Append(CrearParrafoSeparador());
    }

    // =====================================================================
    // 10. CUADRO DE CONTROL Y FIRMAS
    // =====================================================================
    private static void CrearCuadroControlFirmas(Body body)
    {
        CrearTituloSeccion(body, "10. CUADRO DE CONTROL Y FIRMAS");

        var filas = new[]
        {
            new[]
            {
                "Profesional Jurídico\n(Elaboró)\n\n\n______________________\nNombre / Firma",
                "Profesional SIG / Campo\n(Elaboró)\n\n\n______________________\nNombre / Firma",
                "Coordinación de Gestión Inmobiliaria\n(Aprobó)\n\n\n______________________\nNombre / Firma"
            }
        };

        body.Append(CrearTablaFirmas(filas));
        body.Append(CrearParrafoSeparador());
    }

    // =====================================================================
    // HELPER: Composición de elementos base
    // =====================================================================

    /// <summary>Crea una tabla base con bordes grises, encabezado carbón y zebra en filas alternas.</summary>
    private static Table CrearTablaBase(string[] encabezados, IReadOnlyList<string[]> filas)
    {
        var tabla = new Table();
        tabla.Append(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableJustification { Val = TableRowAlignmentValues.Center },
            CrearBordesTablas()));

        var grid = new TableGrid();
        for (int columna = 0; columna < encabezados.Length; columna++)
        {
            int longitudMaxima = encabezados[columna].Length;
            foreach (string[] fila in filas)
            {
                longitudMaxima = Math.Max(longitudMaxima, fila[columna].Length);
            }
            grid.Append(new GridColumn { Width = Math.Clamp(longitudMaxima * 100, 1400, 3600).ToString() });
        }
        tabla.Append(grid);

        var filaEncabezado = new TableRow(new TableRowProperties(new TableHeader()));
        for (int columna = 0; columna < encabezados.Length; columna++)
        {
            filaEncabezado.Append(CrearEncabezadoVerde(encabezados[columna]));
        }
        tabla.Append(filaEncabezado);

        for (int i = 0; i < filas.Count; i++)
        {
            var fila = new TableRow();
            bool zebra = i % 2 == 1;
            foreach (string valor in filas[i])
            {
                fila.Append(CrearCeldaDato(valor, zebra: zebra));
            }
            tabla.Append(fila);
        }

        return tabla;
    }

    private static Table CrearTablaFirmas(IReadOnlyList<string[]> filas)
    {
        string[] encabezados = { "ELABORADO POR", "ELABORADO POR", "APROBADO POR" };

        var tabla = new Table();
        tabla.Append(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableJustification { Val = TableRowAlignmentValues.Center },
            CrearBordesTablas()));

        var grid = new TableGrid();
        for (int columna = 0; columna < 3; columna++)
        {
            grid.Append(new GridColumn { Width = "2200" });
        }
        tabla.Append(grid);

        var filaEncabezado = new TableRow(new TableRowProperties(new TableHeader()));
        foreach (string encabezado in encabezados)
        {
            filaEncabezado.Append(CrearEncabezadoVerde(encabezado));
        }
        tabla.Append(filaEncabezado);

        foreach (string[] fila in filas)
        {
            var filaDatos = new TableRow();
            foreach (string valor in fila)
            {
                filaDatos.Append(CrearCeldaMultilinea(valor));
            }
            tabla.Append(filaDatos);
        }

        return tabla;
    }

    /// <summary>Crea la colección de bordes verdes grisáceos (single, sz 4) para tablas.</summary>
    private static TableBorders CrearBordesTablas()
    {
        return new TableBorders(
            new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4, Color = BordeTabla },
            new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4, Color = BordeTabla },
            new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4, Color = BordeTabla },
            new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4, Color = BordeTabla },
            new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4, Color = BordeTabla },
            new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4, Color = BordeTabla });
    }

    /// <summary>Crea una celda de encabezado con fondo verde bosque y texto blanco en negrita.</summary>
    private static TableCell CrearEncabezadoVerde(string texto)
    {
        var celda = new TableCell();
        var propiedades = new TableCellProperties();
        propiedades.Append(new Shading { Val = ShadingPatternValues.Clear, Fill = VerdeBosque });
        propiedades.Append(CrearMargenCelda());

        celda.Append(propiedades);
        celda.Append(CrearParrafoCelda(texto, 10, Blanco, negrita: true));
        return celda;
    }

    /// <summary>
    /// Crea una celda de dato. Aplica grilla `gridSpan` y fondo `fondoHex` cuando se indican;
    /// de lo contrario usa el zebra striping menta.
    /// </summary>
    private static TableCell CrearCeldaDato(
        string texto,
        bool negrita = false,
        int gridSpan = 1,
        string? fondoHex = null,
        bool zebra = false)
    {
        var celda = new TableCell();
        var propiedades = new TableCellProperties();

        string? relleno = fondoHex ?? (zebra ? MentaFondo : null);
        if (relleno != null)
        {
            propiedades.Append(new Shading { Val = ShadingPatternValues.Clear, Fill = relleno });
        }
        if (gridSpan > 1)
        {
            propiedades.Append(new GridSpan { Val = gridSpan });
        }
        propiedades.Append(CrearMargenCelda());

        celda.Append(propiedades);
        celda.Append(CrearParrafoCelda(texto, 10, Carbono, negrita));
        return celda;
    }

    private static TableCell CrearCeldaMultilinea(string texto)
    {
        var celda = new TableCell();
        var propiedades = new TableCellProperties();
        propiedades.Append(CrearMargenCelda());
        celda.Append(propiedades);

        foreach (string linea in texto.Split('\n'))
        {
            celda.Append(CrearParrafoCentrado(linea, 10, Carbono, negrita: true));
        }
        return celda;
    }

    private static TableCellMargin CrearMargenCelda()
    {
        return new TableCellMargin(
            new TopMargin { Width = "100", Type = TableWidthUnitValues.Dxa },
            new LeftMargin { Width = "150", Type = TableWidthUnitValues.Dxa },
            new BottomMargin { Width = "100", Type = TableWidthUnitValues.Dxa },
            new RightMargin { Width = "150", Type = TableWidthUnitValues.Dxa });
    }

    // =====================================================================
    // HELPER: Párrafos
    // =====================================================================
    private static Paragraph CrearTituloMayuscula(string texto, int tamanoPt)
    {
        var parrafo = new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "80" },
                new Justification { Val = JustificationValues.Center }));
        parrafo.Append(CrearRun(texto, tamanoPt, VerdeBosque, negrita: true));
        return parrafo;
    }

    private static Paragraph CrearParrafoCentral(string texto, int tamanoPt, string colorHex, bool negrita = false)
    {
        var parrafo = new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "120" },
                new Justification { Val = JustificationValues.Center }));
        parrafo.Append(CrearRun(texto, tamanoPt, colorHex, negrita));
        return parrafo;
    }

    private static Paragraph CrearParrafoCentrado(string texto, int tamanoPt, string colorHex, bool negrita = false)
    {
        var parrafo = new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "0" },
                new Justification { Val = JustificationValues.Center }));
        parrafo.Append(CrearRun(texto, tamanoPt, colorHex, negrita));
        return parrafo;
    }

    private static void CrearTituloSeccion(Body body, string titulo)
    {
        var parrafo = new Paragraph(
            new ParagraphProperties(
                new KeepNext(),
                new SpacingBetweenLines { Before = "360", After = "140" }));
        parrafo.Append(CrearRun(titulo, 12, VerdeBosque, negrita: true));
        body.Append(parrafo);
    }

    private static void CrearSubtitulo(Body body, string texto)
    {
        var parrafo = new Paragraph(
            new ParagraphProperties(
                new KeepNext(),
                new SpacingBetweenLines { Before = "160", After = "120" }));
        parrafo.Append(CrearRun(texto, 11, VerdeBosque, negrita: true));
        body.Append(parrafo);
    }

    private static void CrearCuerpoJustificado(Body body, string texto, bool negrita = false)
    {
        var parrafo = new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "80", Line = "276", LineRule = LineSpacingRuleValues.Auto },
                new Justification { Val = JustificationValues.Both }));
        parrafo.Append(CrearRun(texto, 10, Carbono, negrita));
        body.Append(parrafo);
    }

    private static void CrearVineta(Body body, string texto)
    {
        var parrafo = new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "80", Line = "276", LineRule = LineSpacingRuleValues.Auto },
                new Indentation { Left = "360" },
                new Justification { Val = JustificationValues.Both }));
        parrafo.Append(CrearRun("• ", 10, Carbono, negrita: true));
        parrafo.Append(CrearRun(NormalizarSaltoLinea(texto), 10, Carbono, negrita: false));
        body.Append(parrafo);
    }

    private static Paragraph CrearParrafoSeparador()
    {
        return new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "160" }));
    }

    private static Paragraph CrearSeparadorLinea()
    {
        var parrafo = new Paragraph();
        parrafo.Append(new ParagraphProperties(
            new SpacingBetweenLines { After = "200" },
            new ParagraphBorders(
                new BottomBorder
                {
                    Val = new EnumValue<BorderValues>(BorderValues.Single),
                    Size = 6,
                    Color = BordeTabla
                })));
        parrafo.Append(new Run());
        return parrafo;
    }

    private static Paragraph CrearParrafoCelda(string texto, int tamanoPt, string colorHex, bool negrita)
    {
        var parrafo = new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { Before = "0", After = "0" }));
        parrafo.Append(CrearRun(texto, tamanoPt, colorHex, negrita));
        return parrafo;
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

        return new Run(propiedades, new Text(NormalizarSaltoLinea(texto)) { Space = SpaceProcessingModeValues.Preserve });
    }

    // =====================================================================
    // HELPER: Utilidades de texto
    // =====================================================================
    private static bool NoVacio(string valor) => !string.IsNullOrWhiteSpace(valor);

    private static string Valor(string valor, string fallback = "No reportado")
    {
        return NoVacio(valor) ? valor.Trim() : fallback;
    }

    private static string NormalizarSaltoLinea(string texto)
    {
        return (texto ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
    }

    private static string CapitalizarPalabras(string texto)
    {
        if (!NoVacio(texto))
        {
            return texto;
        }

        var palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < palabras.Length; i++)
        {
            string palabra = palabras[i];
            if (palabra.Length == 0)
            {
                continue;
            }
            palabras[i] = char.ToUpperInvariant(palabra[0]) + (palabra.Length > 1 ? palabra.Substring(1).ToLowerInvariant() : string.Empty);
        }
        return string.Join(' ', palabras);
    }
}
