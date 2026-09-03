import { useEffect, useRef, useState } from "react"
import { AnimatePresence, motion } from "framer-motion"
import Navbar from "./components/Navbar"
import LandingPage from "./components/LandingPage"
import MainAppView from "./components/MainAppView"
import LoginModal from "./components/LoginModal"
import ProcessingOverlay from "./components/ProcessingOverlay"
import FloatingAnalysisTracker from "./components/FloatingAnalysisTracker"
import CompletionToast from "./components/CompletionToast"
import TopographyBackground from "./components/TopographyBackground"
import {
  descargarReporteWord,
  procesarAnalisisCtlStreaming,
  obtenerUsuarioActual,
  enviarReporteWord,
  cerrarSesion,
} from "./services/apiService"

const fade = { duration: 0.3 }
const spring = { type: "spring", stiffness: 400, damping: 30 }

function extraerInfoPredio(res) {
  const r = res?.Resultado ?? res?.resultado ?? {}
  const info = r?.informacionGeneral || r?.capitulo1 || r
  const v = (obj, ...keys) => {
    for (const k of keys) {
      const val = obj?.[k]
      if (val !== undefined && val !== null && String(val).trim() !== "") return String(val).trim()
    }
    return ""
  }
  return {
    matricula:
      v(info, "MatriculaFMI", "matriculaFMI", "FolioMatricula", "folioMatricula", "matricula") ||
      v(r, "MatriculaFMI", "matriculaFMI", "FolioMatricula", "folioMatricula", "matricula") ||
      v(res, "MatriculaFMI", "matriculaFMI"),
    nombrePredio:
      v(info, "NombrePredio", "nombrePredio") ||
      v(r, "NombrePredio", "nombrePredio") ||
      v(res, "NombrePredio", "nombrePredio"),
    municipio:
      v(info, "Municipio", "municipio") || v(r, "Municipio", "municipio") || v(res, "Municipio", "municipio"),
    departamento:
      v(info, "Departamento", "departamento") ||
      v(r, "Departamento", "departamento") ||
      v(res, "Departamento", "departamento"),
    orip:
      v(info, "ORIP", "orip", "oficinaRegistro", "OficinaRegistro") ||
      v(r, "ORIP", "orip", "oficinaRegistro", "OficinaRegistro") ||
      v(res, "ORIP", "orip", "oficinaRegistro", "OficinaRegistro"),
    cedulaCatastral:
      v(info, "CedulaCatastral", "cedulaCatastral") ||
      v(r, "CedulaCatastral", "cedulaCatastral") ||
      v(res, "CedulaCatastral", "cedulaCatastral"),
  }
}

function ErrorBanner({ message, onClose }) {
  return (
    <motion.div
      role="alert"
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: 8 }}
      transition={spring}
      className="fixed bottom-5 left-1/2 z-[60] w-[calc(100%-2rem)] max-w-md -translate-x-1/2"
    >
      <div className="flex items-start gap-3 rounded-2xl border border-red-200 bg-red-50/80 p-4 shadow-2xl backdrop-blur-2xl">
        <svg
          viewBox="0 0 24 24"
          className="mt-0.5 h-4 w-4 shrink-0 text-red-600"
          fill="none"
          stroke="currentColor"
          strokeWidth={2}
        >
          <circle cx="12" cy="12" r="9" />
          <path d="M12 8v4M12 16h.01" strokeLinecap="round" />
        </svg>
        <div className="min-w-0 flex-1">
          <p className="text-xs font-bold text-red-600">Algo salió mal</p>
          <p className="mt-0.5 text-xs leading-relaxed text-red-700/80">{message}</p>
        </div>
        <button
          type="button"
          onClick={onClose}
          aria-label="Cerrar notificación"
          className="shrink-0 rounded-full p-1 text-red-400 transition-colors duration-150 hover:bg-red-100 hover:text-red-600"
        >
          <svg
            viewBox="0 0 24 24"
            className="h-3.5 w-3.5"
            fill="none"
            stroke="currentColor"
            strokeWidth={2}
          >
            <path d="M18 6 6 18M6 6l12 12" strokeLinecap="round" />
          </svg>
        </button>
      </div>
    </motion.div>
  )
}

function normalizarUsuario(dto) {
  const rol =
    String(dto?.Rol || dto?.rol || "").toLowerCase() === "adminempresa"
      ? "Admin Legal"
      : "Analista Jurídico"
  return {
    ...dto,
    id: dto?.Id,
    empresaId: dto?.EmpresaId,
    usuarioId: dto?.Id,
    nombre: dto?.Nombre || dto?.nombre || "Analista",
    email: dto?.Email || dto?.email || "",
    empresa: dto?.EmpresaNombre || dto?.empresa || "Empresa Demo S.A.S.",
    rol,
    proveedor: dto?.Provider || dto?.provider || "local",
    creditosUsados: 1,
    creditosTotal: 50,
  }
}

