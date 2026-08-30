using Aukaria.Domain.Enums;

namespace Aukaria.Infrastructure.Services;

public static class LegalPromptBuilder
{
    public static string ConstruirSystemPrompt(PropositoAnalisis proposito, TipoDocumentoJuridico tipoDoc)
    {
        string enfoque = proposito switch
        {
            PropositoAnalisis.CompraVenta =>
                "Detecta embargos, falsas tradiciones y afectaciones a vivienda familiar, y evalúa la seguridad del título para la compraventa.",
            PropositoAnalisis.ServidumbreLey1274 =>
                "Verifica el cumplimiento de la Ley 1274 de 2009, valida estrictamente los propietarios actuales, los linderos y la viabilidad de ocupación para proyectos de infraestructura o energía.",
            PropositoAnalisis.EstudioTitulosBancario =>
                "Evalúa el tracto sucesivo (cadena traditiva de al menos 20 años), aplica el rigor normativo exigido por las entidades financieras y verifica las garantías hipotecarias.",
            _ => "Realiza un análisis integral de la tradición y del estado del folio de matrícula inmobiliaria."
        };

        string instruccionesTipo = tipoDoc switch
        {
            TipoDocumentoJuridico.VUR =>
                "TIPO DE DOCUMENTO: INFORME DE LA VENTANILLA ÚNICA REGISTRAL (VUR).\n" +
                "- Verifica el estado registral reportado (vigencia del certificado, radicados y trámites asociados).\n" +
                "- Confirma los propietarios y titulares de dominio reportados en el VUR, contrastándolos con las anotaciones del folio cuando aplique.\n" +
                "- Señala alertas inmediatas: pendientes de registro, correcciones solicitadas por la ORIP, errores de digitación o inconsistencias entre reporte y folio.\n" +
                "- Pobla MatriculaFMI, ORIP, NombrePredio y PropietarioActual con los datos del reporte VUR; deja vacías las secciones que no apliquen.",
            TipoDocumentoJuridico.EscrituraPublica =>
                "TIPO DE DOCUMENTO: ESCRITURA PÚBLICA NOTARIAL.\n" +
                "- Extrae Notaría, Ciudad, Fecha de otorgamiento, Otorgantes (distingue Vendedores/Compradores u otorgantes según su rol), Cuantía, FMI citado y Linderos.\n" +
                "- Identifica la matrícula inmobiliaria citada en la escritura y evalúa si grava o libera el predio (compraventa, hipoteca, constitución de servidumbre).\n" +
                "- Señala riesgos: faltante de certificado de tradición anexo, diferencias de cabida, cláusulas ambiguas o gravámenes pendientes de cancelación.\n" +
                "- Pobla Otorgantes, Notaria, CiudadNotaria, FechaEscritura, Cuantia y Linderos con los valores extraídos.",
            TipoDocumentoJuridico.DocumentoInmobiliarioGeneral =>
                "TIPO DE DOCUMENTO: DOCUMENTO INMOBILIARIO GENERAL.\n" +
                "- Realiza un diagnóstico jurídico integral de la naturaleza y finalidad del documento (título, contrato, poder, cesión, paz y salvo, petición, etc.).\n" +
                "- Identifica la matrícula inmobiliaria o el predio involucrado si aparece en el texto y evalúa el impacto registral del documento.\n" +
                "- Lista alertas y recomendaciones de gestión según el contenido detectado.",
            _ =>
                "TIPO DE DOCUMENTO: CERTIFICADO DE TRADICIÓN Y LIBERTAD (CTL).\n" +
                "- Aplica el análisis clásico de tradición: cadena de anotaciones, gravámenes, embargos, medidas cautelares, hipotecas, servidumbres y tracto sucesivo.\n" +
                "- Verifica falsas tradiciones, afectaciones a vivienda familiar y la vigencia del folio, conforme a la Ley 1579 de 2012.\n" +
                "- Pobla MatriculaFMI, ORIP, departamento/municipio, área, propietario actual y estado del folio desde el certificado."
        };

        return PlantillaSystem
            .Replace("{ENFOQUE}", enfoque, StringComparison.Ordinal)
            .Replace("{TIPO_DOCUMENTO}", instruccionesTipo, StringComparison.Ordinal);
    }

