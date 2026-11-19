using Aukaria.Application.Interfaces;
using Aukaria.Domain.Enums;
using Aukaria.Domain.Models;
using Aukaria.Infrastructure.Services.Builders;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Aukaria.Infrastructure.Services;

/// <summary>
/// Genera el documento oficial "DIAGNÓSTICO JURÍDICO CATASTRAL" de Aukaria.
/// Utiliza la paleta institucional FROST MONO (carbón/grafito + semáforos de viabilidad),
/// tipografía Arial y una estructura modular de 10 secciones de ficha técnica.
/// Depende exclusivamente de DiagnosticoPredialModel (modelo de dominio), nunca del DTO crudo de Claude.
/// </summary>
public sealed class DiagnosticoWordGeneratorService : IDiagnosticoWordGeneratorService
{
    private const string FuenteArial = "Arial";

    private const string VerdeBosque = "0F3D2E";
    private const string VerdeBosqueAlt = "14532D";
    private const string Blanco = "FFFFFF";
    private const string BordeTabla = "CBD5E1";
    private const string MentaFondo = "F0FDF4";
    private const string Carbono = "1F2937";
    private const string GrisMuted = "6B7280";

    private const string VerdeEsmeralda = "059669";
    private const string VerdeFondo = "ECFDF5";
    private const string VerdeBorde = "10B981";

    private const string Ambar = "D97706";
    private const string AmbarFondo = "FFFBEB";
    private const string AmbarBorde = "F59E0B";

    private const string RojoCarmesi = "DC2626";
    private const string RojoFondo = "FEF2F2";
    private const string RojoBorde = "EF4444";

    private const string ObservacionAmbientalRuap =
        "Verificación en RUNAP: Se recomienda verificar la ubicación geográfica del predio en el portal oficial del " +
        "Registro Único Nacional de Áreas Protegidas (RUNAP) para constatar la inexistencia de afectaciones en zonas " +
        "de reserva o áreas protegidas.";

    private const string ClausulaExclusionResponsabilidad =
        "Aviso Legal: El presente diagnóstico se limita a la revisión documental y aportada. Aukaria no " +
        "realiza consultas en listas restrictivas (OFAC) ni antecedentes judiciales.";