const tieneTokenInicial = (() => {
  const match = window.location.search?.match(/[?&]token=([^&]+)/)
  if (match) return true
  try { return !!localStorage.getItem("aukaria_token") } catch { return false }
})()

function reproducirTonoCompletado() {
  try {
    const AudioCtx = window.AudioContext || window.webkitAudioContext
    if (!AudioCtx) return
    const ctx = new AudioCtx()
    const now = ctx.currentTime
    const notas = [660, 880] // Mi5 -> La5: pequeño arpegio de éxito
    notas.forEach((frec, i) => {
      const osc = ctx.createOscillator()
      const gain = ctx.createGain()
      osc.type = "sine"
      osc.frequency.value = frec
      const t = now + i * 0.18
      gain.gain.setValueAtTime(0.0001, t)
      gain.gain.exponentialRampToValueAtTime(0.18, t + 0.03)
      gain.gain.exponentialRampToValueAtTime(0.0001, t + 0.35)
      osc.connect(gain).connect(ctx.destination)
      osc.start(t)
      osc.stop(t + 0.4)
    })
    window.setTimeout(() => ctx.close().catch(() => {}), 1200)
  } catch {
    /* ignora: el tono es opcional */
  }
}

function App() {
  const [paso, setPaso] = useState(tieneTokenInicial ? "cargando" : "landing")
  const [usuario, setUsuario] = useState(null)
  const [isProcessing, setIsProcessing] = useState(false)
  const [resultado, setResultado] = useState(null)
  const [preAnalisisData, setPreAnalisisData] = useState(null)
  const [historial, setHistorial] = useState([])
  const [progresoAnalisis, setProgresoAnalisis] = useState(null)
  const [errorMsg, setErrorMsg] = useState("")
  const [enSegundoPlano, setEnSegundoPlano] = useState(false)
  const [notificacionLista, setNotificacionLista] = useState(null)
  const errorTimerRef = useRef(null)
  const pendienteRef = useRef(null)
  const segundoPlanoRef = useRef(false)

  useEffect(() => {
    obtenerUsuarioActual().then((dto) => {
      if (dto) {
        setUsuario(normalizarUsuario(dto))
        setPaso("app")
      } else {
        setPaso("landing")
      }
    })
  }, [])

  const notificar = (message) => {
    setErrorMsg(message)
    window.clearTimeout(errorTimerRef.current)
    errorTimerRef.current = window.setTimeout(() => setErrorMsg(""), 6000)
  }

  const runAnalysis = async (file, meta, inBackground = false) => {
    setErrorMsg("")
    setProgresoAnalisis(null)
    segundoPlanoRef.current = inBackground
    setEnSegundoPlano(inBackground)
    setIsProcessing(true)
    try {
      const res = await procesarAnalisisCtlStreaming({
        archivoPdf: file,
        matriculaFmi: meta?.matriculaFmi || "",
        proposito: "CompraVenta",
        tipoDocumento: meta?.tipoDocumento || "CTL",
        empresaId: usuario?.empresaId,
        usuarioId: usuario?.usuarioId,
        onProgreso: (progreso) => setProgresoAnalisis(progreso),
      })
      setProgresoAnalisis(null)
      setResultado(res)
      const info = extraerInfoPredio(res)
      setPreAnalisisData((prev) => ({
        ...(prev || {}),
        matricula: info.matricula || meta?.preAnalisis?.fmi || prev?.matricula || "",
        nombrePredio: info.nombrePredio || meta?.preAnalisis?.nombrePredio || prev?.nombrePredio || "",
        municipio: info.municipio || meta?.preAnalisis?.municipio || prev?.municipio || "",
        departamento: info.departamento || prev?.departamento || "",
        orip: info.orip || meta?.preAnalisis?.orip || prev?.orip || "",
        cedulaCatastral: info.cedulaCatastral || meta?.preAnalisis?.cedulaCatastral || prev?.cedulaCatastral || "",
      }))
      setHistorial((prev) => [res, ...prev.filter((h) => h.Id !== res.Id)].slice(0, 6))
      setIsProcessing(false)

      const corriendoEnSegundoPlano = segundoPlanoRef.current
      segundoPlanoRef.current = false
      setEnSegundoPlano(false)

      if (corriendoEnSegundoPlano) {
        setNotificacionLista(res)
        reproducirTonoCompletado()
      } else {
        setPaso("dashboard")
      }
    } catch (err) {
      setProgresoAnalisis(null)
      setIsProcessing(false)
      segundoPlanoRef.current = false
      setEnSegundoPlano(false)
      notificar(err.message || "Error al procesar el análisis jurídico.")
    }
  }

  const handleAnalyze = (file, meta, inBackground = false) => {
    if (!usuario) {
      pendienteRef.current = { file, meta, inBackground }
      setPaso("login")
      return
    }
    runAnalysis(file, meta, inBackground)
  }

  const pasarASegundoPlano = () => {
    segundoPlanoRef.current = true
    setEnSegundoPlano(true)
  }

  const volverAMaximizar = () => {
    segundoPlanoRef.current = false
    setEnSegundoPlano(false)
  }

  const verDictamenEnSegundoPlano = () => {
    setNotificacionLista(null)
    setPaso("dashboard")
  }

  const handleLogin = (data) => {
    setUsuario(normalizarUsuario(data))
    const pendiente = pendienteRef.current
    pendienteRef.current = null
    if (pendiente) {
      runAnalysis(pendiente.file, pendiente.meta, pendiente.inBackground)
    } else {
      setPaso("app")
    }
  }

  const handleReabrir = (id) => {
    const estudio = historial.find((h) => h.Id === id)
    if (estudio) {
      setResultado(estudio)
      setPreAnalisisData(extraerInfoPredio(estudio))
      setPaso("dashboard")
    }
  }

  const handleNuevoEstudio = () => {
    setPreAnalisisData(null)
    setPaso("app")
  }

  const handleLogout = async () => {
    try {
      await cerrarSesion()
    } catch {
      /* ignora errores al cerrar sesión */
    }
    setUsuario(null)
    pendienteRef.current = null
    setPreAnalisisData(null)
    setEnSegundoPlano(false)
    segundoPlanoRef.current = false
    setNotificacionLista(null)
    setPaso("landing")
  }

  const handleDownload = async () => {
    if (!resultado) return
    try {
      await descargarReporteWord(
        resultado.Id,
        resultado.MatriculaFMI ||
          resultado.Resultado?.MatriculaFMI ||
          preAnalisisData?.matricula,
      )
    } catch (err) {
      notificar(err.message || "No se pudo descargar el reporte.")
    }
  }

  const handleEnviarPorCorreo = async () => {
    if (!resultado) return
    try {
      await enviarReporteWord(resultado.Id)
      notificar("Reporte enviado a tu correo.")
    } catch (err) {
      notificar(err.message || "No se pudo enviar el reporte por correo.")
    }
  }

    const modo = paso === "dashboard" ? "dashboard" : "workspace"

  return (
    <div className="relative min-h-screen bg-neutral-100/80 text-neutral-900 antialiased selection:bg-[var(--brand-g1)] selection:text-white">
      <TopographyBackground />
      <AnimatePresence mode="wait">
        {paso === "cargando" && (
          <motion.div key="cargando" initial={{ opacity: 1 }} exit={{ opacity: 0 }} transition={fade} className="relative z-10 flex min-h-screen items-center justify-center">
            <div className="h-8 w-8 animate-spin rounded-full border-2 border-neutral-300 border-t-[var(--brand-g1)]" />
          </motion.div>
        )}
        {paso === "landing" && (
          <motion.div
            key="landing"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={fade}
            className="relative z-10"
          >
            <LandingPage onAnalyze={handleAnalyze} onLogin={() => setPaso("login")} />
          </motion.div>
        )}
        {paso !== "cargando" && paso !== "landing" && (
          <motion.div key="app" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} transition={fade} className="relative z-10">
            <Navbar usuario={usuario} onLogout={handleLogout} />
            <main className="w-full pb-24 pt-6 md:pt-8">
              <MainAppView
                modo={modo}
                analisis={resultado}
                preAnalisisData={preAnalisisData}
                historial={historial}
                usuario={usuario}
                onAnalyze={runAnalysis}
                onReabrir={handleReabrir}
                onNuevoEstudio={handleNuevoEstudio}
                onDownload={handleDownload}
                onEnviarPorCorreo={handleEnviarPorCorreo}
                onLogout={handleLogout}
              />
            </main>
          </motion.div>
        )}
      </AnimatePresence>

      <AnimatePresence>
        {paso === "login" && (
          <LoginModal onClose={() => setPaso("landing")} onLogin={handleLogin} />
        )}
      </AnimatePresence>

      <AnimatePresence>
        {isProcessing && !enSegundoPlano && (
          <ProcessingOverlay
            onComplete={() => {}}
            autoCompletar={false}
            progreso={progresoAnalisis}
            onRunInBackground={pasarASegundoPlano}
          />
        )}
      </AnimatePresence>

      <AnimatePresence>
        {isProcessing && enSegundoPlano && (
          <FloatingAnalysisTracker progreso={progresoAnalisis} onMaximize={volverAMaximizar} />
        )}
      </AnimatePresence>

      <AnimatePresence>
        {notificacionLista && (
          <CompletionToast
            fmi={
              notificacionLista.MatriculaFMI ||
              notificacionLista.Resultado?.MatriculaFMI ||
              preAnalisisData?.matricula
            }
            onVerDictamen={verDictamenEnSegundoPlano}
            onClose={() => setNotificacionLista(null)}
          />
        )}
      </AnimatePresence>

      <AnimatePresence>
        {errorMsg && <ErrorBanner message={errorMsg} onClose={() => setErrorMsg("")} />}
      </AnimatePresence>
    </div>
  )
}

export default App