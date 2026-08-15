import { useEffect, useState } from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"

const spring = { type: "spring", bounce: 0, duration: 0.4 }

const LANGUAGES = [
  { code: "es-CO", label: "Español (Colombia)", flag: "🇨🇴" },
  { code: "en-US", label: "English (US)", flag: "🇺🇸" },
  { code: "fr-FR", label: "Français", flag: "🇫🇷", pronto: true },
  { code: "de-DE", label: "Deutsch", flag: "🇩🇪", pronto: true },
  { code: "pt-BR", label: "Português", flag: "🇵🇹", pronto: true },
]

export default function LanguageSelector({ className = "" }) {
  const reduce = useReducedMotion()
  const [abierto, setAbierto] = useState(false)
  const [activo, setActivo] = useState(LANGUAGES[0])

  useEffect(() => {
    if (!abierto) return undefined
    const cerrar = () => setAbierto(false)
    document.addEventListener("pointerdown", cerrar)
    return () => document.removeEventListener("pointerdown", cerrar)
  }, [abierto])

  return (
    <div className={`relative ${className}`} onPointerDown={(e) => e.stopPropagation()}>
      <motion.button
        type="button"
        aria-haspopup="listbox"
        aria-expanded={abierto}
        aria-label="Seleccionar idioma"
        whileHover={reduce ? undefined : { scale: 1.03 }}
        whileTap={{ scale: 0.97 }}
        transition={spring}
        onClick={() => setAbierto((v) => !v)}
        className="flex items-center gap-2 rounded-full border border-black/10 bg-white/70 px-3 py-1.5 backdrop-blur-xl transition-colors duration-150 hover:bg-white"
      >
        <span aria-hidden className="text-sm leading-none">
          {activo.flag}
        </span>
        <span className="font-mono text-[11px] font-bold uppercase tracking-widest text-neutral-700">
          {activo.code}
        </span>
        <svg
          viewBox="0 0 24 24"
          aria-hidden
          className={`h-3 w-3 text-neutral-400 transition-transform duration-200 ${
            abierto ? "rotate-180" : ""
          }`}
          fill="none"
          stroke="currentColor"
          strokeWidth={2.5}
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="m6 9 6 6 6-6" />
        </svg>
      </motion.button>

      <AnimatePresence>
        {abierto && (
          <motion.ul
            role="listbox"
            aria-label="Idiomas disponibles"
            initial={reduce ? { opacity: 0 } : { opacity: 0, y: 6, scale: 0.97 }}
            animate={reduce ? { opacity: 1 } : { opacity: 1, y: 0, scale: 1 }}
            exit={reduce ? { opacity: 0 } : { opacity: 0, y: 6, scale: 0.97 }}
            transition={spring}
            className="absolute right-0 top-full z-50 mt-2 w-60 origin-top-right overflow-hidden rounded-2xl border border-black/10 bg-white/90 p-1.5 shadow-2xl backdrop-blur-3xl"
          >
            {LANGUAGES.map((lang) => {
              const activa = lang.code === activo.code
              return (
                <li key={lang.code}>
                  <motion.button
                    type="button"
                    role="option"
                    aria-selected={activa}
                    disabled={lang.pronto}
                    whileHover={lang.pronto || reduce ? undefined : { x: 3 }}
                    whileTap={lang.pronto ? undefined : { scale: 0.98 }}
                    transition={spring}
                    onClick={() => {
                      if (lang.pronto) return
                      setActivo(lang)
                      setAbierto(false)
                    }}
                    className={`flex w-full items-center gap-2.5 rounded-xl px-3 py-2 text-left text-sm transition-colors duration-150 ${
                      activa
                        ? "bg-black text-white"
                        : "text-neutral-700 hover:bg-black/5"
                    } ${lang.pronto ? "cursor-not-allowed opacity-45" : ""}`}
                  >
                    <span aria-hidden className="text-base leading-none">
                      {lang.flag}
                    </span>
                    <span className="min-w-0 flex-1 truncate font-medium">{lang.label}</span>
                    {activa ? (
                      <svg
                        viewBox="0 0 24 24"
                        aria-hidden
                        className="h-3.5 w-3.5 shrink-0"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth={3}
                        strokeLinecap="round"
                        strokeLinejoin="round"
                      >
                        <path d="m5 13 4 4L19 7" />
                      </svg>
                    ) : lang.pronto ? (
                      <span className="shrink-0 font-mono text-[9px] font-bold uppercase tracking-widest text-neutral-400">
                        Pronto
                      </span>
                    ) : null}
                  </motion.button>
                </li>
              )
            })}
          </motion.ul>
        )}
      </AnimatePresence>
    </div>
  )
}