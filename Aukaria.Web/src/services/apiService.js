/**
 * Deriva la URL base (origen) del backend de forma dinámica
 * a partir de VITE_BACKEND_URL o VITE_API_BASE_URL con fallback seguro hacia Render (o localhost en desarrollo).
 * Erradica activamente cualquier remanente de Railway en caché o configuración de entorno.
 */
export const getBackendBaseUrl = () => {
  const envUrl = import.meta.env.VITE_BACKEND_URL || import.meta.env.VITE_API_BASE_URL
  const fallback = import.meta.env.DEV
    ? "https://localhost:7078"
    : "https://aukaria.onrender.com"

  let rawUrl = envUrl || fallback

  // Si la variable viene con menciones a railway.app, forzamos Render
  if (!rawUrl || String(rawUrl).includes("railway")) {
    rawUrl = "https://aukaria.onrender.com"
  }

  const clean = String(rawUrl).trim().replace(/\/+$/, "")
  const schemed = /^https?:\/\//i.test(clean) ? clean : `https://${clean}`

  try {
    const parsed = new URL(schemed)
    if (parsed.hostname.includes("railway")) {
      return "https://aukaria.onrender.com"
    }
    return parsed.origin // Retorna https://aukaria.onrender.com o https://localhost:7078
  } catch {
    return "https://aukaria.onrender.com"
  }
}

export const ROOT_URL = getBackendBaseUrl()

// Base para los endpoints de análisis predial (/api/AnalisisPredial)
export const API_BASE = `${ROOT_URL}/api/AnalisisPredial`

// Base para los endpoints de autenticación (/api/auth)
export const AUTH_BASE = `${ROOT_URL}/api/auth`

console.log("[Aukaria API Service] Servidor backend configurado:", ROOT_URL)
console.log("[Aukaria API Service] API Predial Base:", API_BASE)
console.log("[Aukaria API Service] Auth Base:", AUTH_BASE)

export const EMPRESA_ID = "11111111-1111-1111-1111-111111111111"
export const USUARIO_ID = "22222222-2222-2222-2222-222222222222"

const TIMEOUT_MS = 60000
const ANALISIS_TIMEOUT_MS = 240000

const TOKEN_KEY = "aukaria_token"

export function guardarToken(token) {
  if (token) localStorage.setItem(TOKEN_KEY, token)
  else localStorage.removeItem(TOKEN_KEY)
}

export function obtenerToken() {
  return typeof window !== "undefined" ? localStorage.getItem(TOKEN_KEY) || null : null
}

export function capturarTokenDeUrl() {
  const match = window.location.search?.match(/[?&]token=([^&]+)/)
  if (!match) return null
  try {
    const token = decodeURIComponent(match[1])
    const url = window.location.origin + window.location.pathname
    window.history.replaceState({}, "", url)
    return token
  } catch {
    return null
  }
}

function conToken(opciones = {}) {
  const token = obtenerToken()
  if (!token) return opciones
  const headers = new Headers(opciones.headers || {})
  headers.set("Authorization", `Bearer ${token}`)
  return { ...opciones, headers }
}

async function authRequest(path, options = {}) {
  const response = await fetch(`${AUTH_BASE}${path}`, conToken(options))

  const texto = await response.text()
  let data = null
  if (texto) {
    try {
      data = JSON.parse(texto)
    } catch {
      data = texto
    }
  }

  if (!response.ok) {
    const mensaje =
      (typeof data === "string" && data) ||
      data?.mensaje ||
      data?.message ||
      data?.detalle ||
      data?.error ||
      `[HTTP ${response.status}] Error de autenticación.`
    throw new Error(mensaje)
  }

  return data
}

async function request(path, options = {}, tipoRespuesta = "json", timeoutMs = TIMEOUT_MS) {
  const tieneTimeout = timeoutMs > 0
  const controller = tieneTimeout ? new AbortController() : null
  const timeout = tieneTimeout ? setTimeout(() => controller.abort(), timeoutMs) : null

  const fetchOptions = { ...conToken(options) }
  if (controller) fetchOptions.signal = controller.signal

  let response
  try {
    response = await fetch(`${API_BASE}${path}`, fetchOptions)
  } catch (err) {
    if (err?.name === "AbortError") {
      throw new Error("La solicitud tardó demasiado. Reintenta la operación.")
    }
    throw new Error(
      `No se pudo conectar con la API (${API_BASE}). Verifica que el backend esté corriendo.`,
    )
  } finally {
    if (timeout) clearTimeout(timeout)
  }

  if (!response.ok) {
    let message = `[HTTP ${response.status}] Error desconocido en el servidor.`
    try {
      const texto = await response.text()
      if (texto) {
        try {
          const data = JSON.parse(texto)
          if (typeof data === "string" && data) {
            message = `[HTTP ${response.status}] ${data}`
          } else if (data?.detalle) {
            message = `[HTTP ${response.status}] ${data.detalle}`
          } else if (data?.detail) {
            message = `[HTTP ${response.status}] ${data.detail}`
          } else if (data?.error) {
            message = `[HTTP ${response.status}] ${data.error}`
          } else if (data?.message) {
            message = `[HTTP ${response.status}] ${data.message}`
          } else {
            message = `[HTTP ${response.status}] ${JSON.stringify(data)}`
          }
        } catch {
          message = `[HTTP ${response.status}] ${texto}`
        }
      }
    } catch {
      /* la respuesta no era legible */
    }
    throw new Error(message)
  }

  if (tipoRespuesta === "blob") return response.blob()

  const texto = await response.text()
  if (!texto) return null
  try {
    return JSON.parse(texto)
  } catch {
    return texto
  }
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
  return request("/procesar", { method: "POST", body: formData }, "json", ANALISIS_TIMEOUT_MS)
}

export async function descargarReporteWord(analisisId, matriculaFmi) {
  const blob = await request(`/descargar-word/${analisisId}`, { method: "GET" }, "blob")
  const nombre = matriculaFmi
    ? `Informe_Juridico_Predial_FMI_${matriculaFmi}.docx`
    : `Informe_Juridico_Predial_${analisisId}.docx`
  const url = URL.createObjectURL(blob)
  const link = document.createElement("a")
  link.href = url
  link.download = nombre
  document.body.appendChild(link)
  link.click()
  link.remove()
  setTimeout(() => URL.revokeObjectURL(url), 1500)
}

export async function descargarAnexoTractoWord(analisisId, matriculaFmi) {
  const blob = await request(`/descargar-anexo-tracto/${analisisId}`, { method: "GET" }, "blob")
  const nombre = matriculaFmi
    ? `Anexo_Tracto_Sucesivo_FMI_${matriculaFmi}.docx`
    : `Anexo_Tracto_Sucesivo_${analisisId}.docx`
  const url = URL.createObjectURL(blob)
  const link = document.createElement("a")
  link.href = url
  link.download = nombre
  document.body.appendChild(link)
  link.click()
  link.remove()
  setTimeout(() => URL.revokeObjectURL(url), 1500)
}

export async function procesarAnalisisCtlStreaming({
  archivoPdf,
  matriculaFmi,
  proposito = "CompraVenta",
  tipoDocumento = "CTL",
  empresaId = EMPRESA_ID,
  usuarioId = USUARIO_ID,
  onProgreso,
}) {
  const formData = new FormData()
  formData.append("archivoPdf", archivoPdf)
  formData.append("matriculaFmi", matriculaFmi ?? "")
  formData.append("proposito", proposito)
  formData.append("tipoDocumento", tipoDocumento)
  formData.append("empresaId", empresaId)
  formData.append("usuarioId", usuarioId)

  const response = await fetch(`${API_BASE}/procesar-sse`, conToken({
    method: "POST",
    body: formData,
  }))

  if (!response.ok || !response.body) {
    throw new Error(`[HTTP ${response.status}] No se pudo iniciar el análisis en streaming.`)
  }

  const reader = response.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ""

  while (true) {
    const { done, value } = await reader.read()
    if (done) break
    buffer += decoder.decode(value, { stream: true })

    let idx
    while ((idx = buffer.indexOf("\n\n")) !== -1) {
      const bloque = buffer.slice(0, idx)
      buffer = buffer.slice(idx + 2)
      const evento = parsearBloqueSse(bloque)
      if (!evento) continue

      if (evento.tipo === "progreso") {
        onProgreso?.(evento.datos)
      } else if (evento.tipo === "resultado") {
        return evento.datos
      } else if (evento.tipo === "error") {
        throw new Error(evento.datos?.detalle || "Error durante el análisis.")
      }
    }
  }

  throw new Error("La conexión de streaming finalizó sin resultado.")
}

function parsearBloqueSse(bloque) {
  let tipo = ""
  let data = ""
  for (const linea of bloque.split("\n")) {
    if (linea.startsWith("event:")) tipo = linea.slice(6).trim()
    else if (linea.startsWith("data:")) data = linea.slice(5).trim()
  }
  if (!tipo || !data) return null
  try {
    return { tipo, datos: JSON.parse(data) }
  } catch {
    return null
  }
}

// --- Autenticación ---

export function iniciarGoogle(returnUrl) {
  const backendUrl = getBackendBaseUrl()
  window.location.href = `${backendUrl}/api/auth/login-google${returnUrl ? `?returnUrl=${encodeURIComponent(returnUrl)}` : ""}`
}

export function iniciarMicrosoft(returnUrl) {
  const backendUrl = getBackendBaseUrl()
  window.location.href = `${backendUrl}/api/auth/login-microsoft${returnUrl ? `?returnUrl=${encodeURIComponent(returnUrl)}` : ""}`
}

export const solicitarOtp = (email) =>
  authRequest("/solicitar-otp", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email }),
  })

export const verificarOtp = async (payload) => {
  const res = await authRequest("/verificar-otp", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  })
  if (res?.token) guardarToken(res.token)
  return res
}

export async function obtenerUsuarioActual() {
  const tokenUrl = capturarTokenDeUrl()
  if (tokenUrl) guardarToken(tokenUrl)
  if (!obtenerToken()) return null
  try {
    return await authRequest("/me")
  } catch {
    return null
  }
}

export const cerrarSesion = async () => {
  const token = obtenerToken()
  try {
    if (token) await authRequest("/logout", { method: "POST" })
  } finally {
    guardarToken(null)
  }
}

export async function enviarReporteWord(analisisId) {
  return request(`/envia-word/${analisisId}`, { method: "POST" }, "json", 0)
}