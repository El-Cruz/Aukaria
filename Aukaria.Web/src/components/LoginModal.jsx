import { useState } from "react"
import { motion, useReducedMotion } from "framer-motion"
import { iniciarGoogle, iniciarMicrosoft, solicitarOtp, verificarOtp, EMPRESA_ID } from "../services/apiService"

const spring = { type: "spring", bounce: 0, duration: 0.45 }
const springBtn = { type: "spring", stiffness: 500, damping: 30 }

const MODOS = {
  login: { titulo: "Iniciar sesión", botonCodigo: "Enviar código", requiereNombre: false },
  registro: { titulo: "Crear cuenta", botonCodigo: "Crear cuenta y enviar código", requiereNombre: true },
}

export default function LoginModal({ onClose, onLogin }) {
  const reduce = useReducedMotion()
  const [modo, setModo] = useState("login")
  const [correo, setCorreo] = useState("")
  const [nombre, setNombre] = useState("")
  const [codigo, setCodigo] = useState("")
  const [paso, setPaso] = useState("correo") // correo | otp
  const [cargando, setCargando] = useState(false)
  const [error, setError] = useState("")
  const [info, setInfo] = useState("")

  const conf = MODOS[modo]

  const enviarCodigo = async (evento) => {
    evento?.preventDefault()
    setError("")
    if (!correo.trim() || !correo.includes("@")) {
      setError("Ingresa un correo válido.")
      return
    }
    if (conf.requiereNombre && !nombre.trim()) {
      setError("Ingresa tu nombre para crear la cuenta.")
      return
    }
    setCargando(true)
    try {
      await solicitarOtp(correo.trim())
      setInfo(`Te enviamos un código a ${correo.trim()}. Revísalo e ingrésalo abajo.`)
      setPaso("otp")
      setCargando(false)
    } catch (err) {
      setError(err.message || "No se pudo enviar el código.")
      setCargando(false)
    }
  }

  const verificar = async (evento) => {
    evento?.preventDefault()
    setError("")
    setInfo("")
    if (!codigo.trim()) {
      setError("Ingresa el código de verificación.")
      return
    }
    setCargando(true)
    try {
      const usuario = await verificarOtp({
        email: correo.trim(),
        codigoOtp: codigo.trim(),
        nombre: nombre.trim(),
        empresaId: EMPRESA_ID,
        esRegistro: modo === "registro",
      })
      onLogin(usuario)
    } catch (err) {
      setError(err.message || "Código incorrecto o expirado.")
      setCargando(false)
    }
  }

  const volver = () => {
    setError("")
    setInfo("")
    setCodigo("")
    setPaso("correo")
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
          <h2 className="mt-5 text-xl font-bold tracking-tight text-black">{conf.titulo}</h2>
          <p className="mt-1.5 text-xs text-neutral-500">
            Accede con tu cuenta corporativa o con tu correo verificando un código.
          </p>
        </header>

        {paso === "correo" ? (
          <form onSubmit={enviarCodigo} className="mt-7 flex flex-col gap-3.5">
            <div className="flex items-center gap-1 rounded-full bg-black/5 p-1">
              {Object.entries(MODOS).map(([clave, m]) => (
                <button
                  key={clave}
                  type="button"
                  onClick={() => {
                    setModo(clave)
                    setError("")
                  }}
                  className={`flex-1 rounded-full px-3 py-1.5 text-xs font-semibold transition-colors duration-150 ${
                    modo === clave ? "bg-white text-black shadow" : "text-neutral-500 hover:text-black"
                  }`}
                >
                  {m.titulo}
                </button>
              ))}
            </div>

            {conf.requiereNombre && (
              <label className="flex flex-col gap-1.5">
                <span className="font-mono text-[10px] font-bold uppercase tracking-[0.2em] text-neutral-500">
                  Nombre completo
                </span>
                <input
                  type="text"
                  value={nombre}
                  onChange={(e) => setNombre(e.target.value)}
                  placeholder="Nombre del analista"
                  autoComplete="name"
                  className={campoBase}
                />
              </label>
            )}

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

            {error && <p className="text-xs font-medium text-red-600">{error}</p>}

            <motion.button
              type="submit"
              disabled={cargando}
              whileTap={{ scale: 0.97 }}
              transition={springBtn}
              className="mt-1 flex items-center justify-center gap-1.5 rounded-full bg-[var(--cta)] px-7 py-3 text-sm font-semibold text-white shadow-[0_4px_16px_color-mix(in_srgb,var(--cta)_35%,transparent)] transition-colors duration-150 hover:bg-[var(--cta-hover)] disabled:opacity-70"
            >
              {cargando ? "Enviando…" : conf.botonCodigo}
            </motion.button>
          </form>
        ) : (
          <form onSubmit={verificar} className="mt-7 flex flex-col gap-3.5">
            {info && <p className="text-xs font-medium text-emerald-700">{info}</p>}
            <label className="flex flex-col gap-1.5">
              <span className="font-mono text-[10px] font-bold uppercase tracking-[0.2em] text-neutral-500">
                Código de verificación
              </span>
              <input
                type="text"
                inputMode="numeric"
                value={codigo}
                onChange={(e) => setCodigo(e.target.value)}
                placeholder="000000"
                autoFocus
                className={`${campoBase} text-center tracking-[0.4em]`}
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
              {cargando ? "Verificando…" : "Verificar e ingresar"}
            </motion.button>

            <button
              type="button"
              onClick={volver}
              className="text-center text-xs font-semibold text-neutral-500 transition-colors duration-150 hover:text-black"
            >
              ← Volver y cambiar correo
            </button>
          </form>
        )}

        {paso === "correo" && (
          <>
            <div className="my-6 flex items-center gap-3">
              <span className="h-px flex-1 bg-black/10" />
              <span className="font-mono text-[10px] font-bold uppercase tracking-widest text-neutral-400">o SSO</span>
              <span className="h-px flex-1 bg-black/10" />
            </div>

            <div className="flex flex-col gap-2.5">
              <motion.button
                type="button"
                disabled={cargando}
                onClick={() => iniciarMicrosoft()}
                whileTap={{ scale: 0.97 }}
                transition={springBtn}
                className="flex items-center justify-center gap-2.5 rounded-full border border-black/10 bg-white/70 px-5 py-3 text-sm font-semibold text-neutral-700 transition-colors duration-150 hover:bg-white hover:text-black disabled:opacity-70"
              >
                <AzureIcon />
                Continuar con Microsoft
              </motion.button>
              <motion.button
                type="button"
                disabled={cargando}
                onClick={() => iniciarGoogle()}
                whileTap={{ scale: 0.97 }}
                transition={springBtn}
                className="flex items-center justify-center gap-2.5 rounded-full border border-black/10 bg-white/70 px-5 py-3 text-sm font-semibold text-neutral-700 transition-colors duration-150 hover:bg-white hover:text-black disabled:opacity-70"
              >
                <GoogleIcon />
                Continuar con Google
              </motion.button>
            </div>
          </>
        )}

        <p className="mt-6 text-center font-mono text-[10px] uppercase tracking-widest text-neutral-400">
          SSL cifrado · Acceso SSO y OTP
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
