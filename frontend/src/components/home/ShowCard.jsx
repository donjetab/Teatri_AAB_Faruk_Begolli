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
  const externalUrl = show.reservationMode === 'ExternalUrl' && show.ticketUrl && show.ticketUrl !== 'https://example.com/reservations' ? show.ticketUrl : null

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
      {show.reservationMode === 'Internal' && show.internalReservationUrl ? <Link className="show-ticket-link" to={show.internalReservationUrl} aria-label={t('home.reserveForShow', { title: show.title })}>{i18n.language === 'sq' ? 'Rezervo' : 'Reserve'}</Link>
        : externalUrl ? <a className="show-ticket-link" href={externalUrl} target="_blank" rel="noopener noreferrer" aria-label={t('home.reserveForShow', { title: show.title })}>{i18n.language === 'sq' ? 'Bileta' : 'Tickets'}</a>
          : <span className="show-ticket-link is-unavailable">{i18n.language === 'sq' ? 'E padisponueshme' : 'Unavailable'}</span>}
    </article>
  )
}