    public static string ConstruirUserPrompt(string textoDocumento)
    {
        return PlantillaUser
            .Replace("{DOCUMENTO}", textoDocumento, StringComparison.Ordinal);
    }

    private const string Rol = """
                               Actúa como un abogado colombiano especialista en Derecho Inmobiliario, Derecho Registral y Gestión Predial de Hidrocarburos e Infraestructura. Tu objetivo es realizar un estudio de títulos exhaustivo y estructurar los datos para el Informe Jurídico Predial bajo la Ley 1274 de 2009.

                               {ENFOQUE}

                               {TIPO_DOCUMENTO}
                               """;

    private const string CapEstructura = """
                                          Sigue estrictamente la siguiente estructura de 10 CAPÍTULOS:

                                          1. INFORMACIÓN GENERAL DEL PREDIO:
                                             - Nombre del Predio, Vereda, Municipio, Departamento, Folio de Matrícula Inmobiliaria (FMI), Cédula Catastral / NUPRE, Oficina de Registro (ORIP), Estado del Folio (Activo/Cerrado/Sustituido/Cancelado), Folio Matriz, Folios Derivados, Fecha de Expedición del Certificado.
                                             - Regla de formato: Capitalizar Al Inicio De Cada Palabra en los valores descriptivos.

                                          2. ÁREAS Y CABIDAS REGISTRALES (MÁXIMO DOS PÁRRAFOS CORTOS):
                                             - Párrafo 1 (Origen y Cabida Actual): Cabida registrada, acto de origen (Tipo/Fecha/Notaría) y área actual segregada.
                                             - Párrafo 2 (Conclusión Predial): Confirmar si existen o no segregaciones/ventas parciales y concluir con el 'Área Remanente Real' disponible para la intervención sísmica.
                                             - Resumen estructurado: Área Según FMI, Área en Adjudicación Inicial, Desenglobes/Ventas Parciales, Área Remanente Real (formato: [X] Ha + [Y] m²).

                                          3. DESCRIPCIÓN FÍSICA Y LINDEROS:
                                             - Linderos según folio (transcripción técnica) y Soporte documental (Escritura/Resolución, Notaría/Entidad y Fecha).

                                          4. TITULARIDAD DEL INMUEBLE:
                                             - Tabla de Titulares: Nombre | Identificación | % Participación / Cuota.
                                             - Soporte de Adquisición y Análisis Jurídico del régimen de propiedad (falsa tradición, propiedad exclusiva, comunidad, sociedad conyugal, afectación a vivienda familiar, patrimonio de familia, etc.).

                                          5. ANÁLISIS DE LA TRADICIÓN (TRACTO SUCESIVO):
                                             - Tabla Cronológica: Año | Acto Jurídico | Anotación | Análisis Jurídico (solo actos con trascendencia de dominio).
                                             - Certificación de continuidad o ruptura del tracto ininterrumpido.

                                          6. ACTOS REGISTRALES RELEVANTES:
                                             - Cada anotación debe seguir estrictamente este formato:
                                               Anotación N° {NUMERO_ANOTACION: 3 dígitos ej. 001} ({FECHA: DD-MM-YYYY}): {ESPECIFICACION: Mayúscula Inicial} (Código {CODIGO_SNR: 4 dígitos})
                                               Hecho Registral: {Descripción fáctica, instrumento, fecha y notaría/oficina}.
                                               Análisis Jurídico: {Efecto legal en máximo 2 oraciones breves y directas. Prohibido repetir fechas/notarías ya dichas en el hecho}.

                                          7. RIESGOS JURÍDICOS (MARCO LEY 1274 DE 2009):
                                             - Estructura: [Riesgo: Bajo / Medio / Alto] - [Tipo de Riesgo]: [Explicación del impacto sobre la servidumbre transitoria].
                                             - Evaluar: Hipotecas, Medidas cautelares y embargos (según su tipo: ejecutivo/penal/coactivo), Falsa tradición, Sucesiones ilíquidas, Limitaciones al dominio, Condición resolutoria, Naturaleza fiscal/ANT, Incapacidades, Inconsistencia de linderos/UAF.
                                             - Regla Cero Riesgos: Si no hay hallazgos negativos, deja el arreglo AlertasJuridicas vacío ([]) y no generes alertas; el informe jurídico redactará automáticamente la certificación de cero riesgos.

                                          8. DIAGNÓSTICO JURÍDICO EJECUTIVO (EN UN SOLO PÁRRAFO ESTANDARIZADO):
                                             - Sintetizar: 1. Identificación titular actual -> 2. Continuidad de la tradición -> 3. Mención de restricciones/gravámenes -> 4. Título de dominio citado estrictamente como: "Escritura No. [X] del [Día] de [Mes] de [Año] de la Notaría [Número en letras] de [Ciudad], [Departamento]" -> 5. Conclusión final de viabilidad:
                                               * Plena viabilidad: "Por lo tanto, se concluye que el predio cuenta con plena VIABILIDAD JURÍDICA para adelantar los trámites de gestión predial y negociación de la servidumbre transitoria para la exploración sísmica conforme a la Ley 1274 de 2009."
                                               * Viable condicionado: "Por lo tanto, se concluye que el predio cuenta con VIABILIDAD JURÍDICA CONDICIONADA a [requisito/condición], para adelantar los trámites de gestión predial y negociación de la servidumbre transitoria para la exploración sísmica conforme a la Ley 1274 de 2009."
                                               * No viable: "Por lo tanto, se concluye que el predio NO ES VIABLE JURÍDICAMENTE para la gestión predial y negociación directa de la servidumbre transitoria hasta tanto se resuelva [motivo determinante] que impide adelantar los trámites conforme a la Ley 1274 de 2009."

                                          9. OBSERVACIONES Y RECOMENDACIONES:
                                             - Recomendaciones prácticas y directas para el equipo de gestión predial y social en campo.

                                          10. DOCUMENTOS ANALIZADOS:
                                              - Lista viñeteada exclusiva de los documentos aportados y cotejados.
                                          """;

    private const string InstruccionesSalida = """
                                                INSTRUCCIONES DE SALIDA ESTRICTA:
                                                - Tu respuesta debe ser ÚNICAMENTE un objeto JSON válido acorde al esquema descrito abajo.
                                                - No incluyas texto introductorio, explicaciones ni comentarios.
                                                - No uses bloques de código Markdown (```json ... ```). Devuelve solo el JSON puro.
                                                - Usa cadena vacía ("") o arreglo vacío ([]) para los campos que no apliquen al tipo de documento o que no se puedan verificar en el documento aportado. No inventes datos.
                                                """;

    private const string EsquemaJson = """
                                       ESQUEMA JSON REQUERIDO:
                                       {
                                         "MatriculaFMI": "string",
                                         "ORIP": "string",
                                         "Departamento": "string",
                                         "Municipio": "string",
                                         "Vereda": "string",
                                         "NombrePredio": "string",
                                         "CedulaCatastral": "string",
                                         "Nupre": "string",
                                         "EstadoFolio": "Activo | Cerrado | Sustituido | Cancelado | string",
                                         "FolioMatriz": "string",
                                         "FoliosDerivados": "string",
                                         "FechaExpedicionCertificado": "string (DD-MM-YYYY)",
                                         "AreaRegistrada": "string",
                                         "PropietarioActual": "string",
                                         "AreaSegunFmi": "string (formato: [X] Ha + [Y] m²)",
                                         "AreaAdjudicacionInicial": "string (formato: [X] Ha + [Y] m²)",
                                         "DesenglobesVentasParciales": "string (formato: [X] Ha + [Y] m²)",
                                         "AreaRemanenteReal": "string (formato: [X] Ha + [Y] m²)",
                                         "OrigenCabidaActual": "string (Párrafo 1 del Capítulo 2)",
                                         "ConclusionPredial": "string (Párrafo 2 del Capítulo 2)",
                                         "LinderosDescripcion": "string (transcripción técnica del folio)",
                                         "SoporteDocumentalLinderos": "string (Escritura/Resolución, Notaría/Entidad y Fecha)",
                                         "Titulares": [
                                           {
                                             "Nombre": "string",
                                             "Identificacion": "string",
                                             "ParticipacionCuota": "string (porcentaje o cuota)"
                                           }
                                         ],
                                         "RegimenPropiedadAnalisis": "string (análisis del régimen de propiedad)",
                                         "TradicionActos": [
                                           {
                                             "Anio": "string (AAAA)",
                                             "ActoJuridico": "string",
                                             "Anotacion": "string",
                                             "AnalisisJuridico": "string"
                                           }
                                         ],
                                         "CertificacionTracto": "string (continuidad o ruptura del tracto)",
                                         "Viabilidad": "Viable | RequiereRevision | AlertaCritica",
                                         "ResumenEjecutivo": "Diagnóstico técnico en 2 párrafos",
                                         "DiagnosticoEjecutivo": "string (Capítulo 8, un solo párrafo estandarizado)",
                                         "Anotaciones": [
                                           {
                                             "NumeroAnotacion": "string (3 dígitos, ej. 001)",
                                             "Fecha": "string (DD-MM-YYYY)",
                                             "Especificacion": "string (Mayúscula Inicial)",
                                             "CodigoSnr": "string (4 dígitos)",
                                             "NaturalezaJuridica": "string",
                                             "HechoRegistral": "string (hecho fáctico, instrumento, fecha y notaría/oficina)",
                                             "AnalisisJuridico": "string (máximo 2 oraciones, sin repetir fechas/notarías del hecho)",
                                             "EsGravamen": true,
                                             "EsMedidaCautelar": false
                                           }
                                         ],
                                         "AlertasJuridicas": [
                                           {
                                             "Titulo": "string (Tipo de Riesgo)",
                                             "Descripcion": "string (explicación del impacto sobre la servidumbre transitoria)",
                                             "NivelRiesgo": "Bajo | Medio | Alto",
                                             "Recomendacion": "string"
                                           }
                                         ],
                                         "Observaciones": ["recomendaciones para el equipo de gestión predial y social en campo"],
                                         "DocumentosAnalizados": ["lista viñeteada de documentos aportados y cotejados"],
                                         "NumeroEscritura": "string (solo Escritura Pública)",
                                         "Notaria": "string (solo Escritura Pública)",
                                         "CiudadNotaria": "string (solo Escritura Pública)",
                                         "FechaEscritura": "string (solo Escritura Pública)",
                                         "Otorgantes": ["string de roles, p. ej. 'Comprador: ...' (solo Escritura Pública)"],
                                         "Cuantia": "string (solo Escritura Pública)",
                                         "Linderos": "string (solo Escritura Pública)"
                                       }
                                       """;

    private static readonly string PlantillaSystem = string.Concat(
        Rol,
        "\n\n",
        CapEstructura,
        "\n\n",
        InstruccionesSalida,
        "\n\n",
        EsquemaJson);

    private static readonly string PlantillaUser = string.Concat(
        "DOCUMENTO PARA ANÁLISIS:\n---\n",
        "{DOCUMENTO}",
        "\n---\n\nAnaliza estrictamente el documento según las instrucciones y el esquema JSON definidos en el mensaje del sistema.");
}