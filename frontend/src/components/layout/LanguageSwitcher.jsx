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
    <button
      type="button"
      className={`language-switcher language-${language}`}
      role="switch"
      aria-checked={language === 'en'}
      aria-label={t('a11y.languageSwitcher')}
      onClick={() => changeLanguage(language === 'sq' ? 'en' : 'sq')}
    >
      <span className="language-switch-slider" aria-hidden="true" />
      {languages.map((item) => <span key={item} className={item === language ? 'language-option active' : 'language-option'}>
          {item.toUpperCase()}
        </span>)}
    </button>
  )
}
