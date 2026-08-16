import { motion, useReducedMotion } from "framer-motion"

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

export default function GoogleMorphLoader({ className = "" }) {
  const reduce = useReducedMotion()

  return (
    <div className={`relative flex items-center justify-center ${className}`}>
      <motion.div
        className="h-20 w-20 will-change-transform"
        animate={
          reduce
            ? { scale: 1, rotate: 0 }
            : { scale: [1, 0.88, 1.12, 0.94, 1], rotate: [0, 45, 90, 180, 270, 360] }
        }
        transition={reduce ? { duration: 0.3 } : morphLoop}
      >
        <svg viewBox="0 0 100 100" className="h-full w-full" aria-hidden>
          <motion.path
            d={SHAPES[0]}
            animate={reduce ? { d: SHAPES[0] } : { d: SHAPES }}
            transition={reduce ? { duration: 0.3 } : morphLoop}
            className="fill-neutral-900 drop-shadow-[0_4px_16px_rgba(0,0,0,0.12)] dark:fill-white dark:drop-shadow-[0_4px_16px_rgba(255,255,255,0.25)]"
          />
        </svg>
      </motion.div>
    </div>
  )
}