import { AnimatePresence, motion, useReducedMotion } from "framer-motion"

const SHAPES = [
  "M50 0 C50 27.6 27.6 50 0 50 C27.6 50 50 72.4 50 100 C50 72.4 72.4 50 100 50 C72.4 50 50 27.6 50 0 Z",
  "M50 5 C85 5 95 15 95 50 C95 85 85 95 50 95 C15 95 5 85 5 50 C5 15 15 5 50 5 Z",
  "M50 0 C77.6 0 100 22.4 100 50 C100 77.6 77.6 100 50 100 C22.4 100 0 77.6 0 50 C0 22.4 22.4 0 50 0 Z",
  "M50 10 C65 20 80 35 90 50 C80 65 65 80 50 90 C35 80 20 65 10 50 C20 35 35 20 50 10 Z",
]

const morphLoop = {
  duration: 4,
  repeat: Infinity,
  ease: [0.4, 0, 0.2, 1],
}
const iconEnter = { type: "spring", stiffness: 300, damping: 20 }
const iconExit = { duration: 0.2, ease: [0.23, 1, 0.32, 1] }
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
      className="h-9 w-9 text-white"
      aria-hidden
    >
      <path d="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z" />
      <path d="M14 3v5h5" />
      <motion.g
        animate={reduce ? { opacity: 0.6 } : { y: [0, 6, 0], opacity: [0.9, 1, 0.9] }}
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
      className="h-9 w-9 text-white"
      aria-hidden
    >
      <circle cx="12" cy="12" r="9" />
      <circle cx="12" cy="12" r="5.5" strokeWidth={1} opacity={0.5} />
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
      className="h-9 w-9 text-white"
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
      className="h-9 w-9 text-white"
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

export default function GoogleMorphLoader({ stageIndex = 0, className = "" }) {
  const reduce = useReducedMotion()
  const idx = Math.max(0, Math.min(stageIndex, ICONOS.length - 1))
  const Icono = ICONOS[idx]

  return (
    <div className={`relative flex items-center justify-center ${className}`}>
      <div className="relative h-20 w-20">
        <svg viewBox="0 0 100 100" className="h-full w-full overflow-visible" aria-hidden>
          <motion.path
            d={SHAPES[0]}
            style={{ transformBox: "view-box", transformOrigin: "50px 50px" }}
            animate={
              reduce
                ? { d: SHAPES[0], scale: 1, rotate: 0 }
                : { d: SHAPES, scale: [1, 0.88, 1.12, 0.94, 1], rotate: [0, 45, 90, 180, 270, 360] }
            }
            transition={reduce ? { duration: 0.3 } : morphLoop}
            className="fill-neutral-950 drop-shadow-[0_4px_16px_rgba(0,0,0,0.15)]"
          />
        </svg>

        <div className="absolute inset-0 flex items-center justify-center">
          <AnimatePresence mode="wait" initial={false}>
            <motion.div
              key={idx}
              initial={reduce ? { opacity: 0 } : { opacity: 0, scale: 0.6, filter: "blur(4px)" }}
              animate={reduce ? { opacity: 1 } : { opacity: 1, scale: 1, filter: "blur(0px)" }}
              exit={
                reduce
                  ? { opacity: 0, transition: { duration: 0.15 } }
                  : { opacity: 0, scale: 0.6, filter: "blur(4px)", transition: iconExit }
              }
              transition={reduce ? { duration: 0.15 } : iconEnter}
              className="flex items-center justify-center will-change-transform"
            >
              <Icono reduce={reduce} />
            </motion.div>
          </AnimatePresence>
        </div>
      </div>
    </div>
  )
}