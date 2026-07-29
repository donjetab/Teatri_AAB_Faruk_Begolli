import { useLocation, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { getLocalizedPath, getRouteKey, languages } from '../../routes/localizedRoutes'
import { getNewsArticle } from '../../api/news'

export function LanguageSwitcher({ language }) {
  const { t, i18n } = useTranslation()
  const location = useLocation()
  const navigate = useNavigate()

  async function changeLanguage(nextLanguage) {
    if (nextLanguage === language) return
    const routeKey = getRouteKey(location.pathname)
    let nextPath = getLocalizedPath(routeKey, nextLanguage)
    const segments = location.pathname.split('/').filter(Boolean)

    if (routeKey === 'news' && segments.length >= 3) {
      try {
        const translatedArticle = await getNewsArticle(nextLanguage, segments.at(-1))
        if (translatedArticle && !translatedArticle.isFallbackTranslation) {
          nextPath = `${nextPath}/${translatedArticle.slug}`
        }
      } catch {
        // Keep the translated news-list path when this article has no translation.
      }
    }

    i18n.changeLanguage(nextLanguage)
    localStorage.setItem('aab-theatre-language', nextLanguage)
    document.documentElement.lang = nextLanguage
    navigate(nextPath)
  }

  return (
    <div className="language-switcher" aria-label={t('a11y.languageSwitcher')}>
      {languages.map((item) => (
        <button
          key={item}
          type="button"
          className={item === language ? 'language-option active' : 'language-option'}
          onClick={() => changeLanguage(item)}
          aria-pressed={item === language}
        >
          {item.toUpperCase()}
        </button>
      ))}
    </div>
  )
}
