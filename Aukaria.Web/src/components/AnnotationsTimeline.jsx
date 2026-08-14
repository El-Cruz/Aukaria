import { useState } from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"

const pillSpring = { type: "spring", stiffness: 400, damping: 38 }
const listSpring = { type: "spring", stiffness: 380, damping: 32 }
const expandSpring = { type: "spring", duration: 0.3, bounce: 0 }

const FILTERS = [
  { id: "todas", label: "Todas las Anotaciones" },
  { id: "gravamenes", label: "Solo Gravámenes / Hipotecas" },
  { id: "cautelares", label: "Medidas Cautelares / Embargos" },
  { id: "falsa", label: "Falsa Tradición" },
]

const ANNOTATIONS = [
  {
    nro: 1,
    fecha: "1998-03-14",
    naturaleza: "0125 - COMPRAVENTA",
    EsGravamen: false,
    EsMedidaCautelar: false,
    EsFalsaTradicion: false,
    activa: false,
    documento: "Escritura Pública N° 0418 · Notaría 3.ª de Armenia",
    de: "Sucesión Iliquida de Arturo Mejía Londoño",
    a: "María del Rosario Giraldo Vásquez",
    cuantia: "$12.000.000 COP",
    descripcion:
      "Tradición inicial del dominio. Folio abierto con la adquisición plena de la propiedad sobre el inmueble.",
  },
  {
    nro: 2,
    fecha: "2005-07-22",
    naturaleza: "0301 - HIPOTECA ABIERTA",
    EsGravamen: true,
    EsMedidaCautelar: false,
    EsFalsaTradicion: false,
    activa: false,
    documento: "Escritura Pública N° 1540 · Notaría 4.ª de Armenia",
    de: "María del Rosario Giraldo Vásquez",
    a: "Banco Andino S.A.",
    cuantia: "$180.000.000 COP",
    descripcion:
      "Constitución de hipoteca abierta de primer grado sobre el inmueble. Fue cancelada por paz y salvo (ver anotación 04).",
  },
  {
    nro: 3,
    fecha: "2010-02-09",
    naturaleza: "0404 - EMBARGO PREVENTIVO",
    EsGravamen: false,
    EsMedidaCautelar: true,
    EsFalsaTradicion: false,
    activa: true,
    documento: "Oficio N° 0231 · Juzgado 3.° Civil Municipal de Armenia",
    de: "Juzgado 3.° Civil Municipal de Armenia",
    a: "Afectación sobre el folio 196-2053",
    cuantia: "$90.000.000 COP",
    descripcion:
      "Embargo preventivo decretado con fines de remate. La medida se mantiene vigente y restringe la libre disposición del inmueble.",
  },
  {
    nro: 4,
    fecha: "2012-11-30",
    naturaleza: "0307 - CANCELACIÓN DE HIPOTECA",
    EsGravamen: false,
    EsMedidaCautelar: false,
    EsFalsaTradicion: false,
    activa: false,
    documento: "Oficio N° 0876 · Banco Andino S.A.",
    de: "Banco Andino S.A.",
    a: "María del Rosario Giraldo Vásquez",
    cuantia: "Cancelación total",
    descripcion:
      "Cancela la hipoteca abierta constituida en la anotación 02 al quedar la obligación íntegramente satisfecha.",
  },
  {
    nro: 5,
    fecha: "2019-05-18",
    naturaleza: "0125 - COMPRAVENTA",
    EsGravamen: false,
    EsMedidaCautelar: false,
    EsFalsaTradicion: true,
    activa: false,
    documento: "Escritura Pública N° 0722 · Notaría 1.ª de Calarcá",
    de: "Sucesión Iliquida de Arturo Mejía Londoño (titular no inscrito)",
    a: "Fernando Giraldo Botero",
    cuantia: "$250.000.000 COP",
    descripcion:
      "Venta celebrada por un enajenante que no figura inscrito en el folio al momento del otorgamiento. Genera riesgo de falsa tradición (art. 1871 C.C. y Ley 1579 de 2012) y exige saneamiento previo.",
  },
  {
    nro: 6,
    fecha: "2023-01-15",
    naturaleza: "0302 - HIPOTECA ABIERTA (LÍNEA DE CRÉDITO)",
    EsGravamen: true,
    EsMedidaCautelar: false,
    EsFalsaTradicion: false,
    activa: true,
    documento: "Escritura Pública N° 0310 · Notaría 2.ª de Armenia",
    de: "María del Rosario Giraldo Vásquez",
    a: "QuindíoBank S.A.",
    cuantia: "$60.000.000 COP",
    descripcion:
      "Hipoteca abierta de primer grado en garantía de una línea de crédito. Gravamen vigente que pesa sobre el folio a la fecha del certificado.",
  },
]

function dotClass(a) {
  if (a.EsFalsaTradicion) return "bg-rose-500"
  if (a.EsMedidaCautelar && a.activa) return "bg-rose-500"
  if (a.EsGravamen && a.activa) return "bg-amber-400"
  return "bg-neutral-300"
}

function AlertBadges({ a }) {
  const base = "inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[10px] font-bold"
  return (
    <div className="flex flex-wrap items-center gap-1.5">
      {a.EsGravamen && (
        <span className={`${base} border-amber-500/25 bg-amber-400/10 text-amber-700`}>
          {a.activa && <span className="h-1.5 w-1.5 rounded-full bg-amber-400" />}
          GRAVAMEN
        </span>
      )}
      {a.EsMedidaCautelar && (
        <span className={`${base} border-rose-500/25 bg-rose-500/10 text-rose-600`}>
          {a.activa && <span className="h-1.5 w-1.5 rounded-full bg-rose-500" />}
          MEDIDA CAUTELAR
        </span>
      )}
      {a.EsFalsaTradicion && (
        <span className={`${base} border-rose-500/25 bg-rose-500/10 text-rose-600`}>
          <span className="h-1.5 w-1.5 rounded-full bg-rose-500" />
          FALSA TRADICIÓN
        </span>
      )}
    </div>
  )
}

function AnnotationCard({ a }) {
  const reduce = useReducedMotion()
  const [open, setOpen] = useState(false)

  return (
    <motion.div
      role="button"
      tabIndex={0}
      aria-expanded={open}
      onClick={() => setOpen((o) => !o)}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault()
          setOpen((o) => !o)
        }
      }}
      className="cursor-pointer rounded-2xl border border-black/10 bg-white/60 p-5 text-left outline-none backdrop-blur-xl focus-visible:ring-2 focus-visible:ring-black"
    >
      <header>
        <div className="flex flex-wrap items-center gap-2">
          <span className="font-mono text-xs font-bold tracking-wider text-black">
            ANOTACIÓN #{String(a.nro).padStart(2, "0")}
          </span>
          <span className="font-mono text-xs text-neutral-500">{a.fecha}</span>
          <motion.span
            animate={{ rotate: open ? 180 : 0 }}
            transition={reduce ? { duration: 0.15 } : pillSpring}
            className="ml-auto shrink-0 text-neutral-400"
            aria-hidden
          >
            <svg viewBox="0 0 24 24" className="h-4 w-4" fill="none" stroke="currentColor" strokeWidth={2}>
              <path d="m6 9 6 6 6-6" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
          </motion.span>
        </div>
        <div className="mt-2 flex flex-wrap items-center gap-1.5">
          <span className="rounded-full border border-black/10 bg-black/[0.04] px-2.5 py-1 font-mono text-[10px] font-bold text-neutral-700">
            {a.naturaleza}
          </span>
          <AlertBadges a={a} />
        </div>
      </header>

      <AnimatePresence initial={false}>
        {open && (
          <motion.div
            key="spec"
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: "auto", opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={reduce ? { duration: 0.15 } : expandSpring}
            className="overflow-hidden"
          >
            <div className="mt-4 grid gap-3 rounded-2xl bg-black/[0.04] p-4 sm:grid-cols-2">
              {a.documento && (
                <div>
                  <p className="text-[10px] font-black uppercase tracking-[0.18em] text-neutral-400">
                    Documento soporte
                  </p>
                  <p className="mt-1 text-xs font-medium leading-relaxed text-neutral-700">
                    {a.documento}
                  </p>
                </div>
              )}
              {a.cuantia && (
                <div>
                  <p className="text-[10px] font-black uppercase tracking-[0.18em] text-neutral-400">
                    Cuantía / Valor
                  </p>
                  <p className="mt-1 font-mono text-xs font-semibold text-neutral-700">{a.cuantia}</p>
                </div>
              )}
              {a.de && (
                <div className="sm:col-span-2">
                  <p className="text-[10px] font-black uppercase tracking-[0.18em] text-neutral-400">De</p>
                  <p className="mt-1 text-xs font-medium leading-relaxed text-neutral-700">{a.de}</p>
                </div>
              )}
              {a.a && (
                <div className="sm:col-span-2">
                  <p className="text-[10px] font-black uppercase tracking-[0.18em] text-neutral-400">A</p>
                  <p className="mt-1 text-xs font-medium leading-relaxed text-neutral-700">{a.a}</p>
                </div>
              )}
              <div className="sm:col-span-2">
                <p className="text-[10px] font-black uppercase tracking-[0.18em] text-neutral-400">
                  Observaciones
                </p>
                <p className="mt-1 text-xs font-medium leading-relaxed text-neutral-600">{a.descripcion}</p>
              </div>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.div>
  )
}

export default function AnnotationsTimeline({ annotations = ANNOTATIONS }) {
  const reduce = useReducedMotion()
  const [active, setActive] = useState("todas")

  const visible = annotations.filter((a) => {
    if (active === "gravamenes") return a.EsGravamen
    if (active === "cautelares") return a.EsMedidaCautelar
    if (active === "falsa") return a.EsFalsaTradicion
    return true
  })

  return (
    <div className="flex flex-col gap-5">
      <div
        role="group"
        aria-label="Filtros de anotaciones"
        className="flex flex-wrap items-center gap-2"
      >
        {FILTERS.map((f) => {
          const isActive = active === f.id
          return (
            <motion.button
              key={f.id}
              type="button"
              aria-pressed={isActive}
              onClick={() => setActive(f.id)}
              whileTap={{ scale: 0.97 }}
              transition={pillSpring}
              className={`relative rounded-full px-3.5 py-1.5 text-xs font-semibold outline-none focus-visible:ring-2 focus-visible:ring-black ${
                isActive ? "text-white" : "text-neutral-600 hover:text-black"
              }`}
            >
              {isActive && (
                <motion.span
                  layoutId="activeFilter"
                  transition={pillSpring}
                  className="absolute inset-0 rounded-full bg-black"
                />
              )}
              <span className="relative z-10">{f.label}</span>
            </motion.button>
          )
        })}
      </div>

      <div className="relative">
        <span
          aria-hidden
          className="absolute bottom-2 left-[9px] top-2 w-0.5 rounded-full bg-black/10"
        />
        <ul className="flex flex-col">
          <AnimatePresence initial={false} mode="popLayout">
            {visible.map((a) => (
              <motion.li
                layout={!reduce}
                key={a.nro}
                initial={{ opacity: 0, y: 8 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, height: 0, marginTop: 0 }}
                transition={reduce ? { duration: 0.2 } : listSpring}
                className="relative flex gap-4"
              >
                <div className="relative flex w-5 shrink-0 flex-col items-center">
                  <span
                    className={`relative z-10 mt-6 h-3 w-3 shrink-0 rounded-full border-2 border-neutral-50 ${dotClass(a)}`}
                  />
                </div>
                <div className="min-w-0 flex-1 pb-5">
                  <AnnotationCard a={a} />
                </div>
              </motion.li>
            ))}
          </AnimatePresence>
        </ul>
      </div>
    </div>
  )
}