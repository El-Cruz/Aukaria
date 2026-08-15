import { motion } from "framer-motion"
import LanguageSelector from "./LanguageSelector"

const spring = { type: "spring", stiffness: 400, damping: 25 }

export default function Navbar({ usuario = null, onLogout = () => {} }) {
  const iniciales = usuario?.nombre
    ? usuario.nombre
        .split(" ")
        .map((p) => p[0])
        .slice(0, 2)
        .join("")
        .toUpperCase()
    : "AD"
  const nombre = usuario?.nombre || "Analista Demo"

  return (
    <motion.header
      initial={{ y: -24, opacity: 0 }}
      animate={{ y: 0, opacity: 1 }}
      transition={spring}
      className="sticky top-0 z-50 border-b border-black/10 bg-white/70 backdrop-blur-2xl"
    >
      <nav className="mx-auto flex h-14 max-w-[1700px] items-center gap-3 px-6">
        <div className="flex items-baseline gap-2">
          <span className="text-xl font-black tracking-tighter text-black">AUKARIA</span>
          <span className="rounded border border-black/20 px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-widest text-neutral-500">
            v1.0 B2B
          </span>
        </div>

        <div className="ml-auto flex items-center gap-3">
          <LanguageSelector className="hidden sm:block" />

          <div className="flex items-center gap-2 rounded-full border border-black/10 bg-white/60 py-1 pl-1 pr-3">
            <span className="flex h-6 w-6 select-none items-center justify-center rounded-full bg-black text-[10px] font-bold text-white">
              {iniciales}
            </span>
            <span className="hidden leading-tight lg:block">
              <span className="block text-xs font-semibold text-neutral-700">{nombre}</span>
              <span className="block font-mono text-[9px] uppercase tracking-widest text-neutral-400">
                {usuario?.empresa || "Cuenta Corporativa"}
              </span>
            </span>
          </div>

          <motion.button
            type="button"
            onClick={onLogout}
            whileTap={{ scale: 0.97 }}
            transition={spring}
            className="flex items-center gap-1.5 rounded-full border border-black/10 bg-white/60 px-3.5 py-2 text-xs font-semibold text-neutral-700 backdrop-blur-xl transition-colors duration-150 hover:bg-black hover:text-white"
          >
            <svg viewBox="0 0 24 24" className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth={2}>
              <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4M16 17l5-5-5-5M21 12H9" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
            Cerrar Sesión
          </motion.button>
        </div>
      </nav>
    </motion.header>
  )
}