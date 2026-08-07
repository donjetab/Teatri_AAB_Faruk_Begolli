import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { getStaticPage } from '../api/staticPages'
import reserveHeader from '../assets/teatri/perne-bg.jpg'
import emptyShowsImage from '../assets/teatri-pitf-2024.jpg'
import { resolveMediaUrl } from '../api/client'
import smoke from '../assets/smoke_3.png'
import theatreIcon from '../assets/acting-icon-gold.png'
import { ArrowRightIcon } from '../components/icons/ArrowRightIcon'
import { getLocalizedPath } from '../routes/localizedRoutes'
import { getUpcomingPerformances } from '../api/performances'

function DetailIcon({ type }) {
  return type === 'location' ? (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M20 10c0 5-8 11-8 11S4 15 4 10a8 8 0 1 1 16 0Z" />
      <circle cx="12" cy="10" r="2.5" />
    </svg>
  ) : (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <circle cx="12" cy="12" r="9" />
      <path d="M12 7v5l3.5 2" />
    </svg>
  )
}

const localDateKey = value => {
  const date = new Date(value)
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

const albanianMonths = ['Janar', 'Shkurt', 'Mars', 'Prill', 'Maj', 'Qershor', 'Korrik', 'Gusht', 'Shtator', 'Tetor', 'Nëntor', 'Dhjetor']
const albanianWeekdays = ['E diel', 'E hënë', 'E martë', 'E mërkurë', 'E enjte', 'E premte', 'E shtunë']

export function ReservePage() {
  const { t } = useTranslation()
  const { language = 'sq' } = useParams()
  const [selectedDate, setSelectedDate] = useState('')
  const [activeShows, setActiveShows] = useState([])
  const [pageCopy, setPageCopy] = useState(null)

  useEffect(() => { const controller = new AbortController(); getStaticPage(language, 'reservations', controller.signal).then(setPageCopy).catch(() => setPageCopy(null)); return () => controller.abort() }, [language])

  useEffect(() => {
    const controller = new AbortController()
    getUpcomingPerformances(language, controller.signal)
      .then(setActiveShows)
      .catch((error) => {
        if (error.name !== 'AbortError') {
          setActiveShows([])
        }
      })
    return () => controller.abort()
  }, [language])
  const hasActiveShows = activeShows.length > 0
  const visibleShows = selectedDate
    ? activeShows.filter((show) => localDateKey(show.startDateTimeUtc) === selectedDate)
    : activeShows

  return (
    <article className="reserve-page">
      <section
        className="reserve-page-hero page-hero"
        style={{ '--page-hero-image': `url("${resolveMediaUrl(pageCopy?.headerImageUrl) || reserveHeader}")` }}
        aria-labelledby="reserve-page-title"
      >
        <img className="page-hero-smoke" src={smoke} alt="" aria-hidden="true" />
        <div className="page-hero-content">
          <h1 id="reserve-page-title">{pageCopy?.title || t('reservePage.heroTitle')}</h1>
          <div className="page-hero-rule" aria-hidden="true">
            <span />
            <img src={theatreIcon} alt="" aria-hidden="true" />
            <span />
          </div>
          <p>{pageCopy?.subtitle || t('reservePage.heroSubtitle')}</p>
        </div>
      </section>

      <section
        className={`reserve-upcoming-section${hasActiveShows ? '' : ' reserve-upcoming-section-empty'}`}
        aria-labelledby={hasActiveShows ? 'reserve-upcoming-title' : undefined}
      >
        {hasActiveShows && (
          <header className="reserve-upcoming-heading">
            <h2 id="reserve-upcoming-title">{t('reservePage.upcomingTitle')}</h2>
            <div className="reserve-date-filter"><label className="reserve-calendar-pill">
              <span className="sr-only">{t('reservePage.dateFilter')}</span>
              <input type="date" value={selectedDate} onChange={(event) => setSelectedDate(event.target.value)} aria-label={t('reservePage.dateFilter')} />
            </label>{selectedDate && <button type="button" className="reserve-date-clear" onClick={() => setSelectedDate('')}>{language === 'sq' ? 'Pastro' : 'Clear'}</button>}</div>
          </header>
        )}

        {hasActiveShows ? (
          <div className="reserve-show-list">
            {visibleShows.map((show) => {
              const date = new Date(show.startDateTimeUtc)
              const poster = resolveMediaUrl(show.posterUrl)
              const phoneUrl = show.contactPhone ? `tel:${show.contactPhone.replace(/[^\d+]/g, '')}` : null
              const isPostponed = show.status === 'Postponed'
              const isCancelled = show.status === 'Cancelled'
              const isInactive = isPostponed || isCancelled
              const monthLabel = language === 'sq' ? albanianMonths[date.getMonth()] : new Intl.DateTimeFormat('en-GB', { month: 'long' }).format(date)
              const originalSchedule = language === 'sq'
                ? `${date.getDate()} ${albanianMonths[date.getMonth()]} ${date.getFullYear()}, ${new Intl.DateTimeFormat('sq-AL', { hour: '2-digit', minute: '2-digit' }).format(date)}`
                : new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' }).format(date)
              return (
                <article className="reserve-show-card" key={show.id}>
                  {poster && <img className="reserve-show-poster" src={poster} alt="" />}
                  <div className={`reserve-show-date${isPostponed ? ' postponed' : isCancelled ? ' cancelled' : ''}`}>
                    <strong>{date.getDate()}</strong>
                    <span>{monthLabel}</span>
                    <small>{isPostponed ? (language === 'sq' ? 'E shtyrë' : 'Postponed') : isCancelled ? (language === 'sq' ? 'E anuluar' : 'Cancelled') : language === 'sq' ? albanianWeekdays[date.getDay()] : new Intl.DateTimeFormat('en-GB', { weekday: 'long' }).format(date)}</small>
                  </div>
                  <div className="reserve-show-info">
                    <h3>{show.showTitle}</h3>
                    <p><DetailIcon type="location" />{show.venue ? `${show.venue}${show.venueAddress ? `, ${show.venueAddress}` : ''}${show.hall ? ` · ${show.hall}` : ''}` : show.hall || (language === 'sq' ? 'Lokacioni do të njoftohet' : 'Venue to be announced')}</p>
                    <p><DetailIcon type="time" />{isPostponed ? (language === 'sq' ? `Ishte planifikuar për ${originalSchedule}. Data e re do të njoftohet.` : `Originally scheduled for ${originalSchedule}. A new date will be announced.`) : isCancelled ? (language === 'sq' ? `Ishte planifikuar për ${originalSchedule}.` : `Originally scheduled for ${originalSchedule}.`) : new Intl.DateTimeFormat(language === 'sq' ? 'sq-AL' : 'en-GB', { hour: '2-digit', minute: '2-digit' }).format(date)}</p>
                  </div>
                  <div className="reserve-show-booking">
                    <span className={`reserve-availability${show.status === 'SoldOut' ? ' sold-out' : isPostponed ? ' postponed' : isCancelled ? ' cancelled' : ''}`}>
                      <i aria-hidden="true" />
                      {show.status === 'SoldOut' ? (language === 'sq' ? 'E shitur' : 'Sold out') : isPostponed ? (language === 'sq' ? 'Shfaqja është shtyrë' : 'Performance postponed') : isCancelled ? (language === 'sq' ? 'Shfaqja është anuluar' : 'Performance cancelled') : (language === 'sq' ? 'E hapur për rezervim' : 'Open for booking')}
                    </span>
                    {show.reservationMode === 'Internal' && show.internalReservationUrl && show.status !== 'SoldOut' && !isInactive && <Link to={show.internalReservationUrl} className="reserve-seat-button">
                      <span>{language === 'sq' ? 'Rezervo vendin' : 'Reserve seat'}</span>
                      <ArrowRightIcon />
                    </Link>}
                    {show.reservationMode !== 'Internal' && show.ticketUrl && show.status !== 'SoldOut' && !isInactive && <a href={show.ticketUrl} className="reserve-seat-button" target="_blank" rel="noopener noreferrer">
                      <span>{language === 'sq' ? 'Rezervo vendin' : 'Reserve seat'}</span>
                      <ArrowRightIcon />
                    </a>}
                    {phoneUrl && show.status !== 'SoldOut' && !isInactive && <><small>{show.ticketUrl ? (language === 'sq' ? 'Ose na kontakto' : 'Or contact us') : (language === 'sq' ? 'Na kontakto' : 'Contact us')}</small>
                      <a className="reserve-phone-button" href={phoneUrl}>
                        <span aria-hidden="true">☎</span>
                        {show.contactPhone}
                      </a></>}
                  </div>
                </article>
              )
            })}
          </div>
        ) : (
          <article className="reserve-empty-card">
            <img src={resolveMediaUrl(pageCopy?.featuredImageUrl) || emptyShowsImage} alt="" aria-hidden="true" />
            <div className="reserve-empty-copy">
              <h3>{t('reservePage.emptyTitle')}</h3>
              <p>{t('reservePage.emptyText')}</p>
              <div className="reserve-empty-actions">
                <Link className="reserve-outline-link" to={getLocalizedPath('shows', language)}>
                  <span>{t('reservePage.viewShows')}</span>
                  <ArrowRightIcon />
                </Link>
                <Link className="reserve-filled-link" to={getLocalizedPath('news', language)}>
                  <span>{t('reservePage.viewNews')}</span>
                  <ArrowRightIcon />
                </Link>
              </div>
            </div>
          </article>
        )}
      </section>
    </article>
  )
}
