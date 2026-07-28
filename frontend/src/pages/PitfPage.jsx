import { useEffect, useRef, useState } from 'react'
import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { getHome } from '../api/home'
import { ReservationBanner } from '../components/home/ReservationBanner'
import { ArrowRightIcon } from '../components/icons/ArrowRightIcon'
import pitfHeader from '../assets/pitf-header.jpg'
import pitfPicture from '../assets/pitf-pic.jpg'
import theatreIcon from '../assets/acting-icon-gold.png'
import smoke from '../assets/smoke_3.png'
import edition01 from '../assets/pitf-editions/1.png'
import edition02 from '../assets/pitf-editions/2.png'
import edition03 from '../assets/pitf-editions/3.png'
import edition04 from '../assets/pitf-editions/4.png'
import edition05 from '../assets/pitf-editions/5.png'
import edition06 from '../assets/pitf-editions/6.png'
import edition07 from '../assets/pitf-editions/7.png'
import edition08 from '../assets/pitf-editions/8.png'
import edition09 from '../assets/pitf-editions/9.png'
import edition10 from '../assets/pitf-editions/10.png'

const editions = [
  { number: 10, year: 2026, image: edition10 },
  { number: 9, year: 2025, image: edition09 },
  { number: 8, year: 2024, image: edition08 },
  { number: 7, year: 2023, image: edition07 },
  { number: 6, year: 2022, image: edition06 },
  { number: 5, year: 2021, image: edition05 },
  { number: 4, year: 2020, image: edition04 },
  { number: 3, year: 2019, image: edition03 },
  { number: 2, year: 2018, image: edition02 },
  { number: 1, year: 2017, image: edition01 },
]

export function PitfPage() {
  const { t } = useTranslation()
  const { language = 'sq' } = useParams()
  const [homeMeta, setHomeMeta] = useState(null)
  const [editionsVisible, setEditionsVisible] = useState(false)
  const editionsRef = useRef(null)

  useEffect(() => {
    const controller = new AbortController()

    getHome(language, controller.signal)
      .then(setHomeMeta)
      .catch((error) => {
        if (error.name !== 'CanceledError' && error.code !== 'ERR_CANCELED') {
          setHomeMeta(null)
        }
      })

    return () => controller.abort()
  }, [language])

  useEffect(() => {
    const section = editionsRef.current
    if (!section) {
      return undefined
    }

    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      setEditionsVisible(true)
      return undefined
    }

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setEditionsVisible(true)
          observer.disconnect()
        }
      },
      { threshold: 0.12 },
    )

    observer.observe(section)
    return () => observer.disconnect()
  }, [])

  return (
    <article className="pitf-page">
      <section
        className="pitf-page-hero about-hero"
        style={{ '--about-hero-image': `url("${pitfHeader}")` }}
        aria-labelledby="pitf-page-title"
      >
        <img className="about-smoke" src={smoke} alt="" aria-hidden="true" />
        <div className="about-hero-content">
          <h1 id="pitf-page-title">PITF</h1>
          <div className="about-hero-rule" aria-hidden="true">
            <span />
            <img src={theatreIcon} alt="" aria-hidden="true" />
            <span />
          </div>
          <p>{t('pitfPage.fullName')}</p>
        </div>
      </section>

      <section className="pitf-page-about" aria-label={t('pitfPage.aboutLabel')}>
        <div className="pitf-page-about-inner">
          <figure className="pitf-page-picture">
            <img src={pitfPicture} alt={t('pitfPage.imageAlt')} loading="lazy" />
          </figure>
          <div className="pitf-page-copy">
            <p>{t('pitfPage.intro.paragraph1')}</p>
            <p>{t('pitfPage.intro.paragraph2')}</p>
          </div>
        </div>
      </section>

      <section
        ref={editionsRef}
        className={`pitf-editions-section${editionsVisible ? ' is-visible' : ''}`}
        aria-labelledby="pitf-editions-title"
      >
        <header className="pitf-editions-heading">
          <p>{t('pitfPage.editionsEyebrow')}</p>
          <h2 id="pitf-editions-title">{t('pitfPage.editionsTitle')}</h2>
        </header>

        <ol className="pitf-editions-grid">
          {editions.map((edition) => (
            <li
              className="pitf-edition"
              key={edition.number}
              style={{ '--edition-index': editions.length - edition.number }}
            >
              <div className="pitf-edition-image">
                <img
                  src={edition.image}
                  alt={t('pitfPage.editionAlt', { number: edition.number, year: edition.year })}
                  loading="lazy"
                />
              </div>
              <span className="pitf-edition-marker" aria-hidden="true" />
              <strong>{edition.number.toString().padStart(2, '0')}</strong>
              <time dateTime={edition.year.toString()}>{edition.year}</time>
            </li>
          ))}
        </ol>

        <a className="pitf-reserve-button" href={homeMeta?.reservationUrl ?? '#'}>
          <span>{t('pitfPage.learnMore')}</span>
          <span className="circle-arrow" aria-hidden="true">
            <ArrowRightIcon className="arrow-icon" />
          </span>
        </a>
      </section>

      {homeMeta && <ReservationBanner home={homeMeta} />}
    </article>
  )
}
