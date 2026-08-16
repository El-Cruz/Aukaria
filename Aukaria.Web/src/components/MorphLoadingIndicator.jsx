import { AnimatePresence, motion, useReducedMotion } from "framer-motion"

const RING_R = 29
const RING_C = 2 * Math.PI * RING_R

const glowPulse = { duration: 2.6, repeat: Infinity, ease: "easeInOut" }
const sweepSpin = { duration: 3, repeat: Infinity, ease: "linear" }
const orbitSpin = { duration: 6, repeat: Infinity, ease: "linear" }
const iconEnter = { type: "spring", stiffness: 300, damping: 20 }
const iconExit = { duration: 0.25, ease: [0.23, 1, 0.32, 1] }
const ringSpring = { type: "spring", stiffness: 45, damping: 15, restDelta: 0.5 }
const scanSweep = { duration: 1.8, repeat: Infinity, ease: "easeInOut" }
const needleSpin = { duration: 3.5, repeat: Infinity, ease: "linear" }
const balanceSwing = { duration: 1.6, repeat: Infinity, repeatType: "mirror", ease: "easeInOut" }
const sealDraw = { duration: 0.7, ease: [0.23, 1, 0.32, 1] }

function IconoLectura({ reduce }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.5}
      strokeLinecap="round"
      strokeLinejoin="round"
      className="h-9 w-9 text-black dark:text-white"
      aria-hidden
    >
      <path d="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z" />
      <path d="M14 3v5h5" />
      <motion.g
        animate={reduce ? { opacity: 0.45 } : { y: [0, 6, 0], opacity: [0.9, 1, 0.9] }}
        transition={reduce ? { duration: 0.2 } : scanSweep}
      >
        <rect x="8.75" y="10.25" width="6.5" height="1.5" rx="0.75" fill="currentColor" />
      </motion.g>
    </svg>
  )
}

function IconoRadar({ reduce }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.5}
      className="h-9 w-9 text-black dark:text-white"
      aria-hidden
    >
      <circle cx="12" cy="12" r="9" />
      <circle cx="12" cy="12" r="5.5" strokeWidth={1} opacity={0.45} />
      <motion.g
        style={{ transformOrigin: "12px 12px", transformBox: "view-box" }}
        animate={reduce ? { rotate: 0 } : { rotate: 360 }}
        transition={reduce ? { duration: 0.2 } : needleSpin}
      >
        <path d="M12 4.5L12 19.5" strokeWidth={2} strokeLinecap="round" />
        <circle cx="12" cy="12" r="1.4" fill="currentColor" />
      </motion.g>
    </svg>
  )
}

function IconoBalanza({ reduce }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.5}
      strokeLinecap="round"
      strokeLinejoin="round"
      className="h-9 w-9 text-black dark:text-white"
      aria-hidden
    >
      <path d="M12 4.5V7" />
      <motion.g
        style={{ transformOrigin: "12px 7px", transformBox: "view-box" }}
        animate={reduce ? { rotate: 0 } : { rotate: [-4, 4] }}
        transition={reduce ? { duration: 0.2 } : balanceSwing}
      >
        <path d="M6.5 7h11" />
        <path d="M4 7v7.5a2 2 0 0 0 2 2" />
        <path d="M20 7v7.5a2 2 0 0 1-2 2" />
        <path d="M6 16.5l-2 3" />
        <path d="M6 16.5l2 3" />
        <path d="M18 16.5l-2 3" />
        <path d="M18 16.5l2 3" />
      </motion.g>
    </svg>
  )
}

function IconoSello({ reduce }) {
  const t = reduce ? { duration: 0.2 } : sealDraw
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      className="h-9 w-9 text-black dark:text-white"
      aria-hidden
    >
      <motion.circle
        cx="12"
        cy="12"
        r="9"
        initial={{ pathLength: 0 }}
        animate={{ pathLength: 1 }}
        transition={t}
      />
      <motion.path
        d="m8 12.5 2.5 2.5L16 9.5"
        initial={{ pathLength: 0 }}
        animate={{ pathLength: 1 }}
        transition={{ ...t, delay: reduce ? 0 : 0.3 }}
      />
    </svg>
  )
}

const ICONOS = [IconoLectura, IconoRadar, IconoBalanza, IconoSello]

export default function MorphLoadingIndicator({ stageIndex = 0, progress = 0 }) {
  const reduce = useReducedMotion()
  const idx = Math.max(0, Math.min(stageIndex, ICONOS.length - 1))
  const Icono = ICONOS[idx]
  const dashOffset = RING_C * (1 - Math.min(Math.max(progress, 0), 100) / 100)

  return (
    <div className="relative mx-auto flex h-32 w-32 items-center justify-center will-change-transform">
      {!reduce && (
        <motion.span
          aria-hidden
          animate={{ scale: [0.95, 1.1, 0.95], opacity: [0.15, 0.35, 0.15] }}
          transition={glowPulse}
          className="absolute inset-1 rounded-full bg-black/40 blur-2xl dark:bg-white/30"
        />
      )}

      {!reduce && (
        <motion.span
          aria-hidden
          animate={{ rotate: 360 }}
          transition={sweepSpin}
          className="absolute inset-0 rounded-full"
          style={{
            background:
              "conic-gradient(from 0deg, transparent 0deg, rgba(0,0,0,0.10) 35deg, transparent 70deg)",
            willChange: "transform",
          }}
        />
      )}

      <svg viewBox="0 0 64 64" className="absolute inset-0 h-full w-full -rotate-90" aria-hidden>
        <circle
          cx="32"
          cy="32"
          r={RING_R}
          fill="none"
          strokeWidth={2.5}
          className="stroke-black/5 dark:stroke-white/10"
        />
        <motion.circle
          cx="32"
          cy="32"
          r={RING_R}
          fill="none"
          strokeWidth={2.5}
          strokeLinecap="round"
          strokeDasharray={RING_C}
          initial={false}
          animate={{ strokeDashoffset: dashOffset }}
          transition={reduce ? { duration: 0.3 } : ringSpring}
          className="text-black dark:text-white"
        />
      </svg>

      {!reduce && (
        <motion.span aria-hidden animate={{ rotate: 360 }} transition={orbitSpin} className="absolute inset-0 will-change-transform">
          <span className="absolute left-1/2 top-0 h-1.5 w-1.5 -translate-x-1/2 -translate-y-1/2 rounded-full bg-black shadow-sm dark:bg-white" />
        </motion.span>
      )}

      <div className="relative flex h-20 w-20 items-center justify-center rounded-full border border-black/10 bg-white/80 shadow-inner backdrop-blur-md dark:border-white/10 dark:bg-neutral-900/80">
        <AnimatePresence mode="wait" initial={false}>
          <motion.div
            key={idx}
            initial={reduce ? { opacity: 0 } : { scale: 0.6, rotate: 25, opacity: 0, filter: "blur(4px)" }}
            animate={
              reduce
                ? { opacity: 1 }
                : {
                    scale: [0.6, 1.08, 1],
                    rotate: [25, 0],
                    opacity: [0, 1, 1],
                    filter: ["blur(4px)", "blur(0px)", "blur(0px)"],
                  }
            }
            exit={
              reduce
                ? { opacity: 0, transition: { duration: 0.15 } }
                : { scale: 0.5, rotate: -25, opacity: 0, filter: "blur(4px)", transition: iconExit }
            }
            transition={reduce ? { duration: 0.15 } : iconEnter}
            className="flex items-center justify-center will-change-transform"
          >
            <Icono reduce={reduce} />
          </motion.div>
        </AnimatePresence>
      </div>
    </div>
  )
}