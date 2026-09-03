import { motion, useReducedMotion } from "framer-motion"

const spring = { type: "spring", stiffness: 380, damping: 30 }

/**
 * Notificación emergente (Toast) mostrada en la parte superior derecha cuando
 * el análisis predial que corría en segundo plano se completa exitosamente.
 */
export default function CompletionToast({ fmi, onVerDictamen = () => {}, onClose = () => {} }) {
  const reduce = useReducedMotion()

  const handleVerDictamen = () => {
    onVerDictamen()
    onClose()
  }

  return (
    <motion.div
      role="status"
      aria-live="polite"
      initial={reduce ? { opacity: 0 } : { opacity: 0, x: 40, scale: 0.96 }}
      animate={{ opacity: 1, x: 0, scale: 1 }}
      exit={reduce ? { opacity: 0 } : { opacity: 0, x: 40, scale: 0.96 }}
      transition={spring}
      className="fixed top-6 right-6 z-[70] w-[calc(100vw-3rem)] max-w-sm"
    >
      <div className="overflow-hidden rounded-3xl border border-black/10 bg-white/90 shadow-2xl backdrop-blur-3xl">
        <div className="flex items-start gap-3 p-5">
          <span className="mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-2xl bg-emerald-500/15 text-emerald-600">
            <svg viewBox="0 0 24 24" className="h-5 w-5" fill="none" stroke="currentColor" strokeWidth={2.5} strokeLinecap="round" strokeLinejoin="round">
              <path d="m5 13 4 4L19 7" />
            </svg>
          </span>

          <div className="min-w-0 flex-1">
            <p className="text-sm font-black tracking-tight text-black">
              ¡Dictamen Jurídico Completado!
            </p>
            <p className="mt-1 text-xs leading-relaxed text-neutral-600">
              El análisis predial del FMI{" "}
              <span className="font-mono font-bold text-neutral-800">{fmi || "—"}</span> ha
              finalizado con éxito.
            </p>

            <button
              type="button"
              onClick={handleVerDictamen}
              className="mt-3 inline-flex items-center gap-1.5 rounded-full bg-black px-3.5 py-2 text-xs font-bold text-white transition-colors duration-150 hover:bg-neutral-800"
            >
              Ver Dictamen
              <svg viewBox="0 0 24 24" className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth={2.5} strokeLinecap="round" strokeLinejoin="round">
                <path d="M5 12h14m-6-6 6 6-6 6" />
              </svg>
            </button>
          </div>

          <button
            type="button"
            onClick={onClose}
            aria-label="Cerrar notificación"
            className="shrink-0 rounded-full p-1.5 text-neutral-400 transition-colors duration-150 hover:bg-neutral-100 hover:text-neutral-600"
          >
            <svg viewBox="0 0 24 24" className="h-4 w-4" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round">
              <path d="M18 6 6 18M6 6l12 12" />
            </svg>
          </button>
        </div>
      </div>
    </motion.div>
  )
}
