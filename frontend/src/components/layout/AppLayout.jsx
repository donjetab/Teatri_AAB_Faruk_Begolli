import { Outlet, useLocation, useParams } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Footer } from './Footer'
import { Header } from './Header'
import { getLanguageFromPath } from '../../routes/localizedRoutes'

export function AppLayout() {
  const { i18n } = useTranslation()
  const params = useParams()
  const location = useLocation()
  const language = params.language ?? getLanguageFromPath(location.pathname)
  const [homepageMeta, setHomepageMeta] = useState(null)

  useEffect(() => {
    if (i18n.language !== language) {
      i18n.changeLanguage(language)
    }
    localStorage.setItem('aab-theatre-language', language)
    document.documentElement.lang = language
  }, [i18n, language])

  useEffect(() => {
    setHomepageMeta(null)
  }, [language])

  return (
    <div className="site-shell">
      <Header language={language} reservationUrl={homepageMeta?.reservationUrl} />
      <main className="site-main" id="content">
        <Outlet context={{ setHomepageMeta }} />
      </main>
      <Footer language={language} homepageMeta={homepageMeta} />
    </div>
  )
}
