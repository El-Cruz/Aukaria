import { useState } from "react"
import { motion, useReducedMotion } from "framer-motion"

const spring = { type: "spring", bounce: 0, duration: 0.45 }
const springBtn = { type: "spring", stiffness: 500, damping: 30 }

export default function LoginModal({ onClose, onLogin }) {
  const reduce = useReducedMotion()
  const [correo, setCorreo] = useState("")
  const [contrasena, setContrasena] = useState("")
  const [cargando, setCargando] = useState(false)
  const [error, setError] = useState("")

  const derivarUsuario = (email, proveedor) => {
    const parte = email.split("@")[0] || "analista"
    const nombre = parte
      .replace(/[._-]+/g, " ")
      .replace(/\b\w/g, (c) => c.toUpperCase())
    return {
      nombre,
      email,
      empresa: "Constructora Aurora S.A.S.",
      rol: proveedor === "azure" ? "Admin Legal" : "Analista Jurídico",
      proveedor,
      creditosUsados: 1,
      creditosTotal: 50,
    }
  }

  const ingresar = (evento) => {
    evento?.preventDefault()
    setError("")
    if (!correo.trim() || !contrasena) {
      setError("Ingresa tu correo y contraseña corporativos.")
      return
    }
    setCargando(true)
    window.setTimeout(() => {
      onLogin(derivarUsuario(correo.trim(), "azure"))
    }, reduce ? 50 : 700)
  }

  const ingresarOAuth = (proveedor) => {
    setError("")
    setCargando(true)
    window.setTimeout(() => {
      onLogin(
        derivarUsuario(
          proveedor === "azure" ? "laura.torres@constructoraaurora.com" : "marco.ruiz@constructoraaurora.com",
          proveedor,
        ),
      )
    }, reduce ? 50 : 800)
  }

  const campoBase =
    "w-full rounded-2xl border border-black/10 bg-black/5 px-4 py-3 text-sm text-black placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-[var(--focus-ring)]"

  return (
    <motion.div
      role="dialog"
      aria-modal="true"
      aria-label="Acceso Corporativo B2B"
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.25 }}
      className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/40 p-4 backdrop-blur-2xl"
    >
      <motion.div
        initial={{ opacity: 0, scale: 0.96, y: reduce ? 0 : 14 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={{ opacity: 0, scale: 0.97, y: reduce ? 0 : 8 }}
        transition={spring}
        className="relative w-full max-w-md rounded-3xl border border-black/10 bg-white/85 p-8 shadow-2xl backdrop-blur-3xl"
      >
        <button
          type="button"
          onClick={onClose}
          aria-label="Cerrar ventana de acceso"
          className="absolute right-5 top-5 rounded-full border border-black/10 bg-white/60 p-2 text-neutral-500 transition-colors duration-150 hover:bg-black hover:text-white"
        >
          <svg viewBox="0 0 24 24" className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth={2}>
            <path d="M18 6 6 18M6 6l12 12" strokeLinecap="round" />
          </svg>
        </button>

        <header className="flex flex-col items-center text-center">
          <span className="text-2xl font-black tracking-tighter text-black">AUKARIA</span>
          <span className="mt-1 rounded border border-black/20 px-2 py-0.5 font-mono text-[10px] font-bold uppercase tracking-widest text-neutral-500">
            Multitenant B2B · SNR
          </span>
          <h2 className="mt-5 text-xl font-bold tracking-tight text-black">Acceso Corporativo B2B</h2>
          <p className="mt-1.5 text-xs text-neutral-500">
            Tu cuenta de empresa notarial está protegida por inicio de sesión único (SSO).
          </p>
        </header>

        <form onSubmit={ingresar} className="mt-7 flex flex-col gap-3.5">
          <label className="flex flex-col gap-1.5">
            <span className="font-mono text-[10px] font-bold uppercase tracking-[0.2em] text-neutral-500">
              Correo Corporativo
            </span>
            <input
              type="email"
              required
              value={correo}
              onChange={(e) => setCorreo(e.target.value)}
              placeholder="nombre@empresa.com.co"
              autoComplete="email"
              className={campoBase}
            />
          </label>
          <label className="flex flex-col gap-1.5">
            <span className="font-mono text-[10px] font-bold uppercase tracking-[0.2em] text-neutral-500">
              Contraseña
            </span>
            <input
              type="password"
              required
              value={contrasena}
              onChange={(e) => setContrasena(e.target.value)}
              placeholder="••••••••"
              autoComplete="current-password"
              className={campoBase}
            />
          </label>

          {error && <p className="text-xs font-medium text-red-600">{error}</p>}

          <motion.button
            type="submit"
            disabled={cargando}
            whileTap={{ scale: 0.97 }}
            transition={springBtn}
            className="mt-1 flex items-center justify-center gap-1.5 rounded-full bg-[var(--cta)] px-7 py-3 text-sm font-semibold text-white shadow-[0_4px_16px_color-mix(in_srgb,var(--cta)_35%,transparent)] transition-colors duration-150 hover:bg-[var(--cta-hover)] disabled:opacity-70"
          >
            {cargando ? "Validando credenciales…" : "Iniciar Sesión →"}
          </motion.button>
        </form>

        <div className="my-6 flex items-center gap-3">
          <span className="h-px flex-1 bg-black/10" />
          <span className="font-mono text-[10px] font-bold uppercase tracking-widest text-neutral-400">o SSO</span>
          <span className="h-px flex-1 bg-black/10" />
        </div>

        <div className="flex flex-col gap-2.5">
          <motion.button
            type="button"
            disabled={cargando}
            onClick={() => ingresarOAuth("azure")}
            whileTap={{ scale: 0.97 }}
            transition={springBtn}
            className="flex items-center justify-center gap-2.5 rounded-full border border-black/10 bg-white/70 px-5 py-3 text-sm font-semibold text-neutral-700 transition-colors duration-150 hover:bg-white hover:text-black disabled:opacity-70"
          >
            <AzureIcon />
            Continuar con Microsoft Azure AD / Single Sign-On
          </motion.button>
          <motion.button
            type="button"
            disabled={cargando}
            onClick={() => ingresarOAuth("google")}
            whileTap={{ scale: 0.97 }}
            transition={springBtn}
            className="flex items-center justify-center gap-2.5 rounded-full border border-black/10 bg-white/70 px-5 py-3 text-sm font-semibold text-neutral-700 transition-colors duration-150 hover:bg-white hover:text-black disabled:opacity-70"
          >
            <GoogleIcon />
            Continuar con Google Workspace
          </motion.button>
        </div>

        <p className="mt-6 text-center font-mono text-[10px] uppercase tracking-widest text-neutral-400">
          SSL cifrado · Samsung SDN Colombia · RFC 9000
        </p>
      </motion.div>
    </motion.div>
  )
}

