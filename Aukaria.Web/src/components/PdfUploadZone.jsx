import { useRef, useState } from "react"
import {
  AnimatePresence,
  animate,
  motion,
  useMotionValue,
  useReducedMotion,
  useTransform,
} from "framer-motion"
import { preAnalizarCtl } from "../services/apiService"
import { identificadorPreAnalisis, resolvedorTipo } from "../utils/tiposDocumento"

const spring = { type: "spring", stiffness: 300, damping: 20 }
const fade = { duration: 0.2 }

function PdfIcon({ className = "" }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.5}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden
    >
      <path d="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z" />
      <path d="M14 3v5h5" />
      <path d="M9 13h6M9 17h6" />
    </svg>
  )
}

export default function PdfUploadZone({ onAnalyze = () => {} }) {
  const inputRef = useRef(null)
  const reduce = useReducedMotion()
  const errorRef = useRef(null)

  const [file, setFile] = useState(null)
  const [isDragging, setIsDragging] = useState(false)
  const [phase, setPhase] = useState("idle")
  const [pre, setPre] = useState(null)
  const [preError, setPreError] = useState("")

  const progress = useMotionValue(0)
  const width = useTransform(progress, (v) => `${v}%`)

  const acceptFile = (next) => {
    if (!next) return
    setFile(next)
    setPre(null)
    setPreError("")
    errorRef.current = null
    setPhase("reading")
    progress.set(0)

    let animDone = false
    animate(progress, 100, {
      duration: reduce ? 0.01 : 0.45,
      ease: reduce ? "linear" : [0.22, 0.61, 0.36, 1],
      onComplete: () => {
        animDone = true
        setPhase((p) => (p === "reading" ? (errorRef.current ? "error" : "ready") : p))
      },
    })

    preAnalizarCtl(next)
      .then((res) => {
        setPre({
          fmi: res.MatriculaFMI || "",
          analisisIdPrevio: res.AnalisisIdPrevio ?? null,
          clasificacion: res.ClasificacionDocumento ?? null,
        })
      })
      .catch((err) => {
        errorRef.current = err.message || "No se pudo conectar con el backend."
        setPreError(errorRef.current)
        if (animDone) setPhase("error")
      })
  }

  const resetFile = () => {
    errorRef.current = null
    setFile(null)
    setPre(null)
    setPreError("")
    setPhase("idle")
    progress.set(0)
  }

  const sizeMb = file ? (file.size / (1024 * 1024)).toFixed(2) : "0"
  const fmiOk = Boolean(pre?.fmi)
  const tipoDetectado = pre?.clasificacion ? resolvedorTipo(pre.clasificacion.TipoDocumento) : null
  const detalleIdentificador = pre?.clasificacion
    ? identificadorPreAnalisis(pre.clasificacion, pre.fmi)
    : ""
  const esCtl = tipoDetectado?.key === "CTL"

  return (
    <section aria-label="Subir Certificado de Tradición y Libertad">
      <motion.div
        role="button"
        tabIndex={0}
        aria-label="Zona de carga del Certificado de Tradición y Libertad"
        animate={isDragging ? { scale: 1.02 } : { scale: 1 }}
        transition={spring}
        onClick={() => !file && inputRef.current?.click()}
        onKeyDown={(e) => {
          if ((e.key === "Enter" || e.key === " ") && !file) inputRef.current?.click()
        }}
        onDragOver={(e) => {
          e.preventDefault()
          setIsDragging(true)
        }}
        onDragLeave={() => setIsDragging(false)}
        onDrop={(e) => {
          e.preventDefault()
          setIsDragging(false)
          acceptFile(e.dataTransfer.files?.[0])
        }}
        className={`relative overflow-hidden rounded-3xl p-8 text-center outline-none focus-visible:ring-2 focus-visible:ring-black transition-colors duration-150 ${
          isDragging ? "bg-white/90 backdrop-blur-3xl" : "bg-white/70 backdrop-blur-2xl"
        }`}
      >
        <span
          aria-hidden
          className={`pointer-events-none absolute inset-3 rounded-2xl border-2 border-dashed transition-colors duration-150 ${
            isDragging ? "border-black/40" : "border-black/10"
          }`}
        />
        <input
          ref={inputRef}
          type="file"
          accept="application/pdf,.pdf"
          className="hidden"
          onChange={(e) => {
            acceptFile(e.target.files?.[0])
            e.target.value = ""
          }}
        />

        <AnimatePresence mode="wait" initial={false}>
          {!file ? (
            <motion.div
              key="idle"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              transition={fade}
            >
              <div className="mx-auto flex h-[4.5rem] w-16 flex-col items-center justify-center gap-1.5 rounded-2xl bg-black/5">
                <PdfIcon className="h-8 w-8 text-neutral-700" />
                <span className="font-mono text-[10px] font-bold leading-none tracking-wide text-neutral-500">
                  PDF
                </span>
              </div>
              <h2 className="mx-auto mt-5 max-w-xs text-lg font-semibold tracking-tight text-black md:text-xl">
                Arrastra aquí el Certificado de Tradición y Libertad (CTL)
              </h2>
              <p className="mt-2 font-mono text-xs text-neutral-500">
                Soporta archivos PDF de la SNR hasta 20 MB
              </p>
              <motion.button
                type="button"
                whileTap={{ scale: 0.97 }}
                transition={spring}
                onClick={(e) => {
                  e.stopPropagation()
                  inputRef.current?.click()
                }}
                className="mt-6 rounded-full border border-black/15 bg-black/5 px-6 py-2.5 text-sm font-medium text-black hover:bg-black/10"
              >
                Seleccionar desde el equipo
              </motion.button>
            </motion.div>
          ) : (
            <motion.div
              key="file"
              initial={{ opacity: 0, y: 8 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -8 }}
              transition={spring}
              className="flex flex-col gap-6"
            >
              <div className="flex items-center gap-4 text-left">
                <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-black/5">
                  <PdfIcon className="h-6 w-6 text-neutral-700" />
                </div>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-semibold text-black">{file.name}</p>
                  <p className="mt-0.5 font-mono text-xs text-neutral-500">{sizeMb} MB</p>
                </div>
                <button
                  type="button"
                  onClick={resetFile}
                  className="shrink-0 font-mono text-xs font-medium text-neutral-500 hover:text-black"
                >
                  Cambiar PDF
                </button>
              </div>

              <AnimatePresence mode="wait" initial={false}>
                {phase === "reading" && (
                  <motion.div
                    key="reading"
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                    transition={fade}
                  >
                    <div className="flex items-center justify-between font-mono text-xs text-neutral-500">
                      <span>Pre-análisis rápido · leyendo el CTL…</span>
                      <span aria-hidden>&lt; 500 ms</span>
                    </div>
                    <div className="mt-2 h-1.5 w-full overflow-hidden rounded-full bg-black/5">
                      <motion.div style={{ width }} className="h-full rounded-full bg-black" />
                    </div>
                  </motion.div>
                )}

                {phase === "error" && (
                  <motion.div
                    key="error"
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                    transition={fade}
                  >
                    <div className="rounded-2xl border border-red-200 bg-red-50/80 p-4 text-left">
                      <p className="flex items-center gap-2 font-mono text-xs font-bold text-red-600">
                        <svg
                          viewBox="0 0 24 24"
                          className="h-4 w-4 shrink-0"
                          fill="none"
                          stroke="currentColor"
                          strokeWidth={2}
                        >
                          <circle cx="12" cy="12" r="9" />
                          <path d="M12 8v4M12 16h.01" strokeLinecap="round" />
                        </svg>
                        Error en el pre-análisis
                      </p>
                      <p className="mt-1.5 text-xs leading-relaxed text-red-700/80">{preError}</p>
                    </div>
                    <div className="mt-4 flex flex-wrap items-center justify-center gap-3">
                      <motion.button
                        type="button"
                        whileTap={{ scale: 0.97 }}
                        transition={spring}
                        onClick={() => acceptFile(file)}
                        className="rounded-full bg-black px-5 py-2.5 text-sm font-semibold text-white"
                      >
                        Reintentar
                      </motion.button>
                      <motion.button
                        type="button"
                        whileTap={{ scale: 0.97 }}
                        transition={spring}
                        onClick={resetFile}
                        className="rounded-full border border-black/10 px-5 py-2.5 text-sm font-medium text-neutral-600 hover:bg-black/5 hover:text-black"
                      >
                        Cambiar PDF
                      </motion.button>
                    </div>
                  </motion.div>
                )}

                {phase === "ready" && (
                  <motion.div
                    key="ready"
                    initial={{ opacity: 0, y: 6 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -6 }}
                    transition={spring}
                    className="flex flex-col gap-5"
                  >
                    {tipoDetectado && (
                      <motion.span
                        initial={{ opacity: 0, scale: 0.92 }}
                        animate={{ opacity: 1, scale: 1 }}
                        transition={spring}
                        className={`inline-flex w-fit items-center gap-2 rounded-full border px-3.5 py-1.5 font-mono text-xs font-bold tracking-wider ${tipoDetectado.badge}`}
                      >
                        <span aria-hidden>{tipoDetectado.emoji}</span>
                        <span className={`h-1.5 w-1.5 rounded-full ${tipoDetectado.dot}`} />
                        {tipoDetectado.detalle}
                        {detalleIdentificador && (
                          <span className="font-medium normal-case tracking-normal text-neutral-600">
                            | {detalleIdentificador}
                          </span>
                        )}
                      </motion.span>
                    )}
                    {esCtl && !fmiOk && tipoDetectado && (
                      <span className="inline-flex w-fit items-center gap-2 rounded-full border border-amber-500/25 bg-amber-400/10 px-3.5 py-1.5 font-mono text-[11px] text-amber-700">
                        <span className="h-1.5 w-1.5 rounded-full bg-amber-400" />
                        FMI NO DETECTADO
                      </span>
                    )}
                    {pre?.analisisIdPrevio && (
                      <span className="inline-flex w-fit items-center gap-2 rounded-full border border-black/10 bg-black/5 px-3.5 py-1.5 font-mono text-[11px] text-neutral-500">
                        Análisis previo registrado · {pre.analisisIdPrevio}
                      </span>
                    )}
                    {!fmiOk && (
                      <p className="text-left text-xs leading-relaxed text-neutral-500">
                        No fue posible extraer la matrícula automáticamente. El análisis continuará
                        con la lectura del documento completo.
                      </p>
                    )}

                    <div className="flex flex-wrap items-center justify-center gap-3">
                      <motion.button
                        type="button"
                        whileTap={{ scale: 0.97 }}
                        transition={spring}
                        onClick={resetFile}
                        className="rounded-full border border-black/10 px-5 py-2.5 text-sm font-medium text-neutral-600 hover:bg-black/5 hover:text-black"
                      >
                        Eliminar PDF
                      </motion.button>
                      <motion.button
                        type="button"
                        whileHover={{ scale: 1.03 }}
                        whileTap={{ scale: 0.97 }}
                        transition={spring}
                        onClick={() =>
                          onAnalyze(file, {
                            matriculaFmi: pre?.fmi || "",
                            analisisIdPrevio: pre?.analisisIdPrevio ?? null,
                            tipoDocumento: pre?.clasificacion?.TipoDocumento || "CTL",
                          })
                        }
                        className="rounded-full bg-black px-6 py-2.5 text-sm font-semibold text-white shadow-[0_4px_16px_rgba(0,0,0,0.2)] hover:bg-neutral-900"
                      >
                        Iniciar Análisis Jurídico con IA →
                      </motion.button>
                    </div>
                  </motion.div>
                )}
              </AnimatePresence>
            </motion.div>
          )}
        </AnimatePresence>
      </motion.div>
    </section>
  )
}