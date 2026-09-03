import { motion, useReducedMotion } from "framer-motion"

const spring = { type: "spring", stiffness: 400, damping: 30 }

/**
 * Píldora flotante fija en la esquina inferior derecha que muestra que el
 * análisis predial sigue corriendo en segundo plano. Al hacer clic re-maximiza
 * el overlay de procesamiento.
 */
export default function FloatingAnalysisTracker({
  progreso = null,
  onMaximize = () => {},
}) {
  const reduce = useReducedMotion()
  const pct = progreso?.Porcentaje ? Math.min(progreso.Porcentaje, 100) : null
  const etiqueta = progreso?.Mensaje || "Analizando predio en segundo plano…"

  return (
    <motion.button
      type="button"
      onClick={onMaximize}
      role="status"
      aria-live="polite"
      aria-label="Análisis en segundo plano. Clic para volver a abrir."
      initial={reduce ? { opacity: 0 } : { opacity: 0, y: 16, scale: 0.96 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      exit={reduce ? { opacity: 0 } : { opacity: 0, y: 16, scale: 0.96 }}
      transition={spring}
      whileHover={reduce ? undefined : { scale: 1.02 }}
      whileTap={{ scale: 0.98 }}
      className="fixed bottom-6 right-6 z-50 flex max-w-[calc(100vw-3rem)] items-center gap-3 rounded-2xl border border-black/10 bg-white/80 px-4 py-3 text-left shadow-xl backdrop-blur-xl transition-colors duration-150 hover:bg-white"
    >
      <span className="relative flex h-3.5 w-3.5 shrink-0">
        <span
          className={`absolute inline-flex h-full w-full rounded-full bg-amber-400 ${reduce ? "" : "animate-ping"} opacity-75`}
        />
        <span className="relative inline-flex h-3.5 w-3.5 rounded-full bg-amber-500" />
      </span>

      <span className="min-w-0">
        <span className="block font-mono text-[10px] font-bold uppercase tracking-widest text-neutral-400">
          En segundo plano
        </span>
        <span className="mt-0.5 block truncate text-xs font-semibold text-neutral-800">
          {etiqueta}
          {pct !== null && <span className="ml-1.5 font-mono text-[10px] font-bold text-neutral-400">{pct}%</span>}
        </span>
      </span>
    </motion.button>
  )
}
