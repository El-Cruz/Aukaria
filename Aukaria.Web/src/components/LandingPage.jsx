import { useReducedMotion } from "framer-motion"
import { motion } from "framer-motion"
import LanguageSelector from "./LanguageSelector"
import PdfUploadZone from "./PdfUploadZone"

const spring = { type: "spring", bounce: 0, duration: 0.4 }
const fadeUp = { duration: 0.5, ease: [0.22, 0.61, 0.36, 1] }

function MonoLabel({ children, className = "" }) {
  return (
    <span
      className={`font-mono text-[10px] font-bold uppercase tracking-[0.25em] text-neutral-400 ${className}`}
    >
      {children}
    </span>
  )
}

function SectionHeader({ label, title, subtitle }) {
  return (
    <div className="mx-auto max-w-2xl text-center">
      <MonoLabel>{label}</MonoLabel>
      <h2 className="mt-3 text-3xl font-black tracking-tight text-black md:text-4xl">{title}</h2>
      {subtitle && (
        <p className="mx-auto mt-3 max-w-xl text-base text-neutral-600">{subtitle}</p>
      )}
    </div>
  )
}

function Reveal({ children, delay = 0, className = "" }) {
  const reduce = useReducedMotion()
  return (
    <motion.div
      className={className}
      initial={reduce ? { opacity: 0 } : { opacity: 0, y: 24 }}
      whileInView={reduce ? { opacity: 1 } : { opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-80px" }}
      transition={{ ...fadeUp, delay }}
    >
      {children}
    </motion.div>
  )
}

const NAV_LINKS = [
  { id: "inicio", label: "Inicio" },
  { id: "caracteristicas", label: "Características" },
  { id: "como-funciona", label: "Cómo Funciona" },
  { id: "planes", label: "Planes" },
]

const METRICAS = [
  { valor: "< 500 ms", etiqueta: "Extracción de Matrícula FMI" },
  { valor: "100%", etiqueta: "Detección de Gravámenes y Cautelares" },
  { valor: ".docx", etiqueta: "Exportación Oficial Editable" },
]

const PASOS = [
  {
    nro: "01",
    titulo: "Carga de Documentación Registral (CTL, VUR o Escrituras)",
    descripcion: "Sin guardar archivos desprotegidos. El folio se procesa en memoria y con cifrado de extremo a extremo.",
  },
  {
    nro: "02",
    titulo: "Diagnóstico Jurídico Automatizado",
    descripcion: "Evaluación de tracto sucesivo, cargas y gravámenes con semaforización de riesgos y rigor normativo colombiano.",
  },
  {
    nro: "03",
    titulo: "Emisión de Dictamen en Word / PDF",
    descripcion: "Estructura editable lista para la revisión profesional y firma del abogado colegiado.",
  },
]

const FEATURES = [
  {
    titulo: "Matriz de Anotaciones",
    descripcion: "Filtros inteligentes por naturaleza, fecha y vigencia. Cautelares y gravámenes detectados al instante.",
  },
  {
    titulo: "Semáforo de Viabilidad",
    descripcion: "Viable, Requiere Revisión o Alerta Crítica. Lectura ejecutiva del estado jurídico del folio en tiempo real.",
  },
  {
    titulo: "Ley 1274 de 2009 y Banca",
    descripcion: "Verificación de servidumbres, tracto sucesivo de 20 años y estudios bancarios con el rigor exigido.",
  },
]

function FeatureIcon({ index }) {
  const common = {
    className: "h-6 w-6 text-black",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.75,
    strokeLinecap: "round",
    strokeLinejoin: "round",
  }
  if (index === 0) {
    return (
      <svg viewBox="0 0 24 24" {...common}>
        <rect x="3" y="4" width="18" height="16" rx="2" />
        <path d="M7 9h4M14 13h3M7 13h3M14 9h-1" />
      </svg>
    )
  }
  if (index === 1) {
    return (
      <svg viewBox="0 0 24 24" {...common}>
        <circle cx="12" cy="12" r="9" />
        <path d="M12 7v5l3 2" />
      </svg>
    )
  }
  return (
    <svg viewBox="0 0 24 24" {...common}>
      <path d="M4 19V9m5 10V5m5 14v-7m5 7V3" />
    </svg>
  )
}

export default function LandingPage({ onAnalyze = () => {}, onLogin = () => {} }) {
  const reduce = useReducedMotion()

  const scrollToSection = (id) => (event) => {
    event.preventDefault()
    document.getElementById(id)?.scrollIntoView({
      behavior: reduce ? "auto" : "smooth",
      block: "start",
    })
  }

  return (
    <div className="relative">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 h-[38rem] bg-[radial-gradient(58%_46%_at_50%_0%,rgba(0,0,0,0.05),transparent)]"
      />

      <motion.header
        initial={{ y: -24, opacity: 0 }}
        animate={{ y: 0, opacity: 1 }}
        transition={spring}
        className="sticky top-0 z-50 border-b border-black/10 bg-white/70 backdrop-blur-2xl"
      >
        <nav className="mx-auto flex h-16 max-w-6xl items-center gap-3 px-4 md:px-8">
          <a
            href="#inicio"
            onClick={scrollToSection("inicio")}
            className="flex items-baseline gap-2"
            aria-label="Aukaria — inicio"
          >
            <span className="text-xl font-black tracking-tighter text-black">AUKARIA</span>
            <span className="rounded border border-black/20 px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-widest text-neutral-500">
              B2B SaaS
            </span>
          </a>

          <div className="ml-auto hidden items-center gap-7 md:flex">
            {NAV_LINKS.map((link) => (
              <a
                key={link.id}
                href={`#${link.id}`}
                onClick={scrollToSection(link.id)}
                className="text-sm font-medium text-neutral-600 transition-colors duration-150 hover:text-black"
              >
                {link.label}
              </a>
            ))}
          </div>

          <div className="ml-auto flex items-center gap-3 md:ml-0">
            <LanguageSelector />
            <button
              type="button"
              onClick={onLogin}
              className="hidden rounded-full border border-black/10 bg-white/60 px-4 py-2 text-sm font-semibold text-neutral-700 backdrop-blur-xl transition-colors duration-150 hover:bg-white hover:text-black sm:block"
            >
              Iniciar Sesión
            </button>
            </div>
        </nav>
      </motion.header>

      <main id="inicio">
        <section className="mx-auto w-full max-w-6xl px-4 pb-16 pt-14 md:px-8 md:pt-20">
          <motion.div
            initial={reduce ? { opacity: 0 } : { opacity: 0, y: 20 }}
            animate={reduce ? { opacity: 1 } : { opacity: 1, y: 0 }}
            transition={{ ...fadeUp, delay: 0.05 }}
            className="text-center"
          >
            <span className="inline-flex items-center gap-2 rounded-full border border-black/10 bg-white/60 px-4 py-1.5 font-mono text-[11px] font-medium text-neutral-600 backdrop-blur-xl">
              <span className="relative flex h-2 w-2">
                <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-400 opacity-75" />
                <span className="relative inline-flex h-2 w-2 rounded-full bg-emerald-500" />
              </span>
              Potenciado por Metodología y Parámetros Avalados por Profesionales en Derecho Inmobiliario
            </span>

            <h1 className="mx-auto mt-6 max-w-4xl text-5xl font-black leading-none tracking-tight text-black md:text-7xl">
              Estudios Prediales y Diagnóstico Jurídico en <span className="text-neutral-400">Segundos.</span>
            </h1>

            <p className="mx-auto mt-5 max-w-2xl text-lg text-neutral-600">
              Automatiza la lectura y validación de Certificados de Tradición (CTL), Escrituras y VUR.
              Identifica gravámenes, afectaciones y alertas patrimoniales con precisión técnica.
            </p>
          </motion.div>

          <motion.div
            initial={reduce ? { opacity: 0 } : { opacity: 0, y: 28 }}
            animate={reduce ? { opacity: 1 } : { opacity: 1, y: 0 }}
            transition={{ ...fadeUp, delay: 0.15 }}
            className="mx-auto mt-10 max-w-2xl rounded-3xl border border-black/10 bg-white/80 p-6 shadow-2xl backdrop-blur-3xl md:p-8"
          >
            <div className="mb-6 flex items-center gap-3">
              <span className="flex h-8 w-8 items-center justify-center rounded-full bg-black text-xs font-bold text-white">
                CTL
              </span>
              <p className="font-mono text-[10px] font-bold uppercase tracking-[0.25em] text-neutral-500">
                Prueba el pre-análisis — sin registro
              </p>
            </div>
            <PdfUploadZone onAnalyze={onAnalyze} />
          </motion.div>

          <Reveal delay={0.2} className="mx-auto mt-14 max-w-3xl">
            <div className="grid grid-cols-1 gap-px overflow-hidden rounded-3xl border border-black/10 bg-black/10 backdrop-blur-2xl sm:grid-cols-3">
              {METRICAS.map((metrica) => (
                <div
                  key={metrica.etiqueta}
                  className="flex flex-col items-center gap-1.5 bg-white/80 px-6 py-6 text-center backdrop-blur-3xl"
                >
                  <span className="font-mono text-2xl font-black tracking-tight text-black">
                    {metrica.valor}
                  </span>
                  <span className="font-mono text-[10px] font-bold uppercase tracking-[0.2em] text-neutral-500">
                    {metrica.etiqueta}
                  </span>
                </div>
              ))}
            </div>
          </Reveal>
        </section>

        <section id="como-funciona" className="mx-auto w-full max-w-6xl scroll-mt-20 px-4 py-16 md:px-8">
          <Reveal>
            <SectionHeader
              label="Cómo Funciona"
              title="Del folio al dictamen en tres pasos"
              subtitle="Un flujo cerrado pensado para notarías, bancos, y equipos jurídicos. Sin citas ni colas en el registro."
            />
          </Reveal>

          <div className="mt-12 grid grid-cols-1 gap-5 md:grid-cols-3">
            {PASOS.map((paso, i) => (
              <Reveal key={paso.nro} delay={i * 0.08}>
                <motion.article
                  whileHover={reduce ? undefined : { y: -6 }}
                  transition={spring}
                  className="group relative h-full overflow-hidden rounded-3xl border border-black/10 bg-white/70 p-7 backdrop-blur-3xl"
                >
                  <div className="flex items-baseline gap-3">
                    <span className="font-mono text-sm font-black tracking-tight text-neutral-400">
                      {paso.nro}
                    </span>
                    <span
                      aria-hidden
                      className="h-px flex-1 bg-black/10 transition-colors duration-300 group-hover:bg-black/30"
                    />
                  </div>
                  <h3 className="mt-5 text-xl font-bold tracking-tight text-black">
                    {paso.titulo}
                  </h3>
                  <p className="mt-2 text-sm leading-relaxed text-neutral-600">
                    {paso.descripcion}
                  </p>
                  <span
                    aria-hidden
                    className="pointer-events-none absolute -right-10 -top-10 font-mono text-[7rem] font-black leading-none text-black/[0.03]"
                  >
                    {paso.nro}
                  </span>
                </motion.article>
              </Reveal>
            ))}
          </div>
        </section>

        <section id="caracteristicas" className="mx-auto w-full max-w-6xl scroll-mt-20 px-4 py-16 md:px-8">
          <Reveal>
            <SectionHeader
              label="Características"
              title="Herramientas de precisión para el estudio de títulos"
              subtitle="Diseñado para acelerar revisiones jurídicas sin sacrificar el detalle que exige la ley."
            />
          </Reveal>

          <div className="mt-12 grid grid-cols-1 gap-5 md:grid-cols-3">
            {FEATURES.map((feature, i) => (
              <Reveal key={feature.titulo} delay={i * 0.08}>
                <motion.article
                  whileHover={reduce ? undefined : { y: -6 }}
                  transition={spring}
                  className="group relative h-full overflow-hidden rounded-3xl border border-black/10 bg-white/70 p-7 backdrop-blur-3xl"
                >
                  <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-black/5 transition-colors duration-300 group-hover:bg-black/10">
                    <FeatureIcon index={i} />
                  </div>
                  <h3 className="mt-5 text-lg font-bold tracking-tight text-black">
                    {feature.titulo}
                  </h3>
                  <p className="mt-2 text-sm leading-relaxed text-neutral-600">
                    {feature.descripcion}
                  </p>
                </motion.article>
              </Reveal>
            ))}
          </div>
        </section>

        <section id="planes" className="mx-auto w-full max-w-6xl scroll-mt-20 px-4 py-16 md:px-8">
          <Reveal>
            <div className="relative overflow-hidden rounded-3xl border border-black/10 bg-white/80 px-6 py-14 text-center shadow-xl backdrop-blur-3xl md:px-10">
              <div
                aria-hidden
                className="pointer-events-none absolute inset-x-0 top-0 h-40 bg-[radial-gradient(60%_100%_at_50%_0%,rgba(0,0,0,0.06),transparent)]"
              />
              <MonoLabel>Planes</MonoLabel>
              <h2 className="mx-auto mt-3 max-w-xl text-3xl font-black tracking-tight text-black md:text-4xl">
                Empieza a auditar predios hoy
              </h2>
              <p className="mx-auto mt-4 max-w-lg text-base text-neutral-600">
                Planes por volumen de análisis para empresas, notarías e instituciones. Escala con
                créditos de diagnóstico jurídico, sin contratos rígidos.
              </p>
              <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
                <button
                  type="button"
                  onClick={onLogin}
                  className="inline-flex items-center gap-1.5 rounded-full bg-black px-7 py-3 text-sm font-semibold text-white shadow-[0_4px_16px_rgba(0,0,0,0.25)] transition-colors duration-150 hover:bg-neutral-900"
                >
                  Probar Gratis <span aria-hidden>→</span>
                </button>
                <a
                  href="#inicio"
                  onClick={scrollToSection("inicio")}
                  className="rounded-full border border-black/10 bg-white/60 px-7 py-3 text-sm font-semibold text-neutral-700 transition-colors duration-150 hover:bg-white"
                >
                  Hablar con Ventas
                </a>
              </div>
            </div>
          </Reveal>
        </section>
      </main>

      <footer className="border-t border-black/10 bg-white/60 backdrop-blur-2xl">
        <div className="mx-auto flex w-full max-w-6xl flex-col items-center gap-6 px-4 py-10 md:flex-row md:gap-8 md:px-8">
          <div className="flex items-baseline gap-2">
            <span className="text-lg font-black tracking-tighter text-black">AUKARIA</span>
            <span className="rounded border border-black/20 px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-widest text-neutral-500">
              B2B SaaS
            </span>
          </div>

          <nav className="flex flex-wrap items-center justify-center gap-6 text-sm text-neutral-500">
            <a href="#planes" onClick={scrollToSection("planes")} className="transition-colors duration-150 hover:text-black">
              Términos
            </a>
            <a href="#planes" onClick={scrollToSection("planes")} className="transition-colors duration-150 hover:text-black">
              Privacidad
            </a>
            <a href="#planes" onClick={scrollToSection("planes")} className="transition-colors duration-150 hover:text-black">
              Seguridad
            </a>
            <a href="#planes" onClick={scrollToSection("planes")} className="transition-colors duration-150 hover:text-black">
              Contacto
            </a>
          </nav>

          <div className="flex items-center gap-2 md:ml-auto">
            <span className="relative flex h-2 w-2">
              <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-400 opacity-75" />
              <span className="relative inline-flex h-2 w-2 rounded-full bg-emerald-500" />
            </span>
            <span className="font-mono text-xs font-medium text-neutral-500">Servicio Operativo</span>
          </div>
        </div>
        <div className="border-t border-black/5 py-4 text-center text-xs text-neutral-400">
          © 2026 Aukaria. Todos los derechos reservados.
        </div>
      </footer>
    </div>
  )
}