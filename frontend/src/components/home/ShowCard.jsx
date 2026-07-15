import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { resolveMediaUrl } from '../../api/client'
import { getLocalizedPath } from '../../routes/localizedRoutes'

function getShowUrl(language, slug) {
  return `${getLocalizedPath('shows', language)}/${slug}`
}

export function ShowCard({ show, language, reservationUrl }) {
  const { t, i18n } = useTranslation()
  const posterUrl = resolveMediaUrl(show.posterUrl)
  const date = new Date(show.nearestPerformanceDateUtc)
  const monthKey = date.toLocaleString('en-US', { month: 'short', timeZone: 'UTC' }).toLowerCase()
  const ticketUrl = show.ticketUrl || reservationUrl || getShowUrl(language, show.slug)

  return (
    <article className="show-card">
      <Link to={getShowUrl(language, show.slug)} className="show-card-link" aria-label={show.title}>
        {posterUrl && (
          <img
            src={posterUrl}
            alt={t('home.showPosterAlt', { title: show.title })}
            loading="lazy"
            onError={(event) => { event.currentTarget.hidden = true }}
          />
        )}
        <div className="show-card-overlay" />
        <time className="show-date" dateTime={show.nearestPerformanceDateUtc}>
          <strong>{date.getUTCDate().toString().padStart(2, '0')}</strong>
          <span>{t(`months.${monthKey}`)}</span>
        </time>
        <div className="show-card-copy">
          <h3>{show.title}</h3>
          {show.director && <p>{t('home.directedBy', { director: show.director })}</p>}
        </div>
      </Link>
      <a className="show-ticket-link" href={ticketUrl} aria-label={t('home.reserveForShow', { title: show.title })}>
        {i18n.language === 'sq' ? 'Bileta' : 'Tickets'}
      </a>
    </article>
  )
}
