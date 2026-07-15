export const languages = ['sq', 'en']
export const defaultLanguage = 'sq'

export const routeKeys = {
  home: { sq: '/sq', en: '/en' },
  about: { sq: '/sq/per-ne', en: '/en/about' },
  shows: { sq: '/sq/shfaqjet', en: '/en/shows' },
  news: { sq: '/sq/lajme', en: '/en/news' },
  pitf: { sq: '/sq/pitf', en: '/en/pitf' },
  gallery: { sq: '/sq/galeria', en: '/en/gallery' },
  contact: { sq: '/sq/kontakti', en: '/en/contact' },
}

export function getLanguageFromPath(pathname) {
  const segment = pathname.split('/').filter(Boolean)[0]
  return languages.includes(segment) ? segment : defaultLanguage
}

export function getRouteKey(pathname) {
  return (
    Object.entries(routeKeys).find(([, localized]) =>
      Object.values(localized).includes(pathname),
    )?.[0] ?? 'home'
  )
}

export function getLocalizedPath(routeKey, language) {
  return routeKeys[routeKey]?.[language] ?? routeKeys.home[language]
}
