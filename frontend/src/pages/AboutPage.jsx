import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import aboutBackground from '../assets/teatri/perne-bg.jpg'
import aboutTheatreImage from '../assets/teatri/AAB.jpg'
import quoteBackground from '../assets/teatri/AAB-THEATER-SCENE.jpg'
import galleryOne from '../assets/teatri/AAB-THEATER-CHAIRS.jpg'
import galleryTwo from '../assets/teatri/AAB-THEATER-FARUK-BEGOLLI.jpg'
import galleryThree from '../assets/teatri/AAB-THEATER-SCENE.jpg'
import galleryFour from '../assets/teatri/AAB-THEATER-VIEW-FROM-SCENE.jpg'
import galleryFive from '../assets/teatri/THEATER-AAB.jpg'
import gallerySix from '../assets/teatri/CHAIRS.jpg'
import smokeOne from '../assets/smoke_3.png'
import theatreIcon from '../assets/acting-icon-gold.png'
import statsTheatreIcon from '../assets/theatre-icon.png'
import actingIcon from '../assets/acting-icon.png'
import spectatorsIcon from '../assets/spectators-icon.png'
import { getHome } from '../api/home'
import { ReservationBanner } from '../components/home/ReservationBanner'
import { ArrowRightIcon } from '../components/icons/ArrowRightIcon'
import { getLocalizedPath } from '../routes/localizedRoutes'

export function AboutPage() {
  const { t } = useTranslation()
  const { language = 'sq' } = useParams()
  const [homeMeta, setHomeMeta] = useState(null)
  const [selectedImageIndex, setSelectedImageIndex] = useState(null)
  const stats = [
    { value: '2015', label: t('home.stats.founded'), icon: statsTheatreIcon },
    { value: '500+', label: t('home.stats.performances'), icon: actingIcon },
    { value: '100K+', label: t('home.stats.spectators'), icon: spectatorsIcon },
  ]
  const galleryImages = [
    { src: galleryOne, alt: t('aboutPage.galleryImageAlt', { number: 1 }) },
    { src: galleryTwo, alt: t('aboutPage.galleryImageAlt', { number: 2 }) },
    { src: galleryThree, alt: t('aboutPage.galleryImageAlt', { number: 3 }) },
    { src: galleryFour, alt: t('aboutPage.galleryImageAlt', { number: 4 }) },
    { src: galleryFive, alt: t('aboutPage.galleryImageAlt', { number: 5 }) },
    { src: gallerySix, alt: t('aboutPage.galleryImageAlt', { number: 6 }) },
  ]
  const selectedImage = selectedImageIndex === null ? null : galleryImages[selectedImageIndex]

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
    if (!selectedImage) {
      return undefined
    }

    function closeOnEscape(event) {
      if (event.key === 'Escape') {
        setSelectedImageIndex(null)
      }
    }

    document.body.style.overflow = 'hidden'
    document.addEventListener('keydown', closeOnEscape)

    return () => {
      document.body.style.overflow = ''
      document.removeEventListener('keydown', closeOnEscape)
    }
  }, [selectedImage])

  return (
    <article className="about-page">
      <section
        className="about-hero"
        style={{ '--about-hero-image': `url("${aboutBackground}")` }}
        aria-labelledby="about-page-title"
      >
        <img className="about-smoke" src={smokeOne} alt="" aria-hidden="true" />

        <div className="about-hero-content">
          <h1 id="about-page-title">{t('aboutPage.heroTitle')}</h1>
          <div className="about-hero-rule" aria-hidden="true">
            <span />
            <img src={theatreIcon} alt="" aria-hidden="true" />
            <span />
          </div>
          <p>{t('aboutPage.heroSubtitle')}</p>
        </div>
      </section>

      <section className="about-intro-section" aria-label={t('aboutPage.introLabel')}>
        <div className="about-intro-inner">
          <div className="about-intro-copy">
            <p>{t('aboutPage.intro.paragraph1')}</p>
            <p>{t('aboutPage.intro.paragraph2')}</p>
            <p>{t('aboutPage.intro.paragraph3')}</p>
          </div>

          <figure className="about-framed-image">
            <img src={aboutTheatreImage} alt={t('aboutPage.intro.imageAlt')} loading="lazy" />
          </figure>
        </div>
      </section>

      <section className="about-stats-section" aria-label={t('aboutPage.statsLabel')}>
        <dl className="about-stats-panel">
          {stats.map((item) => (
            <div className="about-stat" key={item.label}>
              <img src={item.icon} alt="" aria-hidden="true" />
              <dd>{item.value}</dd>
              <dt>{item.label}</dt>
            </div>
          ))}
        </dl>
      </section>

      <section
        className="about-quote-band"
        style={{ '--about-quote-image': `url("${quoteBackground}")` }}
        aria-label={t('aboutPage.quoteLabel')}
      >
        <blockquote>
          <p>{t('aboutPage.quote.text')}</p>
          <cite>{t('aboutPage.quote.author')}</cite>
        </blockquote>
      </section>

      <section className="about-gallery-preview" aria-labelledby="about-gallery-title">
        <div className="about-gallery-inner">
          <h2 id="about-gallery-title">{t('aboutPage.galleryTitle')}</h2>
          <div className="about-gallery-rule" aria-hidden="true" />
          <div className="about-gallery-grid">
            {galleryImages.map((image, index) => (
              <figure className="about-gallery-item" key={image.src}>
                <button
                  type="button"
                  className="about-gallery-trigger"
                  onClick={() => setSelectedImageIndex(index)}
                  aria-label={t('aboutPage.openGalleryImage', { number: index + 1 })}
                >
                  <img src={image.src} alt={image.alt} loading="lazy" />
                </button>
              </figure>
            ))}
          </div>
          <Link className="about-gallery-button" to={getLocalizedPath('gallery', language)}>
            <span>{t('aboutPage.viewMoreGallery')}</span>
            <ArrowRightIcon className="about-gallery-button-icon" />
          </Link>
        </div>
      </section>

      {homeMeta && <ReservationBanner home={homeMeta} />}

      {selectedImage && (
        <div
          className="gallery-lightbox"
          role="dialog"
          aria-modal="true"
          aria-label={selectedImage.alt}
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              setSelectedImageIndex(null)
            }
          }}
        >
          <button
            type="button"
            className="gallery-lightbox-close"
            onClick={() => setSelectedImageIndex(null)}
            aria-label={t('aboutPage.closeGalleryPreview')}
          >
            ×
          </button>
          <img src={selectedImage.src} alt={selectedImage.alt} />
        </div>
      )}
    </article>
  )
}
