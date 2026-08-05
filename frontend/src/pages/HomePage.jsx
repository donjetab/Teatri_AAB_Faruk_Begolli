import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { getHome } from '../api/home'
import { getStaticPage } from '../api/staticPages'
import { AboutPreview } from '../components/home/AboutPreview'
import { HeroSection } from '../components/home/HeroSection'
import { PitfPreview } from '../components/home/PitfPreview'
import { ReservationBanner } from '../components/home/ReservationBanner'
import { UpcomingShows } from '../components/home/UpcomingShows'
import { EmptyState } from '../components/ui/EmptyState'
import { ErrorState } from '../components/ui/ErrorState'
import { LoadingState } from '../components/ui/LoadingState'
import { defaultLanguage, languages } from '../routes/localizedRoutes'

export function HomePage() {
  const { t } = useTranslation()
  const { language: languageParam } = useParams()
  const language = languages.includes(languageParam) ? languageParam : defaultLanguage
  const [home, setHome] = useState(null)
  const [aboutStatistics, setAboutStatistics] = useState(null)
  const [status, setStatus] = useState('loading')

  useEffect(() => {
    const controller = new AbortController()
    setStatus('loading')

    Promise.all([
      getHome(language, controller.signal),
      getStaticPage(language, 'about', controller.signal).catch(() => null),
    ])
      .then(([data, aboutPage]) => {
        setHome(data)
        setAboutStatistics(aboutPage?.statistics ?? null)
        setStatus('success')
      })
      .catch((error) => {
        if (error.name === 'CanceledError' || error.code === 'ERR_CANCELED') {
          return
        }

        setHome(null)
        setStatus('error')
      })

    return () => controller.abort()
  }, [language])

  if (status === 'loading') {
    return (
      <section className="homepage-state" aria-live="polite">
        <LoadingState message={t('states.loading')} />
      </section>
    )
  }

  if (status === 'error') {
    return (
      <section className="homepage-state" aria-live="polite">
        <ErrorState message={t('home.loadError')} />
      </section>
    )
  }

  if (!home) {
    return (
      <section className="homepage-state" aria-live="polite">
        <EmptyState message={t('states.empty')} />
      </section>
    )
  }

  return (
    <div className="homepage">
      {home.heroIsVisible !== false && <HeroSection home={home} language={language} />}
      <AboutPreview home={home} statistics={aboutStatistics} />
      <UpcomingShows shows={home.upcomingShows ?? []} language={language} reservationUrl={home.reservationUrl} />
      {home.pitfFeatureIsVisible !== false && <PitfPreview pitf={home.pitfFeatured} title={home.pitfFeatureTitle} buttonText={home.pitfFeatureButtonText} destinationUrl={home.pitfDestinationUrl} />}
      {home.reservationBannerIsVisible !== false && <ReservationBanner home={home} />}
    </div>
  )
}