function AzureIcon() {
  return (
    <svg viewBox="0 0 24 24" className="h-4 w-4 shrink-0" aria-hidden="true">
      <rect x="2" y="2" width="9.5" height="9.5" rx="1.5" fill="currentColor" opacity="0.9" />
      <rect x="12.5" y="2" width="9.5" height="9.5" rx="1.5" fill="currentColor" opacity="0.5" />
      <rect x="2" y="12.5" width="9.5" height="9.5" rx="1.5" fill="currentColor" opacity="0.5" />
      <rect x="12.5" y="12.5" width="9.5" height="9.5" rx="1.5" fill="currentColor" opacity="0.3" />
    </svg>
  )
}

function GoogleIcon() {
  return (
    <svg viewBox="0 0 24 24" className="h-4 w-4 shrink-0" aria-hidden="true">
      <path
        fill="currentColor"
        fillRule="evenodd"
        d="M20.5 12.2c0-.6-.06-1.2-.16-1.7H12v3.2h4.76a3.9 3.9 0 0 1-1.7 2.57v2.1h2.73c1.6-1.48 2.7-3.66 2.7-6.17Z"
        clipRule="evenodd"
        opacity="0.85"
      />
      <path
        fill="currentColor"
        fillRule="evenodd"
        d="M12 21c2.3 0 4.2-.76 5.59-2.05l-2.73-2.1c-.75.5-1.77.86-2.86.86-2.2 0-4.07-1.49-4.73-3.5H4.46v2.2A8.45 8.45 0 0 0 12 21Z"
        clipRule="evenodd"
        opacity="0.6"
      />
      <path
        fill="currentColor"
        fillRule="evenodd"
        d="M7.27 14.2a5.1 5.1 0 0 1 0-3.25V8.76H4.46a8.5 8.5 0 0 0 0 7.63l2.81-2.2Z"
        clipRule="evenodd"
        opacity="0.45"
      />
      <path
        fill="currentColor"
        fillRule="evenodd"
        d="M12 6.96c1.32 0 2.5.46 3.43 1.35l2.55-2.55A8.45 8.45 0 0 0 4.46 8.76l2.81 2.19c.66-2 2.53-3.5 4.73-3.5Z"
        clipRule="evenodd"
        opacity="0.3"
      />
    </svg>
  )
}