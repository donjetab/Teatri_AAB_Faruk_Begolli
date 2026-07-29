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
  reserve: { sq: '/sq/rezervo', en: '/en/reserve' },
}

export function getLanguageFromPath(pathname) {
  const segment = pathname.split('/').filter(Boolean)[0]
  return languages.includes(segment) ? segment : defaultLanguage
}

export function getRouteKey(pathname) {
  return (
    Object.entries(routeKeys).find(([routeKey, localized]) =>
      Object.values(localized).some(
        (path) => pathname === path || (routeKey !== 'home' && pathname.startsWith(`${path}/`)),
      ),
    )?.[0] ?? 'home'
  )
}

export function getLocalizedPath(routeKey, language) {
  return routeKeys[routeKey]?.[language] ?? routeKeys.home[language]
}

export function getManagedDestination(destination, language, fallbackRouteKey) {
  if (!destination) return getLocalizedPath(fallbackRouteKey, language)
  if (!destination.startsWith('#/')) return destination
  return getLocalizedPath(getRouteKey(destination.slice(1)), language)
}
