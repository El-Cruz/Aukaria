import { motion, useReducedMotion } from "framer-motion"

/**
 * Minimal Mono — Estudio Predial real aplicado a 3 variantes.
 * Modo "compare": las 3 juntas en grid. Modo "full": una variante única.
 * Animaciones: springs (stiffness 400, damping 25) en botones y tarjetas.
 */

const spring = { type: "spring", stiffness: 400, damping: 25 }

const STUDY = {
  fmi: "196-2053",
  predio: "Finca La Esperanza",
  estado: "Viable · Sin Embargos",
  alertas: "0 Cautelares",
  action: "Descargar Reporte .docx",
}

function CardHeader({ eyebrow }) {
  return (
    <div>
      <p className="text-[10px] font-black uppercase tracking-[0.2em] text-neutral-400">{eyebrow}</p>
      <h3 className="mt-2 text-xl font-black tracking-tighter text-black md:text-2xl">
        {STUDY.predio}
      </h3>
      <p className="mt-1 font-mono text-sm text-neutral-600">FMI {STUDY.fmi}</p>
    </div>
  )
}

function StatusRow({ chip }) {
  return (
    <div className="flex flex-wrap items-center gap-2.5 text-[13px]">
      <span className="flex items-center gap-2">
        <span className="h-2 w-2 rounded-full bg-emerald-500" />
        <span className="font-semibold text-neutral-800">{STUDY.estado}</span>
      </span>
      <span className={`rounded-full px-2.5 py-0.5 text-xs font-bold ${chip}`}>
        {STUDY.alertas}
      </span>
    </div>
  )
}

function VariantA({ index = 0 }) {
  const reduce = useReducedMotion()
  return (
    <motion.article
      initial={reduce ? { opacity: 0 } : { opacity: 0, y: 24 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ ...spring, delay: index * 0.08 }}
      whileHover={{ y: -3 }}
      className="flex flex-col gap-5 rounded-none border-2 border-black bg-white p-6 shadow-[6px_6px_0px_0px_rgba(0,0,0,1)]"
    >
      <CardHeader eyebrow="Estudio Predial · Neo-Brutalist" />
      <StatusRow chip="border border-black bg-white text-black" />
      <motion.button
        whileHover={{ x: 2, y: 2 }}
        whileTap={{ x: 3, y: 3 }}
        transition={spring}
        className="mt-auto self-start rounded-none border-2 border-black bg-black px-5 py-2.5 text-sm font-semibold text-white shadow-[3px_3px_0px_0px_rgba(0,0,0,1)]"
      >
        {STUDY.action}
      </motion.button>
    </motion.article>
  )
}

function VariantB({ index = 0 }) {
  const reduce = useReducedMotion()
  return (
    <motion.article
      initial={reduce ? { opacity: 0 } : { opacity: 0, y: 24 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ ...spring, delay: index * 0.08 }}
      whileHover={{ y: -2 }}
      className="flex flex-col gap-5 rounded-3xl border border-black/10 bg-white/70 p-6 shadow-[0_12px_32px_rgba(0,0,0,0.06)] backdrop-blur-2xl"
    >
      <CardHeader eyebrow="Estudio Predial · Frost Mono" />
      <StatusRow chip="bg-black/5 text-neutral-600" />
      <motion.button
        whileHover={{ scale: 1.03 }}
        whileTap={{ scale: 0.97 }}
        transition={spring}
        className="mt-auto self-start rounded-full bg-black/5 px-6 py-2.5 text-sm font-medium text-black transition-colors duration-150 hover:bg-black hover:text-white"
      >
        {STUDY.action}
      </motion.button>
    </motion.article>
  )
}

function VariantC({ index = 0 }) {
  const reduce = useReducedMotion()
  return (
    <motion.article
      initial={reduce ? { opacity: 0 } : { opacity: 0, y: 24 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ ...spring, delay: index * 0.08 }}
      whileHover={{ y: -2 }}
      className="flex flex-col gap-5 rounded-2xl bg-white p-6 shadow-2xl shadow-black/10"
    >
      <CardHeader eyebrow="Estudio Predial · Elevated Ink" />
      <StatusRow chip="bg-neutral-100 text-neutral-600" />
      <motion.button
        whileHover={{ y: -2 }}
        whileTap={{ scale: 0.97 }}
        transition={spring}
        className="mt-auto self-start rounded-lg bg-black px-6 py-2.5 text-sm font-semibold text-white shadow-[0_4px_12px_rgba(0,0,0,0.2)]"
      >
        {STUDY.action}
      </motion.button>
    </motion.article>
  )
}

export default function VariantPreview({ tab }) {
  const full = tab !== "compare"

  if (full) {
    return (
      <div className="mx-auto w-full max-w-lg pt-10">
        <motion.div key={tab} initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={spring}>
          {tab === "A" && <VariantA />}
          {tab === "B" && <VariantB />}
          {tab === "C" && <VariantC />}
        </motion.div>

        <p className="mt-6 text-center text-xs font-semibold uppercase tracking-[0.2em] text-neutral-400">
          Vista completa — Variante {tab}
        </p>
      </div>
    )
  }

  return (
    <div className="grid grid-cols-1 gap-6 pt-10 md:grid-cols-3 md:gap-8">
      <VariantA index={0} />
      <VariantB index={1} />
      <VariantC index={2} />
    </div>
  )
}