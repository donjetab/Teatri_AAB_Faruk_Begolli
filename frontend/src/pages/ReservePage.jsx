<<<<<<< HEAD
import { useEffect, useState } from 'react'
=======
import { useState } from 'react'
>>>>>>> 1ae7f1b (respo)
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import reserveHeader from '../assets/teatri/perne-bg.jpg'
import emptyShowsImage from '../assets/teatri-pitf-2024.jpg'
import { postersBySlug } from '../assets/shows/showAssets'
import smoke from '../assets/smoke_3.png'
import theatreIcon from '../assets/acting-icon-gold.png'
import { ArrowRightIcon } from '../components/icons/ArrowRightIcon'
import { getLocalizedPath } from '../routes/localizedRoutes'
<<<<<<< HEAD
import { getDemoReserve } from '../api/demo'
=======
>>>>>>> 1ae7f1b (respo)

function CalendarIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <rect x="3" y="5" width="18" height="16" rx="2" />
      <path d="M8 3v4M16 3v4M3 10h18" />
      <path d="M7 14h2M11 14h2M15 14h2M7 17h2M11 17h2M15 17h2" />
    </svg>
  )
}

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

export function ReservePage() {
  const { t } = useTranslation()
  const { language = 'sq' } = useParams()
  const [selectedDate, setSelectedDate] = useState('')
<<<<<<< HEAD
  const [activeShows, setActiveShows] = useState([])

  useEffect(() => {
    const controller = new AbortController()
    getDemoReserve(language, controller.signal)
      .then((data) => setActiveShows(data?.activeShows ?? []))
      .catch((error) => {
        if (error.name !== 'AbortError') {
          setActiveShows([])
        }
      })
    return () => controller.abort()
  }, [language])
=======
  const activeShows = [
    {
      id: 1,
      title: 'Bretkosa',
      date: '2026-09-10',
      venue: language === 'sq' ? 'Teatri Kombëtar' : 'National Theatre',
      time: '19:30',
      poster: postersBySlug.bretkosa,
    },
    {
      id: 2,
      title: 'Bretkosa',
      date: '2026-09-17',
      venue: language === 'sq' ? 'Teatri Kamertal AAB' : 'AAB Chamber Theatre',
      time: '19:30',
      poster: postersBySlug.bretkosa,
    },
  ]
>>>>>>> 1ae7f1b (respo)
  const hasActiveShows = activeShows.length > 0
  const visibleShows = selectedDate
    ? activeShows.filter((show) => show.date === selectedDate)
    : activeShows

  return (
    <article className="reserve-page">
      <section
        className="reserve-page-hero page-hero"
        style={{ '--page-hero-image': `url("${reserveHeader}")` }}
        aria-labelledby="reserve-page-title"
      >
        <img className="page-hero-smoke" src={smoke} alt="" aria-hidden="true" />
        <div className="page-hero-content">
          <h1 id="reserve-page-title">{t('reservePage.heroTitle')}</h1>
          <div className="page-hero-rule" aria-hidden="true">
            <span />
            <img src={theatreIcon} alt="" aria-hidden="true" />
            <span />
          </div>
          <p>{t('reservePage.heroSubtitle')}</p>
        </div>
      </section>

      <section
        className={`reserve-upcoming-section${hasActiveShows ? '' : ' reserve-upcoming-section-empty'}`}
        aria-labelledby={hasActiveShows ? 'reserve-upcoming-title' : undefined}
      >
        {hasActiveShows && (
          <header className="reserve-upcoming-heading">
            <h2 id="reserve-upcoming-title">{t('reservePage.upcomingTitle')}</h2>
            <label className="reserve-calendar-pill">
              <CalendarIcon />
              <span className="sr-only">{t('reservePage.dateFilter')}</span>
              <input
                type="date"
                value={selectedDate}
                onChange={(event) => setSelectedDate(event.target.value)}
                aria-label={t('reservePage.dateFilter')}
              />
            </label>
          </header>
        )}

        {hasActiveShows ? (
          <div className="reserve-show-list">
            {visibleShows.map((show) => {
              const date = new Date(`${show.date}T12:00:00`)
              return (
                <article className="reserve-show-card" key={show.id}>
<<<<<<< HEAD
                  <img className="reserve-show-poster" src={postersBySlug[show.slug]} alt="" />
=======
                  <img className="reserve-show-poster" src={show.poster} alt="" />
>>>>>>> 1ae7f1b (respo)
                  <div className="reserve-show-date">
                    <strong>{date.getDate()}</strong>
                    <span>{new Intl.DateTimeFormat(language === 'sq' ? 'sq-AL' : 'en-GB', { month: 'long' }).format(date)}</span>
                    <small>{language === 'sq' ? 'E enjte' : 'Thursday'}</small>
                  </div>
                  <div className="reserve-show-info">
                    <h3>{show.title}</h3>
                    <p><DetailIcon type="location" />{show.venue}</p>
                    <p><DetailIcon type="time" />{show.time}</p>
                  </div>
                  <div className="reserve-show-booking">
                    <span className="reserve-availability">
                      <i aria-hidden="true" />
                      {language === 'sq' ? 'Ka vende të lira' : 'Seats available'}
                    </span>
<<<<<<< HEAD
                    <a href={show.reservationUrl} className="reserve-seat-button">
=======
                    <a href="#" className="reserve-seat-button">
>>>>>>> 1ae7f1b (respo)
                      <span>{language === 'sq' ? 'Rezervo vendin' : 'Reserve seat'}</span>
                      <ArrowRightIcon />
                    </a>
                    <small>{language === 'sq' ? 'ose na kontakto' : 'or contact us'}</small>
<<<<<<< HEAD
                    <a className="reserve-phone-button" href={show.phoneUrl}>
                      <span aria-hidden="true">☎</span>
                      {show.phone}
=======
                    <a className="reserve-phone-button" href="tel:+38348999000">
                      <span aria-hidden="true">☎</span>
                      048 999 000
>>>>>>> 1ae7f1b (respo)
                    </a>
                  </div>
                </article>
              )
            })}
          </div>
        ) : (
          <article className="reserve-empty-card">
            <img src={emptyShowsImage} alt="" aria-hidden="true" />
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
