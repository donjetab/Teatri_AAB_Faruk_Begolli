import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { getLocalizedPath } from '../../routes/localizedRoutes'
import { ShowCard } from './ShowCard'

export function UpcomingShows({ shows, language, reservationUrl }) {
  const { t } = useTranslation()

  return (
    <section className="shows-section" aria-labelledby="shows-title">
      <div className="shows-grid">
        <article className="shows-title-card">
          <h2 id="shows-title">
            <span>{t('home.upcomingShows.line1')}</span>
            <strong>{t('home.upcomingShows.line2')}</strong>
          </h2>
          <Link to={getLocalizedPath('shows', language)} className="shows-all-link">
            <span>{t('home.viewAllShows')}</span>
            <span aria-hidden="true">→</span>
          </Link>
        </article>

        {shows.length > 0 ? (
          shows.map((show) => (
            <ShowCard key={show.id} show={show} language={language} reservationUrl={reservationUrl} />
          ))
        ) : (
          <article className="shows-empty" aria-live="polite">
            <div className="shows-empty-mark" aria-hidden="true"><span>◆</span></div>
            <div className="shows-empty-copy">
              <span className="shows-empty-eyebrow">{t('home.noShowsEyebrow')}</span>
              <h3>{t('home.noShowsTitle')}</h3>
              <p>{t('home.noShowsText')}</p>
            </div>
            <Link to={getLocalizedPath('shows', language)} className="shows-empty-link">
              <span>{t('home.browseRepertoire')}</span>
              <span aria-hidden="true">→</span>
            </Link>
          </article>
        )}
      </div>
    </section>
  )
}
