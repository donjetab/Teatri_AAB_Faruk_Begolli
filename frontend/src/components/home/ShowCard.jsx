import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { resolveMediaUrl } from '../../api/client'
import { getLocalizedPath } from '../../routes/localizedRoutes'

export function ShowCard({ show, language }) {
  const { t, i18n } = useTranslation()
  const posterUrl = resolveMediaUrl(show.posterUrl)
  const date = new Date(show.nearestPerformanceDateUtc)
  const monthKey = date.toLocaleString('en-US', { month: 'short', timeZone: 'UTC' }).toLowerCase()
  const externalUrl = show.reservationMode === 'ExternalUrl' && show.ticketUrl && show.ticketUrl !== 'https://example.com/reservations' ? show.ticketUrl : null
  const internalUrl = show.reservationMode === 'Internal' ? show.internalReservationUrl : null
  const bookingUrl = internalUrl || externalUrl || getLocalizedPath('reserve', language)
  const bookingLabel = internalUrl
    ? (i18n.language === 'sq' ? 'Rezervo' : 'Reserve')
    : externalUrl
      ? (i18n.language === 'sq' ? 'Bileta' : 'Tickets')
      : t('home.openReservations')

  return (
    <article className="show-card">
      {externalUrl
        ? <a href={bookingUrl} className="show-card-link" target="_blank" rel="noopener noreferrer" aria-label={t('home.reserveForShow', { title: show.title })}>
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
        <span className="show-ticket-link">{bookingLabel}</span>
      </a>
        : <Link to={bookingUrl} className="show-card-link" aria-label={t('home.reserveForShow', { title: show.title })}>
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
        <span className="show-ticket-link">{bookingLabel}</span>
      </Link>}
    </article>
  )
}
