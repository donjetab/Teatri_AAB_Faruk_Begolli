import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { getShows } from '../api/shows'
import { EmptyState } from '../components/ui/EmptyState'
import { ErrorState } from '../components/ui/ErrorState'
import { LoadingState } from '../components/ui/LoadingState'
import showsHeaderBackground from '../assets/shows-header.jpg'
import smoke from '../assets/smoke_3.png'
import theatreIcon from '../assets/acting-icon-gold.png'
import { postersBySlug } from '../assets/shows/showAssets'

const categoriesBySlug = {
  'profesor-jam-talent': 'comedy',
  'per-caj-te-hillary': 'comedy',
  rrena: 'comedy',
  'dy-gjitare-enderrimtare': 'children',
  'hana-dhe-dielli': 'children',
  'gjirafa-dhe-buburreci': 'children',
}

export function ShowsPage() {
  const { t } = useTranslation()
  const { language = 'sq' } = useParams()
  const [shows, setShows] = useState([])
  const [activeFilter, setActiveFilter] = useState('all')
  const [status, setStatus] = useState('loading')
  const filters = ['all', 'drama', 'comedy', 'children']

  useEffect(() => {
    const controller = new AbortController()
    setStatus('loading')

    getShows(language, controller.signal)
      .then((data) => {
        setShows(data)
        setStatus('success')
      })
      .catch((error) => {
        if (error.name !== 'CanceledError') {
          setStatus('error')
        }
      })

    return () => controller.abort()
  }, [language])

  const filteredShows = useMemo(
    () => [...shows]
      .sort((left, right) => Number(right.slug === 'bretkosa') - Number(left.slug === 'bretkosa'))
      .filter((show) => {
        const category = categoriesBySlug[show.slug] ?? 'drama'
        return activeFilter === 'all' || activeFilter === category
      }),
    [activeFilter, shows],
  )

  return (
    <article className="shows-page">
      <section
        className="shows-page-hero page-hero"
        style={{ '--page-hero-image': `url("${showsHeaderBackground}")` }}
        aria-labelledby="shows-page-title"
      >
        <img className="page-hero-smoke" src={smoke} alt="" aria-hidden="true" />
        <div className="page-hero-content">
          <h1 id="shows-page-title">{t('showsPage.heroTitle')}</h1>
          <div className="page-hero-rule" aria-hidden="true">
            <span />
            <img src={theatreIcon} alt="" aria-hidden="true" />
            <span />
          </div>
          <p>{t('showsPage.heroSubtitle')}</p>
          <div className="shows-filter-bar" aria-label={t('showsPage.filtersLabel')}>
            {filters.map((filter) => (
              <button
                key={filter}
                type="button"
                className={activeFilter === filter ? 'shows-filter active' : 'shows-filter'}
                aria-pressed={activeFilter === filter}
                onClick={() => setActiveFilter(filter)}
              >
                {t(`showsPage.filters.${filter}`)}
              </button>
            ))}
          </div>
        </div>
      </section>

      <section className="shows-list-section" aria-label={t('showsPage.listLabel')}>
        {status === 'loading' && <LoadingState />}
        {status === 'error' && <ErrorState message={t('showsPage.loadError')} />}
        {status === 'success' && filteredShows.length === 0 && <EmptyState />}
        {status === 'success' && filteredShows.length > 0 && (
          <div className="shows-page-grid">
            {filteredShows.map((show) => (
              <article className="shows-page-card" key={show.id}>
                <Link
                  className="shows-page-card-link"
                  to={`/${language}/${language === 'en' ? 'shows' : 'shfaqjet'}/${show.slug}`}
                  aria-label={t('showsPage.openShow', { title: show.title })}
                >
                  <img
                    src={postersBySlug[show.slug]}
                    alt={t('showsPage.posterAlt', { title: show.title })}
                    loading="lazy"
                  />
                  <span className="shows-page-card-overlay" aria-hidden="true" />
                  <div className="shows-page-card-copy">
                    <h2>{show.title}</h2>
                    {show.director && <p>{t('showsPage.directedBy', { director: show.director })}</p>}
                  </div>
                </Link>
                <div className="shows-page-card-actions">
                  <Link to={`/${language}/${language === 'en' ? 'shows' : 'shfaqjet'}/${show.slug}`}>
                    {t('showsPage.read')}
                  </Link>
                  {show.nextPerformanceDateUtc && (
                    <a href={show.reservationUrl || '#'}>{t('showsPage.reserve')}</a>
                  )}
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </article>
  )
}
