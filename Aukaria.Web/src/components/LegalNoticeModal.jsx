import { AnimatePresence, motion } from "framer-motion"

const spring = { type: "spring", bounce: 0, duration: 0.35 }

export default function LegalNoticeModal({ abierto = false, onClose = () => {} }) {
  return (
    <AnimatePresence>
      {abierto && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          transition={{ duration: 0.2 }}
          onClick={onClose}
          className="fixed inset-0 z-[80] flex items-center justify-center bg-black/30 p-4 backdrop-blur-sm"
          role="dialog"
          aria-modal="true"
          aria-label="Avisos legales y de cumplimiento"
        >
          <motion.div
            initial={{ opacity: 0, y: 24, scale: 0.98 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 24, scale: 0.98 }}
            transition={spring}
            onClick={(e) => e.stopPropagation()}
            className="max-h-[86vh] w-full max-w-lg overflow-y-auto rounded-3xl border border-neutral-200 bg-white/90 p-6 shadow-2xl backdrop-blur-3xl md:p-8"
          >
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="font-mono text-[10px] font-bold uppercase tracking-[0.25em] text-[var(--link-hover)]">
                  Aukaria Legal
                </p>
                <h2 className="mt-1.5 text-xl font-black tracking-tight text-neutral-900">
                  Aviso Legal y Política de Cumplimiento
                </h2>
              </div>
              <button
                type="button"
                onClick={onClose}
                aria-label="Cerrar aviso legal"
                className="shrink-0 rounded-full p-1.5 text-neutral-400 transition-colors duration-150 hover:bg-neutral-100 hover:text-neutral-700"
              >
                <svg viewBox="0 0 24 24" className="h-4 w-4" fill="none" stroke="currentColor" strokeWidth={2}>
                  <path d="M18 6 6 18M6 6l12 12" strokeLinecap="round" />
                </svg>
              </button>
            </div>

            <div className="mt-6 space-y-4">
              <section className="rounded-2xl border border-amber-500/20 bg-amber-500/5 p-4">
                <div className="flex items-center gap-2">
                  <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-amber-500/15 text-amber-700">
                    <svg viewBox="0 0 24 24" className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth={2}>
                      <circle cx="12" cy="12" r="9" />
                      <path d="M12 8v4M12 16h.01" strokeLinecap="round" />
                    </svg>
                  </span>
                  <h3 className="text-sm font-bold uppercase tracking-widest text-amber-800">
                    Cláusula de Exclusión de Responsabilidad
                  </h3>
                </div>
                <p className="mt-2.5 text-[13px] leading-relaxed text-neutral-700">
                  Aviso Legal y Alcance: El diagnóstico emitido por Aukaria se limita al análisis
                  jurídico, registral y catastral de los documentos suministrados. La plataforma no
                  efectúa consultas en listas restrictivas (OFAC, listas cautelares internacionales)
                  ni bases de datos de antecedentes penales o disciplinarios. La debida diligencia
                  sobre las partes intervinientes es responsabilidad exclusiva de los contratantes en
                  la etapa de negociación directa.
                </p>
              </section>

              <section className="rounded-2xl border border-emerald-500/20 bg-emerald-500/5 p-4">
                <div className="flex items-center gap-2">
                  <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-emerald-500/15 text-emerald-700">
                    <svg viewBox="0 0 24 24" className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth={2}>
                      <path d="M12 22c4-3 6-6 6-10a6 6 0 1 0-12 0c0 4 2 7 6 10zM12 9v4M10 11h4" strokeLinecap="round" strokeLinejoin="round" />
                    </svg>
                  </span>
                  <h3 className="text-sm font-bold uppercase tracking-widest text-emerald-800">
                    Advertencia Ambiental (RUNAP)
                  </h3>
                </div>
                <p className="mt-2.5 text-[13px] leading-relaxed text-neutral-700">
                  Verificación RUNAP: Es imperativo contrastar la ubicación del polígono predial con
                  la cartografía oficial del RUNAP para constatar la inexistencia de traslapes con
                  áreas protegidas del Sistema Nacional de Áreas Protegidas (SINAP).
                </p>
              </section>
            </div>

            <button
              type="button"
              onClick={onClose}
              className="mt-6 w-full rounded-full bg-[var(--cta)] px-5 py-3 text-sm font-semibold text-white transition-colors duration-150 hover:bg-[var(--cta-hover)]"
            >
              Entendido
            </button>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  )
}
