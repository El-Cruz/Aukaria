import { useState } from "react"
import { AnimatePresence, motion } from "framer-motion"
import AnnotationsTimeline from "./AnnotationsTimeline"
import LegalNoticeModal from "./LegalNoticeModal"
import { resolvedorTipo } from "../utils/tiposDocumento"

const spring = { type: "spring", stiffness: 260, damping: 26 }

const VIABILITY = {
  viable: {
    label: "VIABLE",
    dot: "bg-emerald-500",
    badge: "border-emerald-500/20 bg-emerald-500/10 text-emerald-600",
    note: "Cadena de tradición continua · Sin embargos activos",
  },
  revision: {
    label: "VIABLE CONDICIONADO",
    dot: "bg-amber-500",
    badge: "border-amber-500/20 bg-amber-500/10 text-amber-600",
    note: "Observaciones subsanables antes del cierre legal",
  },
  critica: {
    label: "NO VIABLE",
    dot: "bg-rose-500",
    badge: "border-rose-500/20 bg-rose-500/10 text-rose-600",
    note: "Restricciones inscritas que impiden la negociación",
  },
}

const RISK = {
  alto: { label: "RIESGO ALTO", dot: "bg-rose-500", badge: "border-rose-500/20 bg-rose-500/5 text-rose-600" },
  medio: { label: "RIESGO MEDIO", dot: "bg-amber-500", badge: "border-amber-500/20 bg-amber-500/5 text-amber-700" },
  bajo: { label: "RIESGO BAJO", dot: "bg-emerald-500", badge: "border-emerald-500/20 bg-emerald-500/5 text-emerald-700" },
}

const TEXTO_CERO_RIESGOS =
  "No se identifican riesgos jurídicos. El inmueble cuenta con cadena registral y tracto sucesivo saneados, y dominio consolidado en el titular según el Folio de Matrícula Inmobiliaria, encontrándose libre de gravámenes, medidas cautelares, falsa tradición o limitaciones que afecten la viabilidad bajo la Ley 1274 de 2009."

const OBSERVACION_RUNAP =
  "Verificación Preventiva en RUNAP: Se recomienda validar la georreferenciación del predio en el portal oficial del Registro Único Nacional de Áreas Protegidas (RUNAP), para verificar que no coincida con zonas de exclusión o reservas de conservación estricta."

function viabilidadKey(valor) {
  const v = (valor || "").toLowerCase().replace(/[\s-]+/g, "")
  if (v.includes("requiere") || v === "requiererevision") return "revision"
  if (v.includes("critic") || v === "alertacritica" || v.includes("noviable")) return "critica"
  return "viable"
}

function nivelRiesgo(valor) {
  const v = (valor || "").toLowerCase().replace(/riesgo/g, "").trim()
  if (v.includes("alto")) return "alto"
  if (v.includes("bajo")) return "bajo"
  return "medio"
}

function mapAlerta(item) {
  return {
    riesgo: nivelRiesgo(item?.NivelRiesgo),
    titulo: item?.Titulo || "Alerta detectada",
    codigo: item?.CodigoSnr || "",
    descripcion: item?.Descripcion || "Sin descripción disponible.",
    recomendacion: item?.Recomendacion || "Se recomienda revisión jurídica manual.",
  }
}

function mapAnotacion(item, i) {
  return {
    nro: Number.parseInt(item?.NumeroAnotacion, 10) || i + 1,
    fecha: item?.Fecha || "—",
    naturaleza: item?.NaturalezaJuridica || "ANOTACIÓN",
    EsGravamen: Boolean(item?.EsGravamen),
    EsMedidaCautelar: Boolean(item?.EsMedidaCautelar),
    EsFalsaTradicion: false,
    activa: Boolean(item?.EsGravamen || item?.EsMedidaCautelar),
    documento: "",
    de: "",
    a: "",
    cuantia: "",
    descripcion: item?.Especificacion || "Sin especificación registral disponible.",
  }
}

function capitalizar(valor) {
  const v = (valor || "").trim()
  if (!v) return v
  return v
    .toLowerCase()
    .split(/\s+/)
    .map((p) => (p ? p[0].toUpperCase() + p.slice(1) : p))
    .join(" ")
}

function CampoValor({ campo, valor }) {
  return (
    <div className="rounded-2xl border border-neutral-200/70 bg-white/70 p-4 backdrop-blur-xl">
      <p className="text-[10px] font-black uppercase tracking-[0.18em] text-neutral-400">{campo}</p>
      <p className="mt-1.5 text-sm font-semibold text-neutral-900">{valor}</p>
    </div>
  )
}

function SeccionCard({ num, titulo, children }) {
  return (
    <motion.section
      variants={item}
      className="rounded-3xl border border-neutral-200 bg-white/80 p-6 shadow-sm backdrop-blur-md md:p-7"
    >
      <p className="font-mono text-[10px] font-bold uppercase tracking-[0.25em] text-neutral-400">
        Sección {num}
      </p>
      <h2 className="mt-1.5 text-xl font-black tracking-tight text-neutral-900">{titulo}</h2>
      <div className="mt-5">{children}</div>
    </motion.section>
  )
}

function TablaComparativa({ head, filas }) {
  return (
    <div className="overflow-hidden rounded-2xl border border-neutral-200">
      <table className="w-full border-collapse text-left text-[13px]">
        <thead>
          <tr className="bg-neutral-900 text-white">
            {head.map((h) => (
              <th key={h} className="px-3.5 py-2.5 font-mono text-[10px] font-bold uppercase tracking-widest">
                {h}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {filas.map((fila, i) => (
            <tr key={i} className={i % 2 === 1 ? "bg-neutral-50/80" : "bg-white/70"}>
              {fila.map((celda, j) => (
                <td key={j} className={`px-3.5 py-2.5 align-top text-neutral-700 ${j === 0 ? "font-semibold" : ""}`}>
                  {celda}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

const container = {
  hidden: {},
  show: { transition: { staggerChildren: 0.06, delayChildren: 0.04 } },
}

const item = {
  hidden: { opacity: 0, y: 16 },
  show: { opacity: 1, y: 0, transition: spring },
}

export default function AnalysisDashboard({
  analisis = null,
  viability = "viable",
  onDownload = () => {},
  onDownloadAnexo = () => {},
  onEnviarPorCorreo = () => {},
  preAnalisisData = null,
}) {
  const [firmaSolicitada, setFirmaSolicitada] = useState(false)
  const [avisoLegal, setAvisoLegal] = useState(false)

  const r = analisis?.Resultado ?? null
  const infoGeneral = r?.informacionGeneral || r?.capitulo1 || r || {}

  const fmi =
    infoGeneral.MatriculaFMI ||
    infoGeneral.matriculaFMI ||
    infoGeneral.FolioMatricula ||
    infoGeneral.folioMatricula ||
    analisis?.MatriculaFMI ||
    preAnalisisData?.matricula ||
    "Sin Información"

  const nombrePredio =
    infoGeneral.NombrePredio ||
    infoGeneral.nombrePredio ||
    analisis?.NombrePredio ||
    preAnalisisData?.nombrePredio ||
    "Predio No Identificado"

  const orip =
    infoGeneral.ORIP ||
    infoGeneral.orip ||
    infoGeneral.oficinaRegistro ||
    infoGeneral.OficinaRegistro ||
    analisis?.ORIP ||
    preAnalisisData?.orip ||
    "Sin Información"

  const departamento =
    infoGeneral.Departamento ||
    infoGeneral.departamento ||
    analisis?.Departamento ||
    preAnalisisData?.departamento ||
    "Sin Información"

  const municipio =
    infoGeneral.Municipio ||
    infoGeneral.municipio ||
    analisis?.Municipio ||
    preAnalisisData?.municipio ||
    "Sin Información"

  const cedulaCatastral =
    infoGeneral.CedulaCatastral ||
    infoGeneral.cedulaCatastral ||
    r?.CedulaCatastral ||
    preAnalisisData?.cedulaCatastral ||
    ""

  const viabilidad = viabilidadKey(analisis?.Viabilidad || r?.Viabilidad || viability)
  const tipo = resolvedorTipo(analisis?.TipoDocumento)
  const v = VIABILITY[viabilidad] ?? VIABILITY.viable

  const fmiLinea =
    fmi !== "Sin Información"
      ? `FMI ${fmi} · ${tipo.key === "CTL" ? "Matrícula Inmobiliaria SNR" : "Aukaria Legal"}`
      : "Documento analizado por Aukaria Legal"

  const alertas = (analisis && r?.AlertasJuridicas?.length ? r.AlertasJuridicas : []).map(mapAlerta)
  const tieneRiesgos = alertas.length > 0
  const anotacionesProp = analisis && r?.Anotaciones?.length ? r.Anotaciones.map(mapAnotacion) : undefined

  const titulares = (analisis && r?.Titulares?.length ? r.Titulares : []).map((t) => ({
    nombre: capitalizar(t?.Nombre || ""),
    tipoDoc: "Cédula de ciudadanía",
    identificacion: t?.Identificacion || "",
    condicion: t?.CondicionDominio || "",
    pct: t?.ParticipacionCuota || "",
  }))
  const titularesVisibles =
    titulares.length > 0
      ? titulares
      : [
          {
            nombre: capitalizar(r?.PropietarioActual || analisis?.NombrePredio || "No reportado"),
            tipoDoc: "No reportado",
            identificacion: "No reportado",
            condicion: "No reportado",
            pct: "No reportado",
          },
        ]

  const observaciones = (analisis && r?.Observaciones?.length ? r.Observaciones : []).filter(Boolean)
  const observacionRuap = (analisis && r?.ObservacionAmbiental?.trim()) || OBSERVACION_RUNAP
  const documentos = (analisis && r?.DocumentosAnalizados?.length ? r.DocumentosAnalizados : []).filter(Boolean)
  const resumen = analisis?.ResumenEjecutivo || r?.ResumenEjecutivo || "Sin resumen ejecutivo disponible."
  const dictamen = r?.DiagnosticoEjecutivo || r?.ResumenEjecutivo || resumen

  return (
    <motion.div
      variants={container}
      initial="hidden"
      animate="show"
      className="mx-auto flex w-full max-w-5xl flex-col gap-5"
      aria-label="Diagnóstico Jurídico Catastral"
    >
      {/* 1. Encabezado institucional */}
      <motion.section
        variants={item}
        className="rounded-3xl border border-neutral-200 bg-white/80 p-6 shadow-sm backdrop-blur-md md:p-8"
      >
        <div className="flex flex-col gap-6 md:flex-row md:items-start md:justify-between">
          <div className="min-w-0">
            <div className="flex items-center gap-2.5">
              <span className="flex h-8 w-8 items-center justify-center rounded-xl bg-neutral-900 font-mono text-xs font-black text-white">
                AK
              </span>
              <p className="font-mono text-[10px] font-bold uppercase tracking-[0.2em] text-neutral-400">
                Aukaria · {tipo.nombre}
              </p>
            </div>
            <h1 className="mt-3 text-2xl font-black tracking-tight text-neutral-900 md:text-4xl">
              Diagnóstico Jurídico Catastral
            </h1>
            <p className="mt-2 font-mono text-xs text-neutral-500">{fmiLinea}</p>
            <p className="mt-1 text-sm font-bold text-neutral-800">{capitalizar(nombrePredio)}</p>

            <div className="mt-5 flex flex-wrap items-center gap-3">
              <span
                className={`inline-flex items-center gap-2.5 rounded-full border px-4 py-2 backdrop-blur-xl ${v.badge}`}
              >
                <span className="relative flex h-2 w-2">
                  <span className={`absolute inline-flex h-full w-full animate-ping rounded-full opacity-60 ${v.dot}`} />
                  <span className={`relative inline-flex h-2 w-2 rounded-full ${v.dot}`} />
                </span>
                <span className="font-mono text-xs font-bold uppercase tracking-widest">{v.label}</span>
              </span>
              <span className="max-w-[18rem] text-xs text-neutral-500">{v.note}</span>
            </div>
          </div>

          <div className="flex flex-col items-start gap-2.5 md:items-end">
            <motion.button
              type="button"
              onClick={onDownload}
              whileTap={{ scale: 0.97 }}
              className="rounded-full bg-neutral-900 px-6 py-3 text-sm font-semibold text-white shadow-md transition-transform duration-150 hover:bg-neutral-800"
            >
              Descargar Diagnóstico Jurídico (.docx)
            </motion.button>
            <motion.button
              type="button"
              onClick={onDownloadAnexo}
              whileTap={{ scale: 0.97 }}
              className="inline-flex items-center gap-2 rounded-full border border-neutral-200 bg-white/60 px-5 py-2.5 text-xs font-semibold text-neutral-700 backdrop-blur-xl transition-colors duration-150 hover:bg-white hover:text-neutral-900"
            >
              <svg viewBox="0 0 24 24" className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
                <path d="M13 2 3 14h7l-1 8 11-13h-7l1-7z" />
              </svg>
              Descargar Anexo: Tracto Sucesivo (.docx)
            </motion.button>
            <motion.button
              type="button"
              onClick={onEnviarPorCorreo}
              whileTap={{ scale: 0.97 }}
              className="inline-flex items-center gap-2 rounded-full border border-neutral-200 bg-white/60 px-5 py-2.5 text-xs font-semibold text-neutral-700 backdrop-blur-xl transition-colors duration-150 hover:bg-white hover:text-neutral-900"
            >
              <svg viewBox="0 0 24 24" className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
                <rect x="2" y="4" width="20" height="16" rx="2" />
                <path d="m22 7-10 5L2 7" />
              </svg>
              Enviar por correo
            </motion.button>
            <motion.button
              type="button"
              whileTap={{ scale: 0.97 }}
              onClick={() => setFirmaSolicitada(true)}
              className="inline-flex items-center gap-2 rounded-full border border-neutral-200 bg-white/60 px-5 py-2.5 text-xs font-semibold text-neutral-700 backdrop-blur-xl transition-colors duration-150 hover:bg-white hover:text-neutral-900"
            >
              <svg viewBox="0 0 24 24" className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
                <path d="M7 17a5 5 0 1 1 9.8-1.5A3.5 3.5 0 0 1 16 21H7a3 3 0 0 1-3-3v-1a2 2 0 0 1 2-2" />
                <path d="M12 8V2m0 0 3 3m-3-3L9 5" />
              </svg>
              Solicitar Firma Digital
            </motion.button>
            <AnimatePresence>
              {firmaSolicitada && (
                <motion.p
                  initial={{ opacity: 0, y: 4 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0 }}
                  transition={spring}
                  className="max-w-[16rem] font-mono text-[10px] leading-relaxed text-neutral-500"
                >
                  Solicitud enviada. Un abogado colegiado se contactará para la autenticación y firma
                  digital del dictamen.
                </motion.p>
              )}
            </AnimatePresence>
          </div>
        </div>
      </motion.section>

      {/* 2. Ficha predial y localización */}
      <SeccionCard num="01" titulo="Ficha Predial y Localización">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          <CampoValor campo="No. Folio Matrícula (FMI)" valor={fmi !== "Sin Información" ? fmi : "No reportado"} />
          <CampoValor campo="FMI Matriz" valor={r?.FolioMatriz || "No reportado"} />
          <CampoValor campo="Cédula Catastral" valor={cedulaCatastral || "No reportada"} />
          <CampoValor campo="ORIP / Círculo" valor={orip} />
          <CampoValor campo="Departamento · Municipio" valor={[departamento, municipio].filter((x) => x !== "Sin Información").join(" · ") || "Sin Información"} />
          <CampoValor campo="Estado del Folio" valor={r?.EstadoFolio || "No reportado"} />
        </div>

        <div className="mt-6">
          <p className="mb-2.5 font-mono text-[10px] font-bold uppercase tracking-widest text-neutral-400">
            Localización comparada
          </p>
          <TablaComparativa
            head={["Jurisdicción / Nivel", "Según FMI", "Inspección en Campo"]}
            filas={[
              ["Vereda", r?.Vereda || "No reportado", "Por confirmar"],
              ["Municipio", municipio !== "Sin Información" ? municipio : "No reportado", "Por confirmar"],
              ["Departamento", departamento !== "Sin Información" ? departamento : "No reportado", "Por confirmar"],
            ]}
          />
        </div>
      </SeccionCard>

      {/* 3. Áreas, cabidas y linderos */}
      <SeccionCard num="02" titulo="Áreas, Cabidas y Linderos">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <CampoValor campo="Área según Registro (FMI)" valor={r?.AreaSegunFmi || r?.AreaRegistrada || "No reportada"} />
          <CampoValor campo="Área según Inspección de Campo" valor="No reportada" />
        </div>
        <div className="mt-5">
          <p className="mb-2 font-mono text-[10px] font-bold uppercase tracking-widest text-neutral-400">
            Linderos del predio
          </p>
          <p className="text-[13px] leading-loose text-neutral-700">
            Según FMI: {r?.LinderosDescripcion || r?.Linderos || "Sin transcripción de linderos disponible."}
          </p>
          {(r?.SoporteDocumentalLinderos || "").trim() && (
            <p className="mt-2 text-[13px] leading-loose text-neutral-600">
              Acto de soporte: {r.SoporteDocumentalLinderos}
            </p>
          )}
        </div>
      </SeccionCard>

      {/* 4. Titularidad y régimen de dominio */}
      {(tipo.key === "CTL" || tipo.key === "VUR" || titulares.length > 0 || anotacionesProp) && (
        <SeccionCard num="03" titulo="Titularidad y Régimen de Dominio">
          <TablaComparativa
            head={["Nombre Propietario Legal", "Tipo Doc.", "Identificación", "Posición frente al Dominio", "% Propiedad"]}
            filas={titularesVisibles.map((t) => [
              t.nombre,
              t.tipoDoc,
              t.identificacion,
              t.condicion,
              t.pct,
            ])}
          />

          {anotacionesProp && (
            <div className="mt-6">
              <p className="mb-2.5 font-mono text-[10px] font-bold uppercase tracking-widest text-neutral-400">
                Régimen histórico de la tradición
              </p>
              <AnnotationsTimeline annotations={anotacionesProp} />
            </div>
          )}

          {(r?.RegimenPropiedadAnalisis || "").trim() && (
            <p className="mt-4 text-[13px] leading-loose text-neutral-600">{r.RegimenPropiedadAnalisis}</p>
          )}
        </SeccionCard>
      )}

      {/* 5. Matriz de riesgos jurídicos */}
      <SeccionCard num="04" titulo="Matriz de Riesgos Jurídicos (Ley 1274 de 2009)">
        {!tieneRiesgos ? (
          <div className="rounded-2xl border border-emerald-500/20 bg-emerald-500/5 p-5">
            <div className="flex items-center gap-2">
              <span className="h-2 w-2 rounded-full bg-emerald-500" />
              <p className="text-sm font-bold uppercase tracking-widest text-emerald-700">Cero riesgos</p>
            </div>
            <p className="mt-2.5 text-[13px] leading-relaxed text-neutral-700">{TEXTO_CERO_RIESGOS}</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            {alertas.map((a) => {
              const rk = RISK[a.riesgo] ?? RISK.bajo
              return (
                <motion.article
                  variants={item}
                  key={a.titulo}
                  className="flex flex-col gap-3 rounded-2xl border border-neutral-200 bg-white/70 p-5"
                >
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <span className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[10px] font-bold tracking-widest ${rk.badge}`}>
                      <span className={`h-1.5 w-1.5 rounded-full ${rk.dot}`} />
                      {rk.label}
                    </span>
                    {a.codigo && <span className="font-mono text-[10px] text-neutral-400">{a.codigo}</span>}
                  </div>
                  <h3 className="text-base font-bold tracking-tight text-neutral-900">{a.titulo}</h3>
                  <p className="text-[13px] leading-relaxed text-neutral-600">{a.descripcion}</p>
                  <div className="mt-auto rounded-xl bg-neutral-50 p-3">
                    <p className="text-[10px] font-black uppercase tracking-[0.18em] text-neutral-400">
                      Recomendación de gestión
                    </p>
                    <p className="mt-1 text-xs leading-relaxed text-neutral-700">{a.recomendacion}</p>
                  </div>
                </motion.article>
              )
            })}
          </div>
        )}
      </SeccionCard>

      {/* 6. Diagnóstico jurídico ejecutivo */}
      <SeccionCard num="05" titulo="Diagnóstico Jurídico Ejecutivo">
        <blockquote className="border-l-4 border-neutral-900 pl-4">
          <p className="text-[15px] leading-loose text-neutral-800">{dictamen}</p>
        </blockquote>
        <p className="mt-4 font-mono text-[11px] text-neutral-400">
          Plataforma de Auditoría Predial Aukaria · Ley 1274 de 2009 · Ley 1579 de 2012
        </p>
      </SeccionCard>

      {/* 7. Observaciones, recomendaciones y alertas de cumplimiento */}
      <SeccionCard num="06" titulo="Observaciones, Recomendaciones y Alertas de Cumplimiento">
        {observaciones.length === 0 ? (
          <p className="text-[13px] leading-relaxed text-neutral-600">Sin observaciones adicionales.</p>
        ) : (
          <ul className="space-y-2.5">
            {observaciones.map((o, i) => (
              <li key={i} className="flex items-start gap-2.5 text-[13px] leading-relaxed text-neutral-700">
                <span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-neutral-400" />
                {o}
              </li>
            ))}
          </ul>
        )}

        <div className="mt-5 rounded-2xl border border-emerald-500/25 bg-emerald-500/5 p-4">
          <div className="flex items-center gap-2">
            <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-emerald-500/15 text-emerald-700">
              <svg viewBox="0 0 24 24" className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth={2}>
                <path d="M12 22c4-3 6-6 6-10a6 6 0 1 0-12 0c0 4 2 7 6 10zM12 9v4M10 11h4" strokeLinecap="round" strokeLinejoin="round" />
              </svg>
            </span>
            <p className="text-sm font-bold uppercase tracking-widest text-emerald-800">Alerta de Cumplimiento · RUNAP</p>
          </div>
          <p className="mt-2.5 text-[13px] leading-relaxed text-neutral-700">{observacionRuap}</p>
        </div>
      </SeccionCard>

      {/* 8. Documentos consultados */}
      <SeccionCard num="07" titulo="Documentos Consultados">
        {documentos.length === 0 ? (
          <ul className="space-y-2.5">
            <li className="flex items-start gap-2.5 text-[13px] leading-relaxed text-neutral-700">
              <span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-neutral-400" />
              Certificado de Tradición y Libertad del FMI {fmi !== "Sin Información" ? fmi : "sin identificar"}, expedido por la ORIP de {orip}.
            </li>
          </ul>
        ) : (
          <ul className="space-y-2.5">
            {documentos.map((d, i) => (
              <li key={i} className="flex items-start gap-2.5 text-[13px] leading-relaxed text-neutral-700">
                <span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-neutral-400" />
                {d}
              </li>
            ))}
          </ul>
        )}
      </SeccionCard>

      {/* Pie: aviso legal */}
      <motion.footer variants={item} className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-neutral-200 bg-white/60 px-5 py-4 backdrop-blur-md">
        <p className="font-mono text-[10px] uppercase tracking-widest text-neutral-400">
          Documento con valor informativo · Aukaria Legal
        </p>
        <button
          type="button"
          onClick={() => setAvisoLegal(true)}
          className="inline-flex items-center gap-2 rounded-full border border-neutral-200 bg-white/70 px-4 py-2 text-xs font-semibold text-neutral-700 transition-colors duration-150 hover:bg-white hover:text-neutral-900"
        >
          <svg viewBox="0 0 24 24" className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
            <circle cx="12" cy="12" r="9" />
            <path d="M12 8v4M12 16h.01" strokeLinecap="round" />
          </svg>
          Aviso Legal y Política de Cumplimiento
        </button>
      </motion.footer>

      <LegalNoticeModal abierto={avisoLegal} onClose={() => setAvisoLegal(false)} />
    </motion.div>
  )
}
