import { useState } from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"
import AnalysisDashboard from "./AnalysisDashboard"
import PdfUploadZone from "./PdfUploadZone"
import RightSidebar from "./RightSidebar"

const spring = { type: "spring", bounce: 0, duration: 0.35 }

const VIABILITY = {
  viable: {
    label: "VIABLE",
    cls: "border-emerald-500/25 bg-emerald-400/10 text-emerald-700",
    dot: "bg-emerald-500",
  },
  revision: {
    label: "REVISIÓN",
    cls: "border-amber-500/25 bg-amber-400/10 text-amber-700",
    dot: "bg-amber-400",
  },
  critica: {
    label: "ALERTA",
    cls: "border-rose-500/25 bg-rose-500/10 text-rose-600",
    dot: "bg-rose-500",
  },
}

function viabilidadKey(valor) {
  const v = (valor || "").toLowerCase().replace(/[\s-]+/g, "")
  if (v.includes("requiere") || v === "requiererevision") return "revision"
  if (v.includes("critic") || v === "alertacritica") return "critica"
  return "viable"
}

function fechaTexto(valor) {
  if (!valor) return "—"
  const f = new Date(valor)
  return Number.isNaN(f.getTime()) ? "—" : f.toLocaleDateString("es-CO")
}

export default function MainAppView({
  modo = "workspace",
  usuario = null,
  analisis = null,
  preAnalisisData = null,
  historial = [],
  onAnalyze = () => {},
  onReabrir = () => {},
  onNuevoEstudio = () => {},
  onDownload = () => {},
  onLogout = () => {},
}) {
  const reduce = useReducedMotion()
  const [panelAbierto, setPanelAbierto] = useState(false)

  const nombre = usuario?.nombre || "Analista Demo"
  const empresa = usuario?.empresa || "Empresa Demo S.A.S."
  const usados = usuario?.creditosUsados ?? 1
  const total = usuario?.creditosTotal ?? 50
  const restantes = total - usados
  const fmiActual = analisis?.MatriculaFMI || analisis?.Resultado?.MatriculaFMI || ""

  return (
    <div className="w-full">
      <div className="mx-auto w-full max-w-[1700px] px-6 py-4">
        <AnimatePresence mode="wait" initial={false}>
          {modo === "dashboard" && (
            <motion.div
              key="toolbar-dashboard"
              initial={reduce ? { opacity: 0 } : { opacity: 0, y: -8 }}
              animate={reduce ? { opacity: 1 } : { opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -8 }}
              transition={spring}
              className="mb-5 flex flex-wrap items-center gap-3"
            >
              <motion.button
                type="button"
                onClick={onNuevoEstudio}
                whileTap={{ scale: 0.97 }}
                transition={spring}
                className="flex items-center gap-1.5 rounded-full border border-black/10 bg-white/70 px-3.5 py-2 text-xs font-semibold text-neutral-700 backdrop-blur-xl transition-colors duration-150 hover:bg-white hover:text-black"
              >
                <svg viewBox="0 0 24 24" className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
                  <path d="M19 12H5m6-6-6 6 6 6" />
                </svg>
                Cargar otro documento / Volver al Workspace
              </motion.button>
              {fmiActual && (
                <span className="rounded-full border border-black/10 bg-white/50 px-3 py-1.5 font-mono text-[10px] uppercase tracking-widest text-neutral-500 backdrop-blur-xl">
                  Estudio abierto · FMI {fmiActual}
                </span>
              )}
            </motion.div>
          )}
        </AnimatePresence>

        <div className="grid grid-cols-1 items-start gap-6 lg:grid-cols-12">
          <div className="min-w-0 lg:col-span-8 xl:col-span-9">
            <AnimatePresence mode="wait" initial={false}>
              {modo === "workspace" ? (
                <motion.div
                  key="workspace"
                  initial={reduce ? { opacity: 0 } : { opacity: 0, y: 12 }}
                  animate={reduce ? { opacity: 1 } : { opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -12 }}
                  transition={spring}
                  className="flex flex-col gap-6"
                >
                  <motion.section
                    initial={reduce ? { opacity: 0 } : { opacity: 0, y: 12 }}
                    animate={reduce ? { opacity: 1 } : { opacity: 1, y: 0 }}
                    transition={{ ...spring, delay: 0.05 }}
                    className="rounded-3xl border border-black/10 bg-white/70 p-6 shadow-sm backdrop-blur-2xl md:p-8"
                  >
                    <div className="flex flex-col gap-6 md:flex-row md:items-end md:justify-between">
                      <div className="min-w-0">
                        <p className="font-mono text-[10px] font-bold uppercase tracking-[0.25em] text-neutral-400">
                          Panel de Gestión Predial · B2B
                        </p>
                        <h1 className="mt-2 text-2xl font-black tracking-tighter text-black md:text-4xl">
                          Bienvenido,{" "}
                          <span className="bg-gradient-to-r from-neutral-900 to-neutral-500 bg-clip-text text-transparent">
                            {nombre}
                          </span>{" "}
                          <span aria-hidden className="text-neutral-300">|</span> Panel de Gestión Predial
                        </h1>
                        <p className="mt-2 max-w-xl text-sm text-neutral-600 md:text-[15px]">
                          Analiza Certificados de Tradición (CTL), Escrituras y VUR. Procesa folios de la{" "}
                          <span className="font-semibold text-black">{empresa}</span>, revisa su
                          viabilidad jurídica y descarga el dictamen en Word.
                        </p>
                      </div>
                      <div className="grid grid-cols-2 gap-2 md:gap-3">
                        <StatChip etiqueta="Créditos" valor={`${restantes} / ${total}`} />
                        <StatChip etiqueta="Plan" valor={usuario?.rol || "Empresarial"} />
                      </div>
                    </div>
                  </motion.section>

                  <motion.div
                    initial={reduce ? { opacity: 0 } : { opacity: 0, y: 12 }}
                    animate={reduce ? { opacity: 1 } : { opacity: 1, y: 0 }}
                    transition={{ ...spring, delay: 0.12 }}
                  >
                    <PdfUploadZone onAnalyze={onAnalyze} />
                  </motion.div>

                  <motion.section
                    initial={reduce ? { opacity: 0 } : { opacity: 0, y: 12 }}
                    animate={reduce ? { opacity: 1 } : { opacity: 1, y: 0 }}
                    transition={{ ...spring, delay: 0.18 }}
                    className="rounded-3xl border border-black/10 bg-white/70 p-6 shadow-sm backdrop-blur-2xl md:p-8"
                  >
                    <div className="flex flex-wrap items-end justify-between gap-3">
                      <div>
                        <p className="font-mono text-[10px] font-bold uppercase tracking-[0.25em] text-neutral-400">
                          Ficha de Actividad Reciente
                        </p>
                        <h2 className="mt-1.5 text-xl font-black tracking-tight text-black">
                          Historial de estudios
                        </h2>
                      </div>
                      <span className="rounded-full border border-black/10 bg-white/60 px-3 py-1 font-mono text-[10px] font-bold uppercase tracking-widest text-neutral-500">
                        {historial.length} analizados
                      </span>
                    </div>

                    {historial.length === 0 ? (
                      <div className="mt-5 rounded-2xl border border-black/10 bg-white/60 p-6 text-center backdrop-blur-xl">
                        <p className="font-mono text-2xl font-black text-black/10">DOC</p>
                        <p className="mt-2 text-sm font-semibold text-neutral-700">
                          Aún no hay estudios analizados
                        </p>
                        <p className="mt-1 text-xs text-neutral-500">
                          Sube tu primer documento (CTL, VUR o Escritura) para verlo aquí y
                          reabrirlo con un clic.
                        </p>
                      </div>
                    ) : (
                      <ul className="mt-5 space-y-2.5">
                        {historial.slice(0, 5).map((h, i) => {
                          const fmi = h.MatriculaFMI || h.Resultado?.MatriculaFMI || "—"
                          const predio = h.NombrePredio || h.Resultado?.NombrePredio || "Predio sin identificar"
                          const vk = viabilidadKey(h.Viabilidad || h.Resultado?.Viabilidad)
                          const v = VIABILITY[vk] ?? VIABILITY.viable
                          return (
                            <li key={h.Id}>
                              <motion.button
                                type="button"
                                onClick={() => onReabrir(h.Id)}
                                whileHover={reduce ? undefined : { x: 4 }}
                                whileTap={{ scale: 0.995 }}
                                transition={spring}
                                className="flex w-full items-center gap-4 rounded-2xl border border-black/10 bg-white/60 p-4 text-left backdrop-blur-xl transition-colors duration-150 hover:bg-white"
                              >
                                <span className="hidden h-10 w-10 shrink-0 items-center justify-center rounded-2xl bg-black/5 font-mono text-[10px] font-bold text-neutral-600 sm:flex">
                                  {String(i + 1).padStart(2, "0")}
                                </span>
                                <span className="min-w-0 flex-1 leading-tight">
                                  <span className="block truncate text-sm font-semibold text-black">{predio}</span>
                                  <span className="mt-0.5 block font-mono text-[10px] uppercase tracking-widest text-neutral-400">
                                    FMI {fmi} · {fechaTexto(h.FechaAnalisis)}
                                  </span>
                                </span>
                                <span
                                  className={`inline-flex shrink-0 items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[10px] font-bold tracking-widest ${v.cls}`}
                                >
                                  <span className={`h-1.5 w-1.5 rounded-full ${v.dot}`} />
                                  {v.label}
                                </span>
                              </motion.button>
                            </li>
                          )
                        })}
                      </ul>
                    )}
                  </motion.section>
                </motion.div>
              ) : (
                <motion.div
                  key="dashboard"
                  initial={reduce ? { opacity: 0 } : { opacity: 0, y: 12 }}
                  animate={reduce ? { opacity: 1 } : { opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -12 }}
                  transition={spring}
                >
                  <AnalysisDashboard analisis={analisis} preAnalisisData={preAnalisisData} onDownload={onDownload} />
                </motion.div>
              )}
            </AnimatePresence>
          </div>

          <aside className="hidden min-w-0 lg:col-span-4 lg:block xl:col-span-3">
            <RightSidebar
              modo={modo}
              analisis={analisis}
              preAnalisisData={preAnalisisData}
              usuario={usuario}
              onDownload={onDownload}
              onLogout={onLogout}
            />
          </aside>
        </div>
      </div>

      <button
        type="button"
        onClick={() => setPanelAbierto((v) => !v)}
        className="fixed bottom-5 right-5 z-50 flex items-center gap-2 rounded-full border border-black/10 bg-white/80 px-4 py-2.5 text-xs font-semibold text-black shadow-xl backdrop-blur-3xl transition-colors duration-150 hover:bg-white lg:hidden"
        aria-expanded={panelAbierto}
        aria-controls="panel-control-movil"
      >
        <svg
          viewBox="0 0 24 24"
          className="h-3.5 w-3.5"
          fill="none"
          stroke="currentColor"
          strokeWidth={2}
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <rect x="4" y="4" width="16" height="16" rx="3" />
          <path d="M9 9h6M9 13h6M9 17h4" />
        </svg>
        {panelAbierto ? "Cerrar Panel" : "Panel de Control"}
      </button>

      <AnimatePresence>
        {panelAbierto && (
          <motion.div
            id="panel-control-movil"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.2 }}
            className="fixed inset-0 z-40 flex items-end justify-center bg-black/20 backdrop-blur-sm lg:hidden"
            onClick={(e) => {
              if (e.target === e.currentTarget) setPanelAbierto(false)
            }}
          >
            <motion.div
              initial={reduce ? { opacity: 0 } : { opacity: 0, y: 40 }}
              animate={reduce ? { opacity: 1 } : { opacity: 1, y: 0 }}
              exit={reduce ? { opacity: 0 } : { opacity: 0, y: 40 }}
              transition={{ type: "spring", bounce: 0, duration: 0.4 }}
              className="max-h-[86vh] w-full max-w-lg overflow-y-auto rounded-t-3xl border-t border-black/10 bg-white/90 p-6 pb-8 shadow-2xl backdrop-blur-3xl"
              role="dialog"
              aria-label="Panel de control"
            >
              <div className="mx-auto mb-4 h-1 w-10 rounded-full bg-black/10" aria-hidden />
              <RightSidebar
                modo={modo}
                analisis={analisis}
                preAnalisisData={preAnalisisData}
                usuario={usuario}
                onDownload={onDownload}
                onLogout={onLogout}
                movil
              />
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}

function StatChip({ etiqueta, valor, dot = "" }) {
  return (
    <div className="flex min-w-[4.5rem] flex-col gap-1 rounded-2xl border border-black/10 bg-white/60 px-3 py-2.5 backdrop-blur-xl">
      <span className="font-mono text-[9px] font-bold uppercase tracking-[0.2em] text-neutral-400">
        {etiqueta}
      </span>
      <span className="flex items-center gap-1.5 font-mono text-xs font-bold text-black">
        {dot && <span className={`h-1.5 w-1.5 rounded-full ${dot}`} />}
        {valor}
      </span>
    </div>
  )
}