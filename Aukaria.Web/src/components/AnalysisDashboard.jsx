import { motion, useReducedMotion } from "framer-motion"
import AnnotationsTimeline from "./AnnotationsTimeline"
import { resolvedorTipo } from "../utils/tiposDocumento"

const spring = { type: "spring", stiffness: 260, damping: 26 }

const VIABILITY = {
  viable: {
    label: "VIABLE",
    dot: "bg-emerald-400",
    note: "Cadena de tradición continua · Sin embargos activos",
  },
  revision: {
    label: "REQUIERE REVISIÓN",
    dot: "bg-amber-400",
    note: "Observaciones subsanables antes del cierre legal",
  },
  critica: {
    label: "ALERTA CRÍTICA",
    dot: "bg-rose-500",
    note: "Restricciones inscritas que impiden la negociación",
  },
}

const RISK = {
  alto: {
    label: "RIESGO ALTO",
    dot: "bg-rose-500",
    badge: "border-rose-500/20 bg-rose-500/5 text-rose-600",
  },
  medio: {
    label: "RIESGO MEDIO",
    dot: "bg-amber-400",
    badge: "border-amber-500/20 bg-amber-400/5 text-amber-700",
  },
  bajo: {
    label: "RIESGO BAJO",
    dot: "bg-emerald-500",
    badge: "border-emerald-500/20 bg-emerald-500/5 text-emerald-700",
  },
}

const DEFAULT_FICHA = [
  { label: "ORIP / Círculo de Registro", value: "ORIP Armenia · Círculo 196" },
  { label: "Departamento · Municipio", value: "Quindío · Armenia" },
  { label: "Área Registrada", value: "12,400 m² · 1.24 ha" },
  { label: "Propietario Actual", value: "María del R. Giraldo V." },
  { label: "Estado del Folio", value: "Activo", dot: "bg-emerald-500" },
  { label: "Expedición del Certificado", value: "12-08-2026 · Cert. N° 258741" },
]

const DEFAULT_RESUMEN =
  "De la revisión integra del Certificado de Tradición y Libertad, se desprende una cadena de " +
  "tradición ininterrumpida, sin tachaduras ni enmendaduras que comprometan la certeza del folio. " +
  "Las medidas cautelares y gravámenes inscritos deben ser levantados o saneados antes del cierre " +
  "de cualquier operación, de conformidad con la Ley 1579 de 2012."

const DEFAULT_ALERTS = [
  {
    riesgo: "alto",
    titulo: "Embargo preventivo inscrito",
    codigo: "Anotación N° 14 · Juzgado 3.° Civil Municipal",
    descripcion:
      "El folio registra embargo preventivo decretado con fines de remate. La medida inmoviliza la tradición e impide la cesión del dominio a terceros mientras subsista la anotación.",
    recomendacion:
      "Gestionar ante el juzgado el certificado de cancelación una vez el desembargo esté en firme. No comprometer la venta ni aceptar arras hasta la cancelación del gravamen.",
  },
  {
    riesgo: "medio",
    titulo: "Diferencia de cabida vs. catastro",
    codigo: "Anotación N° 09 · Base catastral IGAC",
    descripcion:
      "El área registrada difiere del área catastral vigente. La divergencia no afecta la identidad del folio, pero sí el avalúo y la liquidación de tributos.",
    recomendacion:
      "Promover rectificación de cabida y linderos ante la ORIP (Ley 1579 de 2012) o celebrar la negociación con cláusula de área según catastro.",
  },
  {
    riesgo: "bajo",
    titulo: "Servidumbre de tránsito por cancelar",
    codigo: "Anotación N° 06 · Registro de servidumbres",
    descripcion:
      "Se advierte servidumbre de tránsito a favor de la finca colindante. El beneficiario registrado falleció sin que conste la cancelación; la carga es de naturaleza técnica y no económicamente onerosa.",
    recomendacion:
      "Verificar la cesación de la servidumbre con el juzgado que la constituyó y cancelarla en el registro para sanear el folio antes del cierre.",
  },
]

function PulseDot({ className }) {
  return (
    <span className="relative flex h-2.5 w-2.5">
      <span
        className={`absolute inline-flex h-full w-full animate-ping rounded-full opacity-60 ${className}`}
      />
      <span className={`relative inline-flex h-2.5 w-2.5 rounded-full ${className}`} />
    </span>
  )
}

function viabilidadKey(valor) {
  const v = (valor || "").toLowerCase().replace(/[\s-]+/g, "")
  if (v.includes("requiere") || v === "requiererevision") return "revision"
  if (v.includes("critic") || v === "alertacritica") return "critica"
  return "viable"
}

function nivelRiesgo(valor) {
  const v = (valor || "").toLowerCase().replace(/riesgo/g, "").trim()
  if (v.includes("alto")) return "alto"
  if (v.includes("bajo")) return "bajo"
  return "medio"
}

function mapAnotacion(item, i) {
  return {
    nro: Number.parseInt(item.NumeroAnotacion, 10) || i + 1,
    fecha: item.Fecha || "—",
    naturaleza: item.NaturalezaJuridica || "ANOTACIÓN",
    EsGravamen: Boolean(item.EsGravamen),
    EsMedidaCautelar: Boolean(item.EsMedidaCautelar),
    EsFalsaTradicion: false,
    activa: Boolean(item.EsGravamen || item.EsMedidaCautelar),
    documento: "",
    de: "",
    a: "",
    cuantia: "",
    descripcion: item.Especificacion || "Sin especificación registral disponible.",
  }
}

function mapAlerta(item) {
  return {
    riesgo: nivelRiesgo(item.NivelRiesgo),
    titulo: item.Titulo || "Alerta detectada",
    codigo: "",
    descripcion: item.Descripcion || "Sin descripción disponible.",
    recomendacion: item.Recomendacion || "Se recomienda revisión jurídica manual.",
  }
}

export default function AnalysisDashboard({ analisis = null, viability = "viable", onDownload }) {
  const reduce = useReducedMotion()
  const r = analisis?.Resultado ?? null

  const predio = analisis?.NombrePredio || r?.NombrePredio || "Finca La Esperanza"
  const fmi = analisis?.MatriculaFMI || r?.MatriculaFMI || "196-2053"
  const viabilidad = viabilidadKey(analisis?.Viabilidad || r?.Viabilidad || viability)
  const tipo = resolvedorTipo(analisis?.TipoDocumento)

  const resumen = analisis?.ResumenEjecutivo || r?.ResumenEjecutivo || DEFAULT_RESUMEN

  const ficha = !analisis
    ? DEFAULT_FICHA
    : tipo.key === "EscrituraPublica"
      ? [
          { label: "Notaría · Ciudad", value: [r?.Notaria, r?.CiudadNotaria].filter(Boolean).join(" · ") || "No registrado" },
          { label: "Fecha de Otorgamiento", value: r?.FechaEscritura || "—" },
          { label: "Número de Escritura", value: r?.NumeroEscritura || "No registrado" },
          { label: "Cuantía", value: r?.Cuantia || "No registrada" },
          { label: "Otorgantes", value: r?.Otorgantes?.length ? r.Otorgantes.join(" · ") : "No registrados" },
          { label: "FMI Citado", value: fmi },
          { label: "Linderos", value: r?.Linderos || "No registrados" },
        ]
      : tipo.key === "VUR"
        ? [
            {
              label: "Estado Registral Reportado",
              value: (r?.EstadoFolio || "").trim() || "No reportado",
              dot: /(activo|abierto|vigente)/i.test(r?.EstadoFolio || "") ? "bg-emerald-500" : "bg-neutral-300",
            },
            { label: "Propietario Reportado", value: (r?.PropietarioActual || "").trim() || "No reportado" },
            {
              label: "Departamento · Municipio",
              value: [r?.Departamento, r?.Municipio].filter(Boolean).join(" · ") || "No reportado",
            },
            { label: "FMI Asociado", value: fmi || "No reportado" },
            { label: "Área Registrada", value: (r?.AreaRegistrada || "").trim() || "No reportada" },
          ]
        : (() => {
            const dp = (r?.Departamento || "").trim()
            const mp = (r?.Municipio || "").trim()
            const estadoFolio = (r?.EstadoFolio || "").trim()
            const folioDot = /(activo|abierto|vigente)/i.test(estadoFolio)
              ? "bg-emerald-500"
              : "bg-neutral-300"
            const fecha = analisis?.FechaAnalisis ? new Date(analisis.FechaAnalisis) : null
            const fechaTexto = fecha && !Number.isNaN(fecha.getTime()) ? fecha.toLocaleDateString("es-CO") : "—"
            return [
              { label: "ORIP / Círculo de Registro", value: (r?.ORIP || "").trim() || "No registrado" },
              {
                label: "Departamento · Municipio",
                value: dp || mp ? [dp, mp].filter(Boolean).join(" · ") : "No registrado",
              },
              { label: "Área Registrada", value: (r?.AreaRegistrada || "").trim() || "No registrada" },
              { label: "Propietario Actual", value: (r?.PropietarioActual || "").trim() || "No registrado" },
              { label: "Estado del Folio", value: estadoFolio || "No registrado", dot: folioDot },
              { label: "Fecha del Análisis", value: fechaTexto },
            ]
          })()

  const alertas = analisis
    ? r?.AlertasJuridicas?.length
      ? r.AlertasJuridicas.map(mapAlerta)
      : DEFAULT_ALERTS
    : DEFAULT_ALERTS

  const anotacionesProp = analisis && r?.Anotaciones?.length ? r.Anotaciones.map(mapAnotacion) : undefined

  const container = {
    hidden: {},
    show: {
      transition: {
        staggerChildren: reduce ? 0 : 0.09,
        delayChildren: reduce ? 0 : 0.05,
      },
    },
  }

  const item = {
    hidden: reduce ? { opacity: 0 } : { opacity: 0, y: 20 },
    show: { opacity: 1, y: 0, transition: spring },
  }

  const v = VIABILITY[viabilidad] ?? VIABILITY.viable

  const fmiLinea = (() => {
    const fmiReal = analisis ? analisis?.MatriculaFMI || r?.MatriculaFMI || "" : "196-2053"
    if (!fmiReal) return "Documento analizado por Aukaria Legal"
    return `FMI ${fmiReal} · ${tipo.key === "CTL" ? "Matrícula Inmobiliaria SNR" : "Aukaria Legal"}`
  })()

  return (
    <motion.div
      variants={container}
      initial="hidden"
      animate="show"
      className="mx-auto flex w-full max-w-5xl flex-col gap-5"
      aria-label="Dictamen Jurídico Automatizado"
    >
      <motion.div
        variants={item}
        className="rounded-3xl border border-black/10 bg-white/70 p-6 shadow-sm backdrop-blur-2xl md:p-8"
      >
        <div className="flex flex-col gap-6 md:flex-row md:items-start md:justify-between">
          <div className="min-w-0">
            <p className="text-[10px] font-black uppercase tracking-[0.2em] text-neutral-400">
              {tipo.nombre}
            </p>
            <h1 className="mt-2 text-3xl font-black tracking-tighter text-black md:text-4xl">
              {predio}
            </h1>
            <p className="mt-2 font-mono text-xs text-neutral-500">
              {fmiLinea}
            </p>

            <div className="mt-5 flex flex-wrap items-center gap-3">
              <span className="inline-flex items-center gap-2.5 rounded-full border border-black/10 bg-white/70 px-4 py-2 backdrop-blur-xl">
                <PulseDot className={v.dot} />
                <span className="font-mono text-xs font-bold uppercase tracking-widest text-neutral-800">
                  {v.label}
                </span>
              </span>
              <span className="max-w-[18rem] text-xs text-neutral-500">{v.note}</span>
            </div>
          </div>

          <div className="flex flex-col items-start gap-3 md:items-end">
            <motion.button
              type="button"
              onClick={onDownload}
              whileTap={{ scale: 0.97 }}
              className="rounded-full bg-black px-6 py-3 text-sm font-semibold text-white shadow-[0_8px_20px_rgba(0,0,0,0.25)] transition-transform duration-150 hover:scale-105"
            >
              Descargar Reporte Word (.docx)
            </motion.button>
          </div>
        </div>
      </motion.div>

      <motion.div
        variants={item}
        className="rounded-3xl border border-black/10 bg-white/70 p-6 shadow-sm backdrop-blur-2xl"
      >
        <p className="text-[10px] font-black uppercase tracking-[0.2em] text-neutral-400">
          Ficha Técnica Predial
        </p>
        <h2 className="mt-2 text-xl font-black tracking-tight text-black">
          {tipo.key === "EscrituraPublica"
            ? "Datos del otorgamiento"
            : tipo.key === "VUR"
              ? "Datos reportados por el VUR"
              : "Datos registrales del folio"}
        </h2>

        <div className="mt-5 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {ficha.map((f) => (
            <div
              key={f.label}
              className="rounded-2xl border border-black/10 bg-white/70 p-4 backdrop-blur-xl"
            >
              <p className="text-[10px] font-black uppercase tracking-[0.18em] text-neutral-400">
                {f.label}
              </p>
              <p className="mt-1.5 flex items-center gap-2 text-sm font-semibold text-black">
                {f.dot && <span className={`h-1.5 w-1.5 shrink-0 rounded-full ${f.dot}`} />}
                {f.value}
              </p>
            </div>
          ))}
        </div>
      </motion.div>

      <motion.div
        variants={item}
        className="rounded-3xl border border-black/10 bg-white/70 p-6 shadow-sm backdrop-blur-2xl md:p-8"
      >
        <p className="text-[10px] font-black uppercase tracking-[0.2em] text-neutral-400">
          Resumen Ejecutivo Jurídico
        </p>
        <h2 className="mt-2 text-xl font-black tracking-tight text-black">
          Diagnóstico técnico del certificado
        </h2>

        <p className="mt-5 max-w-3xl text-sm leading-loose text-neutral-700 md:text-[15px]">
          {resumen}
        </p>

        <p className="mt-5 font-mono text-[11px] text-neutral-400">
          Análisis emitido por Aukaria Legal · Resolución 1587 de 2012 · Ley 1579 de 2012
        </p>
      </motion.div>

      {(tipo.key === "CTL" || anotacionesProp) && (
        <motion.div
          variants={item}
          className="rounded-3xl border border-black/10 bg-white/70 p-6 shadow-sm backdrop-blur-2xl md:p-8"
        >
          <p className="text-[10px] font-black uppercase tracking-[0.2em] text-neutral-400">
            {tipo.nombre} · {fmi}
          </p>
          <h2 className="mt-2 text-xl font-black tracking-tight text-black md:text-2xl">
            {tipo.key === "EscrituraPublica"
              ? "Matriz de Anotaciones Relacionadas"
              : "Estructura Traditiva & Matriz de Anotaciones"}
          </h2>
          <div className="mt-5">
            <AnnotationsTimeline annotations={anotacionesProp} />
          </div>
        </motion.div>
      )}

      <motion.div variants={item} className="flex flex-col gap-4">
        <div>
          <p className="text-[10px] font-black uppercase tracking-[0.2em] text-neutral-400">
            Alertas Jurídicas
          </p>
          <h2 className="mt-2 text-xl font-black tracking-tight text-black">
            Riesgos detectados y gestión recomendada
          </h2>
        </div>

        <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
          {alertas.map((a) => {
            const rk = RISK[a.riesgo] ?? RISK.bajo
            return (
              <motion.article
                variants={item}
                key={a.titulo}
                className="flex flex-col gap-3 rounded-3xl border border-black/10 bg-white/70 p-5 shadow-sm backdrop-blur-2xl"
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <span
                    className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[10px] font-bold tracking-widest ${rk.badge}`}
                  >
                    <span className={`h-1.5 w-1.5 rounded-full ${rk.dot}`} />
                    {rk.label}
                  </span>
                  {a.codigo && (
                    <span className="font-mono text-[10px] text-neutral-400">{a.codigo}</span>
                  )}
                </div>

                <h3 className="text-base font-bold tracking-tight text-black">{a.titulo}</h3>
                <p className="text-[13px] leading-relaxed text-neutral-600">{a.descripcion}</p>

                <div className="mt-auto rounded-2xl bg-black/[0.04] p-3">
                  <p className="text-[10px] font-black uppercase tracking-[0.18em] text-neutral-400">
                    Recomendación de gestión
                  </p>
                  <p className="mt-1 text-xs leading-relaxed text-neutral-700">{a.recomendacion}</p>
                </div>
              </motion.article>
            )
          })}
        </div>
      </motion.div>
    </motion.div>
  )
}