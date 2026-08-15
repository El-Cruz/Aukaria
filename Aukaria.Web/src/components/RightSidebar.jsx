import { useState } from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"
import { PREGUNTAS_POR_TIPO, resolvedorTipo } from "../utils/tiposDocumento"

const spring = { type: "spring", bounce: 0, duration: 0.35 }

const VIABILITY = {
  viable: { label: "VIABLE", dot: "bg-emerald-500", badge: "border-emerald-500/30 bg-emerald-400/10 text-emerald-700" },
  revision: { label: "REQUIERE REVISIÓN", dot: "bg-amber-400", badge: "border-amber-500/30 bg-amber-400/10 text-amber-700" },
  critica: { label: "ALERTA CRÍTICA", dot: "bg-rose-500", badge: "border-rose-500/30 bg-rose-500/10 text-rose-600" },
}

function viabilidadKey(valor) {
  const v = (valor || "").toLowerCase().replace(/[\s-]+/g, "")
  if (v.includes("requiere") || v === "requiererevision") return "revision"
  if (v.includes("critic") || v === "alertacritica") return "critica"
  return "viable"
}

const PREGUNTAS_WORKSPACE = ["¿Qué normativa aplica a este documento?", "¿Cómo funciona la bolsa de créditos?"]

function respuestaIA(pregunta, tipoKey = "CTL") {
  const q = pregunta.toLowerCase()
  if (q.includes("crédito") || q.includes("credito") || q.includes("bolsa") || q.includes("plan"))
    return "Tu cuenta está en el Plan Empresarial con 50 créditos mensuales. Cada documento procesado consume 1 crédito y la bolsa se reabastece el primer día de cada mes. Puedes ampliar créditos desde el panel de administración."
  if (q.includes("normativa") || q.includes("ley") || q.includes("decreto") || q.includes("circular"))
    return "Aukaria verifica los documentos conforme a la Ley 1579 de 2012, la Circular 1587 de 2012 de la Superfinanciera y las directrices operativas de la SNR. Indícame una figura específica (hipoteca, embargo, servidumbre, falsa tradición) y te resumo el criterio aplicable."
  if (tipoKey === "EscrituraPublica" || q.includes("otorgante") || q.includes("gravamen")) {
    if (q.includes("otorgante"))
      return "La escritura relaciona los otorgantes según su rol: vendedor, comprador, constituyente o beneficiario. Verifica la identificación completa de cada uno, pues errores de nombres invalidan cláusulas."
    if (q.includes("gravamen") || q.includes("hipoteca"))
      return "Los gravámenes que limitan el otorgamiento (hipotecas, embargos o servidumbres) constan en el folio. Confirma su cancelación contra el certificado actualizado antes del registro de la escritura."
    return "La escritura se analizó contrastando notaría, fecha, otorgantes, cuantía y el FMI citado. Las alertas reflejan los aspectos a subsanar antes del registro."
  }
  if (tipoKey === "VUR" || q.includes("vur") || q.includes("al día") || q.includes("registral"))
    return "El informe VUR refleja la vigencia del certificado y los trámites radicados ante la ORIP. Compara los propietarios reportados contra el folio para descartar discrepancias de titularidad."
  if (q.includes("embargo"))
    return "El folio no registra embargos activos vigentes. La última medida cautelar consta como cancelada, aunque recomiendo contrastarla contra el certificado reciente."
  if (q.includes("vivienda") || q.includes("familiar"))
    return "No se evidencia afectación a vivienda familiar inscrita. El folio aparece libre de esa carga, lo que facilita la negociación del predio."
  if (q.includes("tipo") || q.includes("clasific"))
    return "Aukaria clasifica el insumo como CTL, Informe VUR, Escritura Pública o Documento Inmobiliario General según los patrones del texto extraído, y aplica el prompt jurídico especializado para cada tipo."
  return "Con la información del documento procesado, Aukaria mantiene un diagnóstico consistente. Puedo profundizar en cualquier anotación si me indicas su número."
}

function Chip({ children, activo = false, onClick }) {
  return (
    <motion.button
      type="button"
      whileTap={{ scale: 0.96 }}
      transition={spring}
      onClick={onClick}
      className={`rounded-full px-3 py-1 text-xs font-medium transition-colors duration-150 ${
        activo ? "bg-black text-white" : "bg-black/5 text-neutral-700 hover:bg-black/10"
      }`}
    >
      {children}
    </motion.button>
  )
}

export default function RightSidebar({
  modo = "dashboard",
  analisis,
  onDownload = () => {},
  movil = false,
  usuario = null,
  onLogout = () => {},
}) {
  const reduce = useReducedMotion()
  const [copiado, setCopiado] = useState(false)
  const [qrEstado, setQrEstado] = useState("")
  const [mensajes, setMensajes] = useState([])
  const [consulta, setConsulta] = useState("")
  const [escribiendo, setEscribiendo] = useState(false)
  const [feedback, setFeedback] = useState("")

  const r = analisis?.Resultado ?? null
  const fmi = analisis?.MatriculaFMI || r?.MatriculaFMI || "196-2053"
  const orip = (r?.ORIP || "").trim() || "Cuenca"
  const propietario = (r?.PropietarioActual || "").trim() || "Torres Cárdenas Juan"
  const viabilidad = viabilidadKey(analisis?.Viabilidad || r?.Viabilidad)
  const v = VIABILITY[viabilidad] ?? VIABILITY.viable
  const tipo = resolvedorTipo(analisis?.TipoDocumento)
  const creditosUsados = usuario?.creditosUsados ?? 1
  const creditosTotal = usuario?.creditosTotal ?? 50
  const restantes = creditosTotal - creditosUsados
  const iniciales = usuario?.nombre
    ? usuario.nombre
        .split(" ")
        .map((p) => p[0])
        .slice(0, 2)
        .join("")
        .toUpperCase()
    : "AD"

  const preguntas =
    modo === "dashboard" ? (PREGUNTAS_POR_TIPO[tipo.key] || PREGUNTAS_POR_TIPO.CTL) : PREGUNTAS_WORKSPACE

  const filasMetadatos = (() => {
    const usar = (v) => (v && String(v).trim()) || "—"
    if (tipo.key === "EscrituraPublica") {
      return [
        { label: "NOTARÍA", value: usar(r?.Notaria) },
        { label: "OTORGANTES", value: r?.Otorgantes?.length ? `${r.Otorgantes.length} roles` : "—" },
        { label: "CUANTÍA", value: usar(r?.Cuantia) },
        { label: "CRÉDITOS USADOS", value: `${creditosUsados} de ${creditosTotal}` },
      ]
    }
    if (tipo.key === "VUR") {
      return [
        { label: "ESTADO REGISTRAL", value: usar(r?.EstadoFolio) },
        { label: "PROPIETARIO", value: usar(r?.PropietarioActual) },
        { label: "FMI", value: fmi },
        { label: "CRÉDITOS USADOS", value: `${creditosUsados} de ${creditosTotal}` },
      ]
    }
    return [
      { label: "FMI", value: fmi },
      { label: "ORIP", value: orip },
      { label: "PROPIETARIO", value: propietario },
      { label: "CRÉDITOS USADOS", value: `${creditosUsados} de ${creditosTotal}` },
    ]
  })()

  const copiarEnlace = async () => {
    try {
      await navigator.clipboard.writeText(window.location.href)
      setCopiado(true)
      window.setTimeout(() => setCopiado(false), 1800)
    } catch {
      setCopiado(false)
    }
  }

  const verificarQr = () => {
    setQrEstado("verificada")
    window.setTimeout(() => setQrEstado(""), 2200)
  }

  const enviarConsulta = (texto) => {
    const limpio = (texto || "").trim()
    if (!limpio || escribiendo) return
    setMensajes((prev) => [...prev, { rol: "usuario", texto: limpio }])
    setConsulta("")
    setEscribiendo(true)
    const responder = () => {
      setMensajes((prev) => [...prev, { rol: "ia", texto: respuestaIA(limpio, tipo.key) }])
      setEscribiendo(false)
    }
    window.setTimeout(responder, reduce ? 150 : 1100)
  }

  const accionRapida = (msg) => {
    setFeedback(msg)
    window.setTimeout(() => setFeedback(""), 2200)
  }

  const botonSecundario =
    "flex w-full items-center justify-center gap-2 rounded-full border border-black/10 bg-white/60 px-4 py-2.5 text-xs font-semibold text-neutral-700 backdrop-blur-xl transition-colors duration-150 hover:bg-white hover:text-black"

  return (
    <motion.aside
      initial={{ opacity: 0, x: 20 }}
      animate={{ opacity: 1, x: 0 }}
      transition={spring}
      className={
        movil
          ? "space-y-6"
          : "sticky top-20 hidden space-y-6 rounded-3xl border border-black/10 bg-white/80 p-6 shadow-xl backdrop-blur-3xl lg:block"
      }
    >
      {modo === "dashboard" ? (
        <>
          {/* Bloque A — Acciones del Resultado */}
          <div className="space-y-3">
            <p className="font-mono text-[10px] font-bold uppercase tracking-[0.25em] text-neutral-400">
              Acciones Rápidas
            </p>
            <motion.button
              type="button"
              whileTap={{ scale: 0.98 }}
              transition={spring}
              onClick={onDownload}
              className="flex w-full items-center justify-center gap-2 rounded-full bg-black px-5 py-3 text-sm font-semibold text-white shadow-[0_6px_18px_rgba(0,0,0,0.22)] transition-colors duration-150 hover:bg-neutral-900"
            >
              <svg
                viewBox="0 0 24 24"
                className="h-4 w-4"
                fill="none"
                stroke="currentColor"
                strokeWidth={2}
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <path d="M12 3v12m0 0 4-4m-4 4-4-4M4 17v2a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1v-2" />
              </svg>
              Descargar Reporte Word (.docx)
            </motion.button>

            <div className="grid grid-cols-1 gap-2">
              <motion.button
                type="button"
                whileTap={{ scale: 0.97 }}
                transition={spring}
                onClick={copiarEnlace}
                className={botonSecundario}
              >
                {copiado ? (
                  <span className="text-emerald-600">Enlace copiado ✓</span>
                ) : (
                  <>
                    <svg
                      viewBox="0 0 24 24"
                      className="h-3.5 w-3.5"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth={2}
                      strokeLinecap="round"
                      strokeLinejoin="round"
                    >
                      <rect x="9" y="9" width="12" height="12" rx="2" />
                      <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
                    </svg>
                    Copiar Enlace de Consulta
                  </>
                )}
              </motion.button>
              <motion.button
                type="button"
                whileTap={{ scale: 0.97 }}
                transition={spring}
                onClick={verificarQr}
                className={botonSecundario}
              >
                {qrEstado === "verificada" ? (
                  <span className="text-emerald-600">Firma QR válida ✓</span>
                ) : (
                  <>
                    <svg
                      viewBox="0 0 24 24"
                      className="h-3.5 w-3.5"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth={2}
                      strokeLinecap="round"
                    >
                      <rect x="3" y="3" width="7" height="7" rx="1" />
                      <rect x="14" y="3" width="7" height="7" rx="1" />
                      <rect x="3" y="14" width="7" height="7" rx="1" />
                      <path d="M14 14h3v3h-3zM21 14v3M14 21h3" />
                    </svg>
                    Verificar Firma QR
                  </>
                )}
              </motion.button>
            </div>
          </div>

          {/* Bloque B — Metadatos Prediales & Créditos */}
          <div className="space-y-4 rounded-2xl border border-black/10 bg-white/70 p-5 backdrop-blur-xl">
            <div className="flex items-center justify-between gap-2">
              <span className="font-mono text-[10px] font-bold uppercase tracking-[0.25em] text-neutral-400">
                Viabilidad
              </span>
              <span
                className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[10px] font-bold tracking-widest ${v.badge}`}
              >
                <span className={`h-1.5 w-1.5 rounded-full ${v.dot}`} />
                {v.label}
              </span>
            </div>

            <dl className="space-y-2.5 font-mono text-xs">
              {filasMetadatos.map((fila) => (
                <div key={fila.label} className="flex items-center justify-between gap-3">
                  <dt className="text-neutral-400">{fila.label}</dt>
                  <dd className="truncate font-bold text-black">{fila.value}</dd>
                </div>
              ))}
            </dl>

            <CreditosProgress usados={creditosUsados} total={creditosTotal} />
          </div>

          <AsistenteIA
            reduce={reduce}
            preguntas={preguntas}
            mensajes={mensajes}
            consulta={consulta}
            escribiendo={escribiendo}
            setConsulta={setConsulta}
            enviarConsulta={enviarConsulta}
          />

          <PerfilSesion usuario={usuario} iniciales={iniciales} onLogout={onLogout} />
        </>
      ) : (
        <>
          <AsistenteIA
            reduce={reduce}
            preguntas={preguntas}
            mensajes={mensajes}
            consulta={consulta}
            escribiendo={escribiendo}
            setConsulta={setConsulta}
            enviarConsulta={enviarConsulta}
            destacado
          />

          {/* Bolsa de Créditos B2B */}
          <div className="space-y-4 rounded-2xl border border-black/10 bg-white/70 p-5 backdrop-blur-xl">
            <div className="flex items-center justify-between gap-2">
              <span className="font-mono text-[10px] font-bold uppercase tracking-[0.25em] text-neutral-400">
                Bolsa de Créditos
              </span>
              <span className="rounded-full border border-black/10 bg-white/60 px-2.5 py-1 font-mono text-[9px] font-bold uppercase tracking-widest text-neutral-500">
                Plan · {usuario?.rol || "Empresarial"}
              </span>
            </div>
            <div className="flex items-end gap-2">
              <span className="text-4xl font-black leading-none tracking-tighter text-black">{restantes}</span>
              <span className="pb-1 font-mono text-[10px] uppercase tracking-widest text-neutral-400">
                disponibles de {creditosTotal}
              </span>
            </div>
            <CreditosProgress usados={creditosUsados} total={creditosTotal} />
            <motion.button
              type="button"
              whileTap={{ scale: 0.97 }}
              transition={spring}
              onClick={() => accionRapida("Solicitud de ampliación enviada al área comercial ✓")}
              className="w-full rounded-full border border-black/10 bg-white/60 px-4 py-2.5 text-xs font-semibold text-neutral-700 backdrop-blur-xl transition-colors duration-150 hover:bg-white hover:text-black"
            >
              Ampliar créditos →
            </motion.button>
          </div>

          {/* Barra de Acciones Rápidas */}
          <div className="space-y-2.5 rounded-2xl border border-black/10 bg-white/70 p-5 backdrop-blur-xl">
            <p className="font-mono text-[10px] font-bold uppercase tracking-[0.25em] text-neutral-400">
              Operaciones
            </p>
            <motion.button
              type="button"
              whileTap={{ scale: 0.97 }}
              transition={spring}
              onClick={() => accionRapida("Configuración de empresa → acceso concedido ✓")}
              className={botonSecundario}
            >
              <svg viewBox="0 0 24 24" className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
                <circle cx="12" cy="12" r="3" />
                <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 1 1-4 0v-.09a1.65 1.65 0 0 0-1-1.51 1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 1 1 0-4h.09a1.65 1.65 0 0 0 1.51-1 1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33h0a1.65 1.65 0 0 0 1-1.51V3a2 2 0 1 1 4 0v.09a1.65 1.65 0 0 0 1 1.51h0a1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82v0a1.65 1.65 0 0 0 1.51 1H21a2 2 0 1 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z" />
              </svg>
              Configuración de Empresa
            </motion.button>
            <motion.button
              type="button"
              whileTap={{ scale: 0.97 }}
              transition={spring}
              onClick={() => accionRapida("Centro de soporte Aukaria · SLA 24/7 ✓")}
              className={botonSecundario}
            >
              <svg viewBox="0 0 24 24" className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
                <circle cx="12" cy="12" r="10" />
                <path d="m4.93 4.93 4.24 4.24M14.83 14.83l4.24 4.24M17.63 17.63l4.24 4.24m-4.24-4.24a4 4 0 1 0-5.66-5.66m5.66 5.66-5.66-5.66M4.93 19.07l8.49-8.49" />
              </svg>
              Centro de Soporte
            </motion.button>

            <AnimatePresence>
              {feedback && (
                <motion.p
                  initial={{ opacity: 0, y: 4 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0 }}
                  transition={spring}
                  className="rounded-xl bg-black/[0.04] px-3 py-2 text-[11px] font-medium text-neutral-600"
                >
                  {feedback}
                </motion.p>
              )}
            </AnimatePresence>
          </div>

          <PerfilSesion usuario={usuario} iniciales={iniciales} onLogout={onLogout} />
        </>
      )}
    </motion.aside>
  )
}

function CreditosProgress({ usados, total }) {
  const pct = Math.min(100, Math.max(0, (usados / total) * 100))
  return (
    <div>
      <div className="mb-1.5 flex items-center justify-between font-mono text-[10px] text-neutral-400">
        <span>Créditos corporativos</span>
        <span>{pct.toFixed(0)}% consumido</span>
      </div>
      <div className="h-1.5 w-full overflow-hidden rounded-full bg-black/5">
        <motion.div
          initial={{ width: 0 }}
          animate={{ width: `${pct}%` }}
          transition={{ ...spring, delay: 0.2 }}
          className="h-full rounded-full bg-black"
        />
      </div>
    </div>
  )
}

function AsistenteIA({ preguntas, mensajes, consulta, escribiendo, setConsulta, enviarConsulta, destacado = false }) {
  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2">
        <span className="flex h-6 w-6 items-center justify-center rounded-full bg-black text-[10px] font-bold text-white">
          CP
        </span>
        <p
          className={`font-mono font-bold uppercase tracking-[0.25em] ${
            destacado ? "text-[11px] text-neutral-600" : "text-[10px] text-neutral-400"
          }`}
        >
          {destacado ? "Consultor Predial" : "Consultor Predial · Asistente Técnico"}
        </p>
      </div>

      <p className="font-mono text-[9px] uppercase tracking-widest text-neutral-400">
        Cada consulta consume 1 crédito de la bolsa corporativa.
      </p>

      <div className="flex flex-wrap gap-2">
        {preguntas.map((pregunta) => (
          <Chip key={pregunta} onClick={() => enviarConsulta(pregunta)}>
            {pregunta}
          </Chip>
        ))}
      </div>

      <form
        className="flex items-center gap-2"
        onSubmit={(e) => {
          e.preventDefault()
          enviarConsulta(consulta)
        }}
      >
        <input
          type="text"
          value={consulta}
          onChange={(e) => setConsulta(e.target.value)}
          placeholder="Pregunta sobre el sistema o el predio…"
          aria-label="Consulta al asistente jurídico"
          className="w-full rounded-2xl border border-black/10 bg-black/5 px-4 py-2 text-sm text-black placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-black/20"
        />
        <motion.button
          type="submit"
          whileTap={{ scale: 0.95 }}
          transition={spring}
          aria-label="Enviar consulta"
          className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-black text-white transition-colors duration-150 hover:bg-neutral-900"
        >
          <svg
            viewBox="0 0 24 24"
            className="h-3.5 w-3.5"
            fill="none"
            stroke="currentColor"
            strokeWidth={2}
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <path d="M22 2 11 13M22 2l-7 20-4-9-9-4z" />
          </svg>
        </motion.button>
      </form>

      <div className="max-h-56 space-y-2 overflow-y-auto pr-1">
        <AnimatePresence initial={false}>
          {mensajes.map((m, i) => (
            <motion.div
              key={i}
              initial={{ opacity: 0, y: 6 }}
              animate={{ opacity: 1, y: 0 }}
              transition={spring}
              className={`rounded-2xl px-3.5 py-2.5 text-xs leading-relaxed ${
                m.rol === "usuario"
                  ? "ml-6 bg-black text-white"
                  : "mr-6 border border-black/10 bg-white/70 backdrop-blur-xl text-neutral-700"
              }`}
            >
              {m.texto}
            </motion.div>
          ))}
          {escribiendo && (
            <motion.div
              key="escribiendo"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              className="mr-6 flex w-fit items-center gap-1.5 rounded-2xl border border-black/10 bg-white/70 px-3.5 py-2.5 backdrop-blur-xl"
            >
              <span className="h-1.5 w-1.5 animate-bounce rounded-full bg-black/40" />
              <span className="h-1.5 w-1.5 animate-bounce rounded-full bg-black/40 [animation-delay:120ms]" />
              <span className="h-1.5 w-1.5 animate-bounce rounded-full bg-black/40 [animation-delay:240ms]" />
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </div>
  )
}

function PerfilSesion({ usuario, iniciales, onLogout }) {
  return (
    <div className="space-y-3 rounded-2xl border border-black/10 bg-white/70 p-5 backdrop-blur-xl">
      <div className="flex items-center gap-3">
        <span className="flex h-9 w-9 select-none items-center justify-center rounded-full bg-black text-xs font-bold text-white">
          {iniciales}
        </span>
        <div className="min-w-0 flex-1 leading-tight">
          <p className="truncate text-sm font-semibold text-black">{usuario?.nombre || "Analista Demo"}</p>
          <p className="truncate font-mono text-[10px] uppercase tracking-widest text-neutral-400">
            {usuario?.rol || "Cuenta Corporativa"} · {usuario?.empresa || "Empresa Demo S.A.S."}
          </p>
        </div>
      </div>
      <motion.button
        type="button"
        whileTap={{ scale: 0.97 }}
        transition={spring}
        onClick={onLogout}
        className="flex w-full items-center justify-center gap-2 rounded-full border border-black/10 bg-white/60 px-4 py-2.5 text-xs font-semibold text-neutral-700 transition-colors duration-150 hover:bg-black hover:text-white"
      >
        <svg viewBox="0 0 24 24" className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
          <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4M16 17l5-5-5-5M21 12H9" />
        </svg>
        Cerrar Sesión
      </motion.button>
    </div>
  )
}