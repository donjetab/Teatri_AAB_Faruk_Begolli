import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { getShow } from '../api/shows'
import {
  galleriesBySlug,
  heroImagesBySlug,
  postersBySlug,
  trailersBySlug,
} from '../assets/shows/showAssets'
import smoke from '../assets/smoke_3.png'
import theatreIcon from '../assets/acting-icon-gold.png'
import { CalendarIcon, CreditIcon } from '../components/shows/CreditIcon'
import { ErrorState } from '../components/ui/ErrorState'
import { LoadingState } from '../components/ui/LoadingState'

function consolidateCredits(credits) {
  const directors = credits.filter((credit) => credit.typeCode === 'director')
  const otherCredits = credits.filter((credit) => credit.typeCode !== 'director')
  const rolesByPerson = new Map()

  otherCredits.forEach((credit, creditIndex) => {
    credit.people.forEach((person) => {
      const entry = rolesByPerson.get(person) ?? { person, roles: [], order: creditIndex }
      if (!entry.roles.some((role) => role.typeCode === credit.typeCode)) {
        entry.roles.push({ typeCode: credit.typeCode, typeName: credit.typeName })
      }
      rolesByPerson.set(person, entry)
    })
  })

  const groupedCredits = new Map()
  rolesByPerson.forEach((entry) => {
    const key = entry.roles.map((role) => role.typeCode).sort().join('|')
    const group = groupedCredits.get(key) ?? {
      typeCode: entry.roles[0].typeCode,
      typeName: entry.roles.map((role) => role.typeName).join(' / '),
      people: [],
      order: entry.order,
    }
    group.people.push(entry.person)
    group.order = Math.min(group.order, entry.order)
    groupedCredits.set(key, group)
  })

  return [
    ...directors,
    ...[...groupedCredits.values()]
      .sort((left, right) => left.order - right.order)
      .map(({ order: _order, ...credit }) => credit),
  ]
}

export function ShowDetailPage() {
  const { t } = useTranslation()
  const { language = 'sq', slug } = useParams()
  const [show, setShow] = useState(null)
  const [status, setStatus] = useState('loading')
  const [galleryStart, setGalleryStart] = useState(0)
  const [visibleGalleryCount, setVisibleGalleryCount] = useState(3)
  const [selectedImageIndex, setSelectedImageIndex] = useState(null)
  const trailerRef = useRef(null)
  const galleryImages = galleriesBySlug[slug] ?? []
  const trailer = trailersBySlug[slug]

  useEffect(() => {
    const controller = new AbortController()
    setStatus('loading')

    getShow(language, slug, controller.signal)
      .then((data) => {
        setShow(data)
        setStatus('success')
      })
      .catch((error) => {
        if (error.name !== 'CanceledError') {
          setStatus('error')
        }
      })

    return () => controller.abort()
  }, [language, slug])

  useEffect(() => {
    const updateVisibleCount = () => {
      setVisibleGalleryCount(window.innerWidth <= 640 ? 1 : window.innerWidth <= 900 ? 2 : 3)
    }

    updateVisibleCount()
    window.addEventListener('resize', updateVisibleCount)
    return () => window.removeEventListener('resize', updateVisibleCount)
  }, [])

  useEffect(() => {
    setGalleryStart((current) =>
      Math.min(current, Math.max(0, galleryImages.length - visibleGalleryCount)),
    )
  }, [galleryImages.length, visibleGalleryCount])

  useEffect(() => {
    if (selectedImageIndex === null) {
      return undefined
    }

    const onKeyDown = (event) => {
      if (event.key === 'Escape') {
        setSelectedImageIndex(null)
      } else if (event.key === 'ArrowLeft') {
        setSelectedImageIndex((current) =>
          current === null ? null : (current - 1 + galleryImages.length) % galleryImages.length,
        )
      } else if (event.key === 'ArrowRight') {
        setSelectedImageIndex((current) =>
          current === null ? null : (current + 1) % galleryImages.length,
        )
      }
    }

    document.body.style.overflow = 'hidden'
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.body.style.overflow = ''
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [galleryImages.length, selectedImageIndex])

  useEffect(() => {
    const video = trailerRef.current
    if (status !== 'success' || !trailer || !video) {
      return undefined
    }

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          video.muted = true
          video.play().catch(() => {
            // Browsers may still block autoplay based on the user's preferences.
          })
        } else {
          video.pause()
        }
      },
      { threshold: 0.5 },
    )

    observer.observe(video)
    return () => observer.disconnect()
  }, [status, trailer])

  if (status === 'loading') {
    return <section className="show-detail-state"><LoadingState /></section>
  }

  if (status === 'error' || !show) {
    return <section className="show-detail-state"><ErrorState message={t('showDetail.loadError')} /></section>
  }

  const poster = postersBySlug[show.slug]
  const heroImage = heroImagesBySlug[show.slug] ?? poster
  const visibleGalleryImages = galleryImages.slice(
    galleryStart,
    galleryStart + visibleGalleryCount,
  )
  const displayCredits = consolidateCredits(show.credits)
  const premiere = show.premiereDate
    ? new Intl.DateTimeFormat(language === 'en' ? 'en-GB' : 'sq-AL').format(
        new Date(`${show.premiereDate}T00:00:00Z`),
      )
    : null

  return (
    <article
      className="show-detail-page"
      style={{ '--page-hero-image': `url("${heroImage}")` }}
    >
      <section className="show-detail-hero page-hero" aria-labelledby="show-detail-title">
        <img className="page-hero-smoke" src={smoke} alt="" aria-hidden="true" />
        <div className="page-hero-content">
          <h1 id="show-detail-title">{show.title}</h1>
          <div className="page-hero-rule" aria-hidden="true">
            <span />
            <img src={theatreIcon} alt="" />
            <span />
          </div>
        </div>
      </section>

      <section className="show-detail-overview-section" aria-label={show.title}>
        <div className="show-detail-inner">
          <Link
            className="show-detail-back"
            to={`/${language}/${language === 'en' ? 'shows' : 'shfaqjet'}`}
          >
            <span aria-hidden="true">←</span>
            {t('showDetail.backToShows')}
          </Link>
          <div className="show-detail-overview">
            <div className="show-detail-poster-frame">
              <img src={poster} alt={t('showsPage.posterAlt', { title: show.title })} />
            </div>

            <div className="show-detail-facts">
              {premiere && (
                <div className="show-detail-fact">
                  <span className="show-detail-fact-icon"><CalendarIcon /></span>
                  <div>
                    <h2>{t('showDetail.premiere')}</h2>
                    <p>{premiere}</p>
                  </div>
                </div>
              )}

              {displayCredits.map((credit) => (
                <div className="show-detail-fact" key={credit.typeCode}>
                  <span className="show-detail-fact-icon">
                    <CreditIcon type={credit.typeCode} />
                  </span>
                  <div>
                    <h2>{credit.typeName}</h2>
                    <p>{credit.people.join(', ')}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      <section className="show-synopsis-section" aria-labelledby="show-synopsis-title">
        <div className="show-synopsis-panel">
          <span className="show-synopsis-icon">
            <img src={theatreIcon} alt="" aria-hidden="true" />
          </span>
          <div>
            <h2 id="show-synopsis-title">{t('showDetail.synopsis')}</h2>
            <p>{show.synopsis}</p>
          </div>
        </div>
      </section>

      {galleryImages.length > 0 && (
        <section className="show-gallery-section" aria-labelledby="show-gallery-title">
          <div className="show-section-heading">
            <div>
              <h2 id="show-gallery-title">{t('showDetail.gallery')}</h2>
              <span aria-hidden="true" />
            </div>
            <div className="show-gallery-controls">
              <output aria-live="polite">
                {galleryStart + 1}–{Math.min(galleryStart + visibleGalleryCount, galleryImages.length)}
                {' / '}{galleryImages.length}
              </output>
              <button
                type="button"
                onClick={() => setGalleryStart((current) => Math.max(0, current - visibleGalleryCount))}
                disabled={galleryStart === 0}
                aria-label={t('showDetail.previousImages')}
              >
                ‹
              </button>
              <button
                type="button"
                onClick={() => setGalleryStart((current) =>
                  Math.min(galleryImages.length - visibleGalleryCount, current + visibleGalleryCount),
                )}
                disabled={galleryStart + visibleGalleryCount >= galleryImages.length}
                aria-label={t('showDetail.nextImages')}
              >
                ›
              </button>
            </div>
          </div>

          <div
            className="show-gallery-grid"
            style={{ '--show-gallery-columns': visibleGalleryCount }}
          >
            {visibleGalleryImages.map((image, visibleIndex) => {
              const imageIndex = galleryStart + visibleIndex
              return (
                <button
                  type="button"
                  className="show-gallery-item"
                  key={image}
                  onClick={() => setSelectedImageIndex(imageIndex)}
                  aria-label={t('showDetail.openImage', { number: imageIndex + 1 })}
                >
                  <img
                    src={image}
                    alt={t('showDetail.galleryImageAlt', { title: show.title, number: imageIndex + 1 })}
                    loading="lazy"
                  />
                </button>
              )
            })}
          </div>
        </section>
      )}

      {trailer && (
        <section className="show-trailer-section" aria-labelledby="show-trailer-title">
          <div className="show-section-heading">
            <div>
              <h2 id="show-trailer-title">{t('showDetail.trailer')}</h2>
              <span aria-hidden="true" />
            </div>
          </div>
          <div className="show-trailer-frame">
            <video key={trailer} ref={trailerRef} controls muted playsInline preload="auto">
              <source src={trailer} type="video/mp4" />
              {t('showDetail.videoUnsupported')}
            </video>
          </div>
        </section>
      )}

      {selectedImageIndex !== null && (
        <div
          className="gallery-lightbox show-gallery-lightbox"
          role="dialog"
          aria-modal="true"
          aria-label={t('showDetail.galleryImageAlt', {
            title: show.title,
            number: selectedImageIndex + 1,
          })}
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
          <button
            type="button"
            className="show-lightbox-arrow show-lightbox-arrow-previous"
            onClick={() => setSelectedImageIndex(
              (selectedImageIndex - 1 + galleryImages.length) % galleryImages.length,
            )}
            aria-label={t('showDetail.previousImage')}
          >
            ‹
          </button>
          <img
            src={galleryImages[selectedImageIndex]}
            alt={t('showDetail.galleryImageAlt', {
              title: show.title,
              number: selectedImageIndex + 1,
            })}
          />
          <span className="show-lightbox-count">
            {selectedImageIndex + 1} / {galleryImages.length}
          </span>
          <button
            type="button"
            className="show-lightbox-arrow show-lightbox-arrow-next"
            onClick={() => setSelectedImageIndex(
              (selectedImageIndex + 1) % galleryImages.length,
            )}
            aria-label={t('showDetail.nextImage')}
          >
            ›
          </button>
        </div>
      )}
    </article>
  )
}
