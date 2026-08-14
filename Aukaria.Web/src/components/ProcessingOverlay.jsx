import { useEffect, useRef, useState } from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"

const spring = { type: "spring", stiffness: 320, damping: 30 }
const progressSpring = { type: "spring", stiffness: 24, damping: 20, restDelta: 0.5 }
const fade = { duration: 0.2 }

const STEP_MS = 1100
const TOTAL_MS = 3900

const STEPS = [
  {
    label: "Lectura y extracción de texto del CTL",
    msg: "Extrayendo texto del CTL y detectando el FMI…",
  },
  {
    label: "Análisis con IA & Derecho Inmobiliario",
    msg: "Consultando Claude API · derecho inmobiliario…",
  },
  {
    label: "Evaluación de gravámenes y alertas",
    msg: "Cruzándo anotaciones contra gravámenes y embargos…",
  },
  {
    label: "Generación del estudio predial",
    msg: "Generando diagnóstico y estructura traditiva…",
  },
]

const DONE_MSG = "Estudio predial generado correctamente"

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

function StepIndicator({ state, reduce }) {
  const t = reduce ? { duration: 0.15 } : spring
  return (
    <span className="relative flex h-6 w-6 shrink-0 items-center justify-center">
      <AnimatePresence initial={false} mode="popLayout">
        {state === "done" && (
          <motion.span
            key="done"
            initial={{ scale: 0.5, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            exit={{ scale: 0.5, opacity: 0 }}
            transition={t}
            className="flex h-6 w-6 items-center justify-center rounded-full bg-black text-white"
          >
            <svg
              viewBox="0 0 24 24"
              className="h-3.5 w-3.5"
              fill="none"
              stroke="currentColor"
              strokeWidth={3}
            >
              <path d="m5 13 4 4L19 7" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
          </motion.span>
        )}
        {state === "active" && (
          <motion.span
            key="active"
            initial={{ scale: 0.5, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            exit={{ scale: 0.5, opacity: 0 }}
            transition={t}
            className="relative flex h-6 w-6 items-center justify-center"
          >
            <svg
              viewBox="0 0 24 24"
              className="h-6 w-6 animate-spin text-neutral-900"
            >
              <circle
                cx="12"
                cy="12"
                r="9"
                fill="none"
                stroke="currentColor"
                strokeWidth={2.5}
                strokeDasharray="30 30"
                strokeLinecap="round"
              />
            </svg>
          </motion.span>
        )}
        {state === "pending" && (
          <motion.span
            key="pending"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={t}
            className="h-2.5 w-2.5 rounded-full border-2 border-neutral-300"
          />
        )}
      </AnimatePresence>
    </span>
  )
}

function StepRow({ label, state, reduce }) {
  const status =
    state === "done" ? "Completado" : state === "active" ? "En proceso…" : "Pendiente"
  return (
    <li className="flex items-center gap-3">
      <StepIndicator state={state} reduce={reduce} />
      <span
        className={`min-w-0 flex-1 truncate text-[13px] font-medium ${
          state === "pending" ? "text-neutral-400" : "text-black"
        }`}
      >
        {label}
      </span>
      <span
        className={`shrink-0 font-mono text-[10px] font-bold uppercase tracking-widest ${
          state === "active"
            ? "text-black"
            : state === "done"
              ? "text-neutral-400"
              : "text-neutral-300"
        }`}
      >
        {status}
      </span>
    </li>
  )
}

export default function ProcessingOverlay({ onComplete, autoCompletar = true }) {
  const reduce = useReducedMotion()
  const [done, setDone] = useState(1)
  const [msgTick, setMsgTick] = useState(0)
  const onCompleteRef = useRef(onComplete)

  useEffect(() => {
    onCompleteRef.current = onComplete
  }, [onComplete])

  useEffect(() => {
    if (!autoCompletar) return undefined
    const timers = []
    for (let i = 1; i < STEPS.length; i += 1) {
      timers.push(setTimeout(() => setDone(i + 1), i * STEP_MS))
    }
    timers.push(setTimeout(() => onCompleteRef.current(), TOTAL_MS))
    return () => timers.forEach(clearTimeout)
  }, [autoCompletar])

  useEffect(() => {
    if (autoCompletar) return undefined
    const id = setInterval(() => setMsgTick((t) => t + 1), 1800)
    return () => clearInterval(id)
  }, [autoCompletar])

  const activeMsg = autoCompletar
    ? done < STEPS.length
      ? STEPS[done].msg
      : DONE_MSG
    : STEPS[1 + (msgTick % 3)].msg
  const progress = autoCompletar ? (done / STEPS.length) * 100 : 80

  return (
    <motion.div
      role="dialog"
      aria-modal="true"
      aria-label="Procesando análisis jurídico"
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={fade}
      className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/40 p-4 backdrop-blur-2xl"
    >
      <motion.div
        initial={reduce ? { opacity: 0 } : { opacity: 0, scale: 0.96, y: 10 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={reduce ? { opacity: 0 } : { opacity: 0, scale: 0.97 }}
        transition={spring}
        className="w-full max-w-md rounded-3xl border border-black/10 bg-white/85 p-8 shadow-2xl backdrop-blur-3xl"
      >
        <div className="mb-6 h-1 overflow-hidden rounded-full bg-black/5">
          <motion.div
            animate={{ width: `${progress}%` }}
            transition={reduce ? { duration: 0.3 } : progressSpring}
            className="h-full rounded-full bg-black"
          />
        </div>

        <p className="text-[10px] font-black uppercase tracking-[0.2em] text-neutral-400">
          Aukaria IA · Auditoría en tiempo real
        </p>
        <h3 className="mt-2 text-lg font-black tracking-tight text-black">
          Analizando el folio predial
        </h3>

        <div className="relative mx-auto mt-7 flex h-16 w-16 items-center justify-center">
          <motion.span
            animate={
              reduce
                ? { scale: 1, opacity: 0 }
                : { scale: [1, 1.1, 1], opacity: [0.3, 0.45, 0.3] }
            }
            transition={{ duration: 2, repeat: Infinity, ease: "easeInOut" }}
            className="absolute inset-0 rounded-3xl bg-black/5"
          />
          <motion.span
            animate={{ rotate: 360 }}
            transition={{ duration: 2.4, repeat: Infinity, ease: "linear" }}
            className="absolute inset-0 text-neutral-900"
          >
            <svg viewBox="0 0 64 64" className="h-full w-full">
              <circle
                cx="32"
                cy="32"
                r="29"
                fill="none"
                stroke="currentColor"
                strokeWidth={2}
                strokeDasharray="40 142"
                strokeLinecap="round"
              />
            </svg>
          </motion.span>
          <div className="relative flex h-14 w-14 items-center justify-center rounded-2xl bg-black shadow-[0_8px_24px_rgba(0,0,0,0.25)]">
            <PdfIcon className="h-7 w-7 text-white" />
          </div>
        </div>

        <div className="mt-5 flex h-8 items-center justify-center">
          <AnimatePresence mode="wait" initial={false}>
            <motion.p
              key={activeMsg}
              initial={{ opacity: 0, y: 4 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -4 }}
              transition={fade}
              className="text-center font-mono text-xs text-neutral-500"
            >
              {activeMsg}
            </motion.p>
          </AnimatePresence>
        </div>

        <ul className="mt-4 flex flex-col gap-3">
          {STEPS.map((s, i) => {
            const state = i < done ? "done" : i === done ? "active" : "pending"
            return <StepRow key={s.label} label={s.label} state={state} reduce={reduce} />
          })}
        </ul>
      </motion.div>
    </motion.div>
  )
}