import { useEffect, useState } from 'react'
import { useOutletContext, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { getHome } from '../api/home'
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
  const outletContext = useOutletContext()
  const setHomepageMeta = outletContext?.setHomepageMeta
  const [home, setHome] = useState(null)
  const [status, setStatus] = useState('loading')

  useEffect(() => {
    const controller = new AbortController()
    setStatus('loading')

    getHome(language, controller.signal)
      .then((data) => {
        setHome(data)
        setHomepageMeta?.({
          reservationUrl: data.reservationUrl,
          facebookUrl: data.facebookUrl,
          instagramUrl: data.instagramUrl,
          address: data.address,
          phone: data.phone,
          email: data.email,
        })
        setStatus('success')
      })
      .catch((error) => {
        if (error.name === 'CanceledError' || error.code === 'ERR_CANCELED') {
          return
        }

        setHome(null)
        setHomepageMeta?.(null)
        setStatus('error')
      })

    return () => controller.abort()
  }, [language, setHomepageMeta])

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
      <HeroSection home={home} language={language} />
      <AboutPreview home={home} />
      <UpcomingShows shows={home.upcomingShows ?? []} language={language} reservationUrl={home.reservationUrl} />
      <PitfPreview pitf={home.pitfFeatured} language={language} />
      <ReservationBanner home={home} />
    </div>
  )
}
