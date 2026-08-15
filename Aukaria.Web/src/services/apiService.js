// Limpia barras finales y define la raíz
const RAW_URL =
  import.meta.env.VITE_API_BASE_URL ||
  (import.meta.env.PROD
    ? "https://aukaria-production.up.railway.app"
    : "https://localhost:7078")

const CLEAN_URL = RAW_URL.replace(/\/+$/, "")

// Garantiza el esquema https si la variable viene sin protocolo
const SCHEMED_URL = /^https?:\/\//.test(CLEAN_URL) ? CLEAN_URL : `https://${CLEAN_URL}`

// Elimina rutas legadas (/api/analisis) para evitar duplicados
const ROOT_URL = SCHEMED_URL.replace(/\/api\/analisis$/i, "")

// Garantiza que termine en /api/AnalisisPredial
const API_BASE = ROOT_URL.includes("/api/AnalisisPredial")
  ? ROOT_URL
  : `${ROOT_URL}/api/AnalisisPredial`

console.log("[Aukaria API Service] URL Base configurada:", API_BASE)

export const EMPRESA_ID = "11111111-1111-1111-1111-111111111111"
export const USUARIO_ID = "22222222-2222-2222-2222-222222222222"

const TIMEOUT_MS = 60000

async function request(path, options = {}, tipoRespuesta = "json") {
  const controller = new AbortController()
  const timeout = setTimeout(() => controller.abort(), TIMEOUT_MS)

  let response
  try {
    response = await fetch(`${API_BASE}${path}`, { signal: controller.signal, ...options })
  } catch (err) {
    if (err?.name === "AbortError") {
      throw new Error("La solicitud tardó demasiado. Reintenta la operación.")
    }
    throw new Error(
      `No se pudo conectar con la API (${API_BASE}). Verifica que el backend esté corriendo.`,
    )
  } finally {
    clearTimeout(timeout)
  }

  if (!response.ok) {
    let message = `El servidor respondió con un error (${response.status}).`
    try {
      const texto = await response.text()
      if (texto) {
        try {
          const data = JSON.parse(texto)
          if (typeof data === "string" && data) message = data
          else if (data?.detail) message = data.detail
          else if (data?.message) message = data.message
        } catch {
          message = texto
        }
      }
    } catch {
      /* la respuesta no era legible */
    }
    throw new Error(message)
  }

  return tipoRespuesta === "blob" ? response.blob() : response.json()
}

export async function preAnalizarCtl(archivoPdf, empresaId = EMPRESA_ID) {
  const formData = new FormData()
  formData.append("archivoPdf", archivoPdf)
  formData.append("empresaId", empresaId)
  return request("/pre-analisis", { method: "POST", body: formData })
}

export async function procesarAnalisisCtl({
  archivoPdf,
  matriculaFmi,
  proposito = "CompraVenta",
  tipoDocumento = "CTL",
  empresaId = EMPRESA_ID,
  usuarioId = USUARIO_ID,
}) {
  const formData = new FormData()
  formData.append("archivoPdf", archivoPdf)
  formData.append("matriculaFmi", matriculaFmi ?? "")
  formData.append("proposito", proposito)
  formData.append("tipoDocumento", tipoDocumento)
  formData.append("empresaId", empresaId)
  formData.append("usuarioId", usuarioId)
  return request("/procesar", { method: "POST", body: formData })
}

export async function descargarReporteWord(analisisId, matriculaFmi) {
  const blob = await request(`/descargar-word/${analisisId}`, { method: "GET" }, "blob")
  const nombre = matriculaFmi
    ? `Estudio_Predial_${matriculaFmi}.docx`
    : `Estudio_Predial_${analisisId}.docx`
  const url = URL.createObjectURL(blob)
  const link = document.createElement("a")
  link.href = url
  link.download = nombre
  document.body.appendChild(link)
  link.click()
  link.remove()
  setTimeout(() => URL.revokeObjectURL(url), 1500)
}