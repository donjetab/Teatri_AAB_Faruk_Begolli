import { useTranslation } from 'react-i18next'
import logoScene from '../../assets/Teatri Logo/Teatri AAB - Scene Logo.png'
import { resolveMediaUrl } from '../../api/client'

function splitReservationTitle(title) {
  const parts = title.split(/\s+/)
  const pivot = Math.max(2, Math.ceil(parts.length / 2))
  return [parts.slice(0, pivot).join(' '), parts.slice(pivot).join(' ')]
}

export function ReservationBanner({ home }) {
  const { t } = useTranslation()
  const background = resolveMediaUrl(home.reservationBanner?.url)
  const [firstLine, secondLine] = splitReservationTitle(home.reservationTitle ?? t('home.reservationTitleFallback'))

  return (
    <section className="reservation-section" aria-labelledby="reservation-title">
      <div
        className="reservation-banner"
        style={background ? { '--reservation-image': `url("${background}")` } : undefined}
      >
        <div className="reservation-content">
          <div className="reservation-heading">
            <img src={logoScene} alt="" loading="lazy" aria-hidden="true" />
            <h2 id="reservation-title">
              <span>{firstLine}</span>
              {secondLine && <strong>{secondLine}</strong>}
            </h2>
          </div>
          <p>{home.reservationText ?? t('home.reservationTextFallback')}</p>
          <a href={home.reservationUrl ?? '#'} className="reservation-button">
            <span>{t('home.reserveTicket')}</span>
            <span className="circle-arrow" aria-hidden="true">→</span>
          </a>
        </div>
      </div>
    </section>
  )
}