    public Task<byte[]> GenerarDiagnosticoWordAsync(
        DiagnosticoPredialModel model,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => GenerarDiagnostico(model, cancellationToken), cancellationToken);
    }

    public Task<byte[]> GenerarAnexoTractoWordAsync(
        DiagnosticoPredialModel model,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => GenerarAnexoTracto(model, cancellationToken), cancellationToken);
    }

    private static byte[] GenerarAnexoTracto(DiagnosticoPredialModel model, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        using (WordprocessingDocument documento = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = documento.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = mainPart.Document.AppendChild(new Body());

            AplicarPaginaA4(body);
            CrearBannerAnexo(body, model);
            TradicionSectionBuilder.Build(body, model);
            GravamenesSectionBuilder.Build(body, model);
            CrearAnexoCertificacionTracto(body, model);

            mainPart.Document.Save();
        }

        ct.ThrowIfCancellationRequested();
        return ms.ToArray();
    }

    private static bool En(DiagnosticoPredialModel model) =>
        string.Equals(model.Idioma, "en", StringComparison.OrdinalIgnoreCase);

    private static string T(DiagnosticoPredialModel model, string es, string en) => En(model) ? en : es;

    private static void CrearBannerAnexo(Body body, DiagnosticoPredialModel model)
    {
        string predio = Valor(model.NombrePredio, T(model, "PREDIO SIN IDENTIFICAR", "UNIDENTIFIED PROPERTY")).ToUpperInvariant();
        string fmi = Valor(model.MatriculaFMI, T(model, "SIN FMI", "NO FMI")).ToUpperInvariant();

        body.Append(CrearTituloMayuscula(T(model, "ANEXO DE TRADICIÓN Y TRACTO SUCESIVO", "CHAIN OF TITLE & TRADITION ANNEX"), 14));
        body.Append(CrearParrafoCentral(T(model, "AUKARIA - AUDITORÍA PREDIAL Y ESTUDIO DE TÍTULOS", "AUKARIA - PROPERTY AUDIT & TITLE EXAMINATION"), 9, VerdeBosque, negrita: true));
        body.Append(CrearParrafoCentral($"{predio}  |  FMI {fmi}", 10, VerdeBosque, negrita: true));
        body.Append(CrearParrafoCentral(T(model, "Anexo del Diagnóstico Jurídico Catastral · Instrumento de soporte", "Cadastral Legal Diagnosis Annex · Supporting Instrument"), 9, GrisMuted, negrita: false));
        body.Append(CrearSeparadorLinea());
    }

    private static void CrearAnexoCertificacionTracto(Body body, DiagnosticoPredialModel model)
    {
        CrearTituloSeccion(body, T(model, "C. CERTIFICACIÓN DEL TRACTO", "C. CHAIN OF TITLE CERTIFICATION"));

        string certificacion = NoVacio(model.CertificacionTracto)
            ? model.CertificacionTracto
            : T(model,
                "El tracto sucesivo se reconstruyó a partir de las anotaciones del folio de matrícula inmobiliaria.",
                "The chain of title was reconstructed from the annotations in the real estate registration folio.");

        CrearCuerpoJustificado(body, certificacion);
        body.Append(CrearParrafoSeparador());
    }

    private static byte[] GenerarDiagnostico(DiagnosticoPredialModel model, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        using (WordprocessingDocument documento = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = documento.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = mainPart.Document.AppendChild(new Body());

            AplicarPaginaA4(body);
            CrearBannerBranding(body, model);
            CrearSeccionIdentificacionPredio(body, model);
            CrearSeccionLocalizacion(body, model);
            CrearSeccionInformacionComplementaria(body, model);
            CrearSeccionTitularidad(body, model);
            CrearSeccionModoAdquisicion(body, model);
            CrearSeccionConceptoCatastral(body, model);
            CrearSeccionConceptoJuridico(body, model);
            CrearSeccionObservaciones(body, model);
            CrearSeccionDocumentosConsultados(body, model);
            CrearCuadroControlFirmas(body, model);

            mainPart.Document.Save();
        }

        ct.ThrowIfCancellationRequested();
        return ms.ToArray();
    }

    private static void AplicarPaginaA4(Body body)
    {
        var sectPr = new SectionProperties();
        sectPr.Append(
            new PageSize { Width = 11906, Height = 16838 },
            new PageMargin { Top = 1134, Right = 1134, Bottom = 1134, Left = 1134, Header = 708, Footer = 708 });
        body.AppendChild(sectPr);
    }

    private static void CrearBannerBranding(Body body, DiagnosticoPredialModel model)
    {
        string predio = Valor(model.NombrePredio, T(model, "PREDIO SIN IDENTIFICAR", "UNIDENTIFIED PROPERTY")).ToUpperInvariant();
        string fmi = Valor(model.MatriculaFMI, T(model, "SIN FMI", "NO FMI")).ToUpperInvariant();

        body.Append(CrearTituloMayuscula(T(model, "DIAGNÓSTICO JURÍDICO CATASTRAL", "CADASTRAL LEGAL DIAGNOSIS"), 14));
        body.Append(CrearParrafoCentral(T(model, "AUKARIA - AUDITORÍA PREDIAL Y ESTUDIO DE TÍTULOS", "AUKARIA - PROPERTY AUDIT & TITLE EXAMINATION"), 9, VerdeBosque, negrita: true));
        body.Append(CrearParrafoCentral($"{predio}  |  FMI {fmi}", 10, VerdeBosque, negrita: false));
        body.Append(CrearSeparadorLinea());
    }

    private static void CrearSeccionIdentificacionPredio(Body body, DiagnosticoPredialModel model)
    {
        CrearTituloSeccion(body, T(model, "1. IDENTIFICACIÓN DEL PREDIO", "1. PROPERTY IDENTIFICATION"));
        string nr = T(model, "No reportado", "Not reported");

        var tabla = new Table();
        tabla.Append(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableJustification { Val = TableRowAlignmentValues.Center },
            CrearBordesTablas()));

        var grid = new TableGrid();
        grid.Append(new GridColumn { Width = "2200" });
        grid.Append(new GridColumn { Width = "3000" });
        grid.Append(new GridColumn { Width = "2200" });
        grid.Append(new GridColumn { Width = "3000" });
        tabla.Append(grid);

        var fila1 = new TableRow();
        fila1.Append(CrearCeldaDato(T(model, "ID Predio / Aukaria", "Property ID / Aukaria"), negrita: true, fondoHex: MentaFondo));
        fila1.Append(CrearCeldaDato(Valor(model.NombrePredio, nr)));
        fila1.Append(CrearCeldaDato(T(model, "No. Folio Matrícula", "Registration Folio No."), negrita: true, fondoHex: MentaFondo));
        fila1.Append(CrearCeldaDato(Valor(model.MatriculaFMI, nr)));
        tabla.Append(fila1);

        var fila2 = new TableRow();
        fila2.Append(CrearCeldaDato(T(model, "Cédula Catastral", "Cadastral ID"), negrita: true, fondoHex: MentaFondo));
        fila2.Append(CrearCeldaDato(Valor(model.CedulaCatastral, nr)));
        fila2.Append(CrearCeldaDato(T(model, "FMI Matriz", "Parent FMI"), negrita: true, fondoHex: MentaFondo));
        fila2.Append(CrearCeldaDato(Valor(model.FolioMatriz, nr)));
        tabla.Append(fila2);

        var fila3 = new TableRow();
        fila3.Append(CrearCeldaDato(T(model, "FMI Segregada", "Derived FMIs"), negrita: true, fondoHex: MentaFondo));
        fila3.Append(CrearCeldaDato(Valor(model.FoliosDerivados, nr), gridSpan: 3));
        tabla.Append(fila3);

        var fila4 = new TableRow();
        fila4.Append(CrearCeldaDato(T(model, "Nombre Predio (FMI)", "Property Name (FMI)"), negrita: true, fondoHex: MentaFondo));
        fila4.Append(CrearCeldaDato(Valor(model.NombrePredio, nr)));
        fila4.Append(CrearCeldaDato(T(model, "Nombre Predio (Campo)", "Property Name (Field)"), negrita: true, fondoHex: MentaFondo));
        fila4.Append(CrearCeldaDato(nr));
        tabla.Append(fila4);

        body.Append(tabla);
        body.Append(CrearParrafoSeparador());
    }

    private static void CrearSeccionLocalizacion(Body body, DiagnosticoPredialModel model)
    {
        CrearTituloSeccion(body, T(model, "2. LOCALIZACIÓN DEL INMUEBLE", "2. PROPERTY LOCATION"));
        string nr = T(model, "No reportado", "Not reported");

        var filas = new List<string[]>
        {
            new[] { T(model, "Vereda", "Rural District"), Valor(model.Vereda, nr), nr },
            new[] { T(model, "Municipio", "Municipality"), Valor(model.Municipio, nr), nr },
            new[] { T(model, "Departamento", "Department"), Valor(model.Departamento, nr), nr },
            new[] { "ORIP", Valor(model.ORIP, nr), nr }
        };

        body.Append(CrearTablaBase(new[]
        {
            T(model, "JURISDICCIÓN / NIVEL", "JURISDICTION / LEVEL"),
            T(model, "SEGÚN FMI", "PER FMI"),
            T(model, "INSPECCIÓN EN CAMPO", "FIELD INSPECTION")
        }, filas));
        body.Append(CrearParrafoSeparador());
    }

    private static void CrearSeccionInformacionComplementaria(Body body, DiagnosticoPredialModel model)
    {
        CrearTituloSeccion(body, T(model, "3. INFORMACIÓN COMPLEMENTARIA DEL PREDIO", "3. COMPLEMENTARY PROPERTY INFORMATION"));

        var filas = new List<string[]>
        {
            new[] { T(model, "Área según Registro (FMI)", "Area per Registry (FMI)"), Valor(model.AreaSegunFmi) },
            new[] { T(model, "Área según Inspección de Campo", "Area per Field Inspection"), T(model, "No reportado", "Not reported") }
        };
        body.Append(CrearTablaBase(new[] { T(model, "ÁREA", "AREA"), T(model, "VALOR", "VALUE") }, filas));
        body.Append(CrearParrafoSeparador());

        CrearSubtitulo(body, T(model, "LINDEROS DEL PREDIO", "PROPERTY BOUNDARIES"));
        string linderos = NoVacio(model.LinderosDescripcion) ? model.LinderosDescripcion : Valor(model.Linderos);
        CrearCuerpoJustificado(body, $"{T(model, "Según FMI", "Per FMI")}: {linderos}");
        if (NoVacio(model.SoporteDocumentalLinderos))
        {
            CrearCuerpoJustificado(body, $"{T(model, "Acto de soporte", "Supporting instrument")}: {model.SoporteDocumentalLinderos}");
        }
        body.Append(CrearParrafoSeparador());
    }

    private static void CrearSeccionTitularidad(Body body, DiagnosticoPredialModel model)
    {
        CrearTituloSeccion(body, T(model, "4. TITULARIDAD DEL PREDIO", "4. PROPERTY OWNERSHIP"));
        string nr = T(model, "No reportado", "Not reported");

        var filas = new List<string[]>();
        foreach (var titular in model.Titulares)
        {
            filas.Add(new[]
            {
                CapitalizarPalabras(Valor(titular.Nombre)),
                T(model, "Cédula de ciudadanía", "National ID"),
                Valor(titular.NumeroDocumento, nr),
                Valor(titular.Porcentaje, nr)
            });
        }

        if (filas.Count == 0)
        {
            filas.Add(new[]
            {
                CapitalizarPalabras(Valor(model.PropietarioActual)),
                nr,
                nr,
                nr
            });
        }

        body.Append(CrearTablaBase(new[]
        {
            T(model, "NOMBRE PROPIETARIO LEGAL", "LEGAL OWNER NAME"),
            T(model, "TIPO DOC.", "ID TYPE"),
            T(model, "IDENTIFICACIÓN", "ID NUMBER"),
            T(model, "% PROPIEDAD", "% OWNERSHIP")
        }, filas));
        body.Append(CrearParrafoSeparador());
    }

    private static void CrearSeccionModoAdquisicion(Body body, DiagnosticoPredialModel model)
    {
        CrearTituloSeccion(body, T(model, "5. MODO DE ADQUISICIÓN", "5. MODE OF ACQUISITION"));

        string texto = ModoAdquisicionTexto(model);
        CrearCuerpoJustificado(body, texto);
        body.Append(CrearParrafoSeparador());
    }

    private static string ModoAdquisicionTexto(DiagnosticoPredialModel model)
    {
        if (En(model))
        {
            string titularEn = Valor(model.PropietarioActual, "The registered owner");
            string soporteEn = $"{Valor(model.NumeroEscritura, "Not reported")} dated {Valor(model.FechaEscritura, "Not reported")}";
            if (NoVacio(model.Notaria) || NoVacio(model.CiudadNotaria))
            {
                soporteEn += $" executed at {Valor(model.Notaria)} in {Valor(model.CiudadNotaria)}";
            }

            return $"Title to the property was acquired by {titularEn} through {soporteEn}. " +
                   $"The chain-of-title source corresponds to {Valor(model.OrigenCabidaActual, "the registry information recorded in the real estate folio")}.";
        }

        string titular = Valor(model.PropietarioActual, "El titular registral");
        string soporte = $"{Valor(model.NumeroEscritura, "Información no reportada")} de fecha {Valor(model.FechaEscritura)}";
        if (NoVacio(model.Notaria) || NoVacio(model.CiudadNotaria))
        {
            soporte += $" otorgada en la {Valor(model.Notaria)} de {Valor(model.CiudadNotaria)}";
        }

        return $"El derecho de dominio sobre el predio se adquirió por {titular} mediante {soporte}. " +
               $"La fuente traslaticia corresponde a {Valor(model.OrigenCabidaActual, "la información registral consignada en el folio de matrícula inmobiliaria")}.";
    }

    private static void CrearSeccionConceptoCatastral(Body body, DiagnosticoPredialModel model)
    {
        CrearTituloSeccion(body, T(model, "6. CONCEPTO CATASTRAL", "6. CADASTRAL ASSESSMENT"));

        string certificado = NoVacio(model.CedulaCatastral)
            ? T(model, $"Se constata la cédula catastral {model.CedulaCatastral}.", $"Cadastral ID verified: {model.CedulaCatastral}.")
            : T(model, "No se reporta cédula catastral en el folio analizado.", "No cadastral ID is reported in the analyzed folio.");
        string nupre = NoVacio(model.Nupre)
            ? T(model, $"El Número Predial Nacional (NUPRE) reportado es {model.Nupre}.", $"Reported National Property Number (NUPRE): {model.Nupre}.")
            : T(model, "No se reporta NUPRE en el folio analizado.", "No NUPRE is reported in the analyzed folio.");

        CrearCuerpoJustificado(body, string.Join(" ", certificado, nupre));

        string cabida = NoVacio(model.ConclusionPredial)
            ? model.ConclusionPredial
            : T(model, $"Área registrada: {Valor(model.AreaRegistrada)}.", $"Registered area: {Valor(model.AreaRegistrada)}.");
        CrearCuerpoJustificado(body, $"{T(model, "Consistencia de cabidas", "Area consistency")}: {cabida}");
        body.Append(CrearParrafoSeparador());
    }

    private static void CrearSeccionConceptoJuridico(Body body, DiagnosticoPredialModel model)
    {
        CrearTituloSeccion(body, T(model, "7. CONCEPTO JURÍDICO DEL PREDIO", "7. LEGAL OPINION ON THE PROPERTY"));

        string dictamen = NoVacio(model.DiagnosticoEjecutivo)
            ? model.DiagnosticoEjecutivo
            : Valor(model.ResumenEjecutivo, T(model, "Sin dictamen ejecutivo disponible.", "No executive opinion available."));

        CrearCuerpoJustificado(body, dictamen);
        CrearBadgeViabilidad(body, model.Viabilidad, En(model));

        foreach (var alerta in model.AlertasJuridicas)
        {
            if (NoVacio(alerta.Descripcion))
            {
                CrearCuerpoJustificado(body, $"• {Valor(alerta.NivelRiesgo)}: {alerta.Descripcion}", negrita: false);
            }
        }
        body.Append(CrearParrafoSeparador());
    }

    private static void CrearBadgeViabilidad(Body body, EstadoViabilidad viabilidad, bool en = false)
    {
        var (etiqueta, color, fondo, borde) = ResolverViabilidad(viabilidad, en);

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
        parrafo.Append(CrearRun(en ? "CADASTRAL RULING: " : "DICTAMEN CATASTRAL: ", 11, Carbono, negrita: true));
        parrafo.Append(CrearRun(etiqueta, 11, color, negrita: true));
        celda.Append(parrafo);
        fila.Append(celda);
        tabla.Append(fila);

        body.Append(tabla);
        body.Append(CrearParrafoSeparador());
    }

    private static (string Etiqueta, string Color, string Fondo, string Borde) ResolverViabilidad(EstadoViabilidad viabilidad, bool en = false)
    {
        return viabilidad switch
        {
            EstadoViabilidad.Viable => (en ? "Viable" : "Viable", VerdeEsmeralda, VerdeFondo, VerdeBorde),
            EstadoViabilidad.AlertaCritica => (en ? "Not Viable" : "No Viable", RojoCarmesi, RojoFondo, RojoBorde),
            _ => (en ? "Requires Review" : "Requiere Revisión", GrisMuted, "F3F4F6", "9CA3AF")
        };
    }

    private static void CrearSeccionObservaciones(Body body, DiagnosticoPredialModel model)
    {
        CrearTituloSeccion(body, T(model, "8. OBSERVACIONES Y/O RECOMENDACIONES DE SANEAMIENTO", "8. OBSERVATIONS AND/OR REMEDIATION RECOMMENDATIONS"));

        if (model.Observaciones.Count == 0 && !NoVacio(model.ObservacionAmbiental) && !NoVacio(model.ExclusionResponsabilidad))
        {
            CrearCuerpoJustificado(body, T(model, "Sin observaciones adicionales.", "No additional observations."));
        }

        foreach (string observacion in model.Observaciones)
        {
            if (NoVacio(observacion))
            {
                CrearVineta(body, observacion);
            }
        }

        string obsAmbiental = NoVacio(model.ObservacionAmbiental)
            ? model.ObservacionAmbiental.Trim()
            : T(model,
                ObservacionAmbientalRuap,
                "RUNAP Verification: It is recommended to verify the property's geographic location on the official RUNAP portal (National Registry of Protected Areas) to rule out overlaps with conservation zones or natural parks.");
        CrearVineta(body, obsAmbiental);

        string exclusion = NoVacio(model.ExclusionResponsabilidad)
            ? model.ExclusionResponsabilidad.Trim()
            : T(model,
                ClausulaExclusionResponsabilidad,
                "Legal Notice: This diagnosis is limited to the documentary review provided. Aukaria does not perform checks against restrictive lists (OFAC) or judicial background databases.");
        CrearVineta(body, exclusion);

        body.Append(CrearParrafoSeparador());
    }

    private static void CrearSeccionDocumentosConsultados(Body body, DiagnosticoPredialModel model)
    {
        CrearTituloSeccion(body, T(model, "9. DOCUMENTOS CONSULTADOS", "9. REVIEWED DOCUMENTS"));

        if (model.DocumentosAnalizados.Count == 0)
        {
            string fmi = Valor(model.MatriculaFMI, T(model, "SIN FMI", "NO FMI"));
            CrearVineta(body, T(model,
                $"Certificado de Tradición y Libertad (CTL) del FMI {fmi}, expedido por la ORIP de {Valor(model.ORIP)}.",
                $"Certificate of Title & Encumbrances (CTL) for FMI {fmi}, issued by the ORIP of {Valor(model.ORIP)}."));
            return;
        }

        foreach (string documento in model.DocumentosAnalizados)
        {
            if (NoVacio(documento))
            {
                CrearVineta(body, documento);
            }
        }
        body.Append(CrearParrafoSeparador());
    }

    private static void CrearCuadroControlFirmas(Body body, DiagnosticoPredialModel model)
    {
        CrearTituloSeccion(body, T(model, "10. CUADRO DE CONTROL Y FIRMAS", "10. CONTROL AND SIGNATURE BLOCK"));

        var filas = new[]
        {
            new[]
            {
                T(model,
                    "Profesional Jurídico\n(Elaboró)\n\n\n______________________\nNombre / Firma",
                    "Legal Counsel\n(Prepared by)\n\n\n______________________\nName / Signature"),
                T(model,
                    "Profesional SIG / Campo\n(Elaboró)\n\n\n______________________\nNombre / Firma",
                    "GIS / Field Specialist\n(Prepared by)\n\n\n______________________\nName / Signature"),
                T(model,
                    "Coordinación de Gestión Inmobiliaria\n(Aprobó)\n\n\n______________________\nNombre / Firma",
                    "Real Estate Coordination\n(Approved by)\n\n\n______________________\nName / Signature")
            }
        };

        body.Append(CrearTablaFirmas(filas));
        body.Append(CrearParrafoSeparador());
    }

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
