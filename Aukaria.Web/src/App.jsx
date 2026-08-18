import { useRef, useState } from "react"
import { AnimatePresence, motion } from "framer-motion"
import Navbar from "./components/Navbar"
import LandingPage from "./components/LandingPage"
import MainAppView from "./components/MainAppView"
import LoginModal from "./components/LoginModal"
import ProcessingOverlay from "./components/ProcessingOverlay"
import { descargarReporteWord, procesarAnalisisCtl } from "./services/apiService"

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

function App() {
  const [paso, setPaso] = useState("landing")
  const [usuario, setUsuario] = useState(null)
  const [isProcessing, setIsProcessing] = useState(false)
  const [resultado, setResultado] = useState(null)
  const [preAnalisisData, setPreAnalisisData] = useState(null)
  const [historial, setHistorial] = useState([])
  const [errorMsg, setErrorMsg] = useState("")
  const errorTimerRef = useRef(null)
  const pendienteRef = useRef(null)

  const notificar = (message) => {
    setErrorMsg(message)
    window.clearTimeout(errorTimerRef.current)
    errorTimerRef.current = window.setTimeout(() => setErrorMsg(""), 6000)
  }

  const runAnalysis = async (file, meta) => {
    setErrorMsg("")
    setIsProcessing(true)
    try {
      const res = await procesarAnalisisCtl({
        archivoPdf: file,
        matriculaFmi: meta?.matriculaFmi || "",
        proposito: "CompraVenta",
        tipoDocumento: meta?.tipoDocumento || "CTL",
      })
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
      setPaso("dashboard")
    } catch (err) {
      setIsProcessing(false)
      notificar(err.message || "Error al procesar el análisis jurídico.")
    }
  }

  const handleAnalyze = (file, meta) => {
    if (!usuario) {
      pendienteRef.current = { file, meta }
      setPaso("login")
      return
    }
    runAnalysis(file, meta)
  }

  const handleLogin = (data) => {
    setUsuario(data)
    const pendiente = pendienteRef.current
    pendienteRef.current = null
    if (pendiente) {
      runAnalysis(pendiente.file, pendiente.meta)
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

  const handleLogout = () => {
    setUsuario(null)
    pendienteRef.current = null
    setPreAnalisisData(null)
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

    const modo = paso === "dashboard" ? "dashboard" : "workspace"

  return (
    <div className="min-h-screen bg-neutral-100/80 text-neutral-900 antialiased selection:bg-black selection:text-white">
      <AnimatePresence mode="wait">
        {paso === "landing" ? (
          <motion.div
            key="landing"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={fade}
          >
            <LandingPage onAnalyze={handleAnalyze} onLogin={() => setPaso("login")} />
          </motion.div>
        ) : (
          <motion.div key="app" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} transition={fade}>
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
        {isProcessing && <ProcessingOverlay onComplete={() => {}} autoCompletar={false} />}
      </AnimatePresence>

      <AnimatePresence>
        {errorMsg && <ErrorBanner message={errorMsg} onClose={() => setErrorMsg("")} />}
      </AnimatePresence>
    </div>
  )
}

export default App