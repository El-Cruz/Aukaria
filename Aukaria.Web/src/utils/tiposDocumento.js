export const TIPOS_DOCUMENTO = {
  CTL: {
    key: "CTL",
    emoji: "📄",
    sigla: "CTL",
    nombre: "Certificado de Tradición y Libertad",
    detalle: "CTL DETECTADO",
    badge: "border-emerald-500/25 bg-emerald-400/10 text-emerald-700",
    dot: "bg-emerald-500",
  },
  VUR: {
    key: "VUR",
    emoji: "🏛️",
    sigla: "VUR",
    nombre: "Informe VUR (Ventanilla Única Registral)",
    detalle: "CONSULTA VUR DETECTADA",
    badge: "border-sky-500/25 bg-sky-400/10 text-sky-700",
    dot: "bg-sky-500",
  },
  EscrituraPublica: {
    key: "EscrituraPublica",
    emoji: "📜",
    sigla: "ESCRITURA PÚBLICA",
    nombre: "Escritura Pública Notarial",
    detalle: "ESCRITURA PÚBLICA DETECTADA",
    badge: "border-indigo-500/25 bg-indigo-400/10 text-indigo-700",
    dot: "bg-indigo-500",
  },
  DocumentoInmobiliarioGeneral: {
    key: "DocumentoInmobiliarioGeneral",
    emoji: "📑",
    sigla: "DOCUMENTO JURÍDICO GENERAL",
    nombre: "Documento Inmobiliario General",
    detalle: "DOCUMENTO JURÍDICO GENERAL",
    badge: "border-neutral-500/25 bg-neutral-400/10 text-neutral-700",
    dot: "bg-neutral-500",
  },
}

export function resolvedorTipo(tipo) {
  if (!tipo) return TIPOS_DOCUMENTO.CTL
  return TIPOS_DOCUMENTO[tipo] || TIPOS_DOCUMENTO.DocumentoInmobiliarioGeneral
}

export function identificadorPreAnalisis(clasificacion, fmiFallback = "") {
  const tipo = clasificacion?.TipoDocumento
  const id = clasificacion?.IdentificadorExtraido || ""
  if (tipo === "CTL") return id ? `Matrícula: ${id}` : fmiFallback ? `Matrícula: ${fmiFallback}` : "Certificado SNR"
  if (tipo === "EscrituraPublica") return id ? id : "Escritura Pública"
  if (tipo === "VUR") return id ? id : "Reporte Registral"
  return ""
}

export const PREGUNTAS_POR_TIPO = {
  CTL: ["¿El predio tiene embargos activos?", "¿Hay afectación a vivienda familiar?"],
  VUR: ["¿El folio está al día en el registro?", "¿Qué alertas inmediatas muestra el VUR?"],
  EscrituraPublica: ["¿Qué gravámenes condicionan la escritura?", "¿Quiénes figuran como otorgantes?"],
  DocumentoInmobiliarioGeneral: ["¿Qué tipo de documento se analizó?", "¿Qué riesgos legales presenta?"],
}