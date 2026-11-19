import { createContext, useContext, useEffect, useState } from "react"
import es from "./es"
import en from "./en"

const STORAGE_KEY = "aukaria_lang"

const DICTS = {
  "es-CO": es,
  "en-US": en,
}

export const LANGUAGES = [
  { code: "es-CO", short: "es", label: "Español (Colombia)", flag: "🇨🇴" },
  { code: "en-US", short: "en", label: "English (US)", flag: "🇺🇸" },
  { code: "fr-FR", short: "fr", label: "Français", flag: "🇫🇷", pronto: true },
  { code: "de-DE", short: "de", label: "Deutsch", flag: "🇩🇪", pronto: true },
  { code: "pt-BR", short: "pt", label: "Português", flag: "🇵🇹", pronto: true },
]

function leerIdiomaInicial() {
  if (typeof window === "undefined") return "es-CO"
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved && DICTS[saved]) return saved
  } catch {
    /* ignore */
  }
  return "es-CO"
}

const LanguageContext = createContext(null)

export function LanguageProvider({ children }) {
  const [langCode, setLangCode] = useState(leerIdiomaInicial)

  const language = LANGUAGES.find((l) => l.code === langCode) || LANGUAGES[0]
  const dict = DICTS[language.code] || es

  useEffect(() => {
    try {
      localStorage.setItem(STORAGE_KEY, language.code)
    } catch {
      /* ignore */
    }
    if (typeof document !== "undefined") {
      document.documentElement.lang = language.short
    }
  }, [language])

  const setLanguage = (code) => {
    const found = LANGUAGES.find((l) => l.code === code && !l.pronto)
    if (found) setLangCode(found.code)
  }

  const t = (key, vars) => {
    let text = dict[key] ?? es[key] ?? key
    if (vars && typeof text === "string") {
      for (const [k, v] of Object.entries(vars)) {
        text = text.replaceAll(`{${k}}`, String(v ?? ""))
      }
    }
    return text
  }

  return (
    <LanguageContext.Provider value={{ language, langCode: language.code, langShort: language.short, setLanguage, t }}>
      {children}
    </LanguageContext.Provider>
  )
}

export function useLanguage() {
  const ctx = useContext(LanguageContext)
  if (!ctx) {
    throw new Error("useLanguage must be used within LanguageProvider")
  }
  return ctx
}

export function useT() {
  return useLanguage().t
}
