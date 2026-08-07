import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import galleryHeader from '../assets/gallery-header.jpg'
import smoke from '../assets/smoke_3.png'
import theatreIcon from '../assets/acting-icon-gold.png'
import { getGalleryImages } from '../api/gallery'
import { resolveMediaUrl } from '../api/client'
import { getStaticPage } from '../api/staticPages'
import { useParams } from 'react-router-dom'
import { ArrowRightIcon } from '../components/icons/ArrowRightIcon'

const INITIAL_IMAGE_COUNT = 12
const IMAGE_BATCH_SIZE = 8
export function GalleryPage() {
  const { t, i18n } = useTranslation()
  const { language = 'sq' } = useParams()
  const [pageCopy, setPageCopy] = useState(null)
  const [visibleCount, setVisibleCount] = useState(INITIAL_IMAGE_COUNT)
  const [selectedImageIndex, setSelectedImageIndex] = useState(null)
  const [uploadedImages, setUploadedImages] = useState([])
  useEffect(() => { const controller = new AbortController(); getStaticPage(language, 'gallery-introduction', controller.signal).then(setPageCopy).catch(() => setPageCopy(null)); return () => controller.abort() }, [language])

  useEffect(() => {
    const controller = new AbortController()
    getGalleryImages(i18n.language?.startsWith('en') ? 'en' : 'sq', controller.signal)
      .then(items => setUploadedImages(items.map(item => ({ src: resolveMediaUrl(item.fileUrl), showSlug: 'theatre', index: item.id, altText: item.altText }))))
      .catch(error => { if (error.name !== 'CanceledError' && error.code !== 'ERR_CANCELED') setUploadedImages([]) })
    return () => controller.abort()
  }, [i18n.language])

  const images = useMemo(() => uploadedImages, [uploadedImages])

  useEffect(() => {
    if (selectedImageIndex === null) {
      return undefined
    }

    function onKeyDown(event) {
      if (event.key === 'Escape') {
        setSelectedImageIndex(null)
      } else if (event.key === 'ArrowLeft') {
        setSelectedImageIndex((current) => (current - 1 + images.length) % images.length)
      } else if (event.key === 'ArrowRight') {
        setSelectedImageIndex((current) => (current + 1) % images.length)
      }
    }

    document.body.style.overflow = 'hidden'
    document.addEventListener('keydown', onKeyDown)

    return () => {
      document.body.style.overflow = ''
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [images.length, selectedImageIndex])

  return (
    <article className="gallery-page">
      <section
        className="gallery-page-hero page-hero"
        style={{ '--page-hero-image': `url("${resolveMediaUrl(pageCopy?.headerImageUrl) || galleryHeader}")` }}
        aria-labelledby="gallery-page-title"
      >
        <img className="page-hero-smoke" src={smoke} alt="" aria-hidden="true" />
        <div className="page-hero-content">
          <h1 id="gallery-page-title">{pageCopy?.title || t('galleryPage.heroTitle')}</h1>
          <div className="page-hero-rule" aria-hidden="true">
            <span />
            <img src={theatreIcon} alt="" aria-hidden="true" />
            <span />
          </div>
          <p>{pageCopy?.subtitle || t('galleryPage.heroSubtitle')}</p>
        </div>
      </section>

      <section className="gallery-page-content" aria-label={t('galleryPage.imagesLabel')}>
        <div className="gallery-page-grid">
          {images.slice(0, visibleCount).map((image, index) => (
            <button
              type="button"
              className="gallery-page-item"
              key={image.src}
              onClick={() => setSelectedImageIndex(index)}
              aria-label={t('galleryPage.openImage', { number: index + 1 })}
            >
              <img
                src={image.src}
                alt={image.altText || t('galleryPage.imageAlt', { number: image.index + 1 })}
                loading="lazy"
              />
            </button>
          ))}
        </div>

        {visibleCount < images.length && (
          <button
            type="button"
            className="gallery-page-more"
            onClick={() => setVisibleCount((count) => Math.min(count + IMAGE_BATCH_SIZE, images.length))}
          >
            {t('galleryPage.showMore')}
          </button>
        )}
      </section>

      {selectedImageIndex !== null && (
        <div
          className="gallery-lightbox gallery-page-lightbox"
          role="dialog"
          aria-modal="true"
          aria-label={t('galleryPage.previewLabel')}
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
            aria-label={t('galleryPage.closePreview')}
          >
            ×
          </button>
          <button
            type="button"
            className="gallery-page-lightbox-nav previous"
            onClick={() => setSelectedImageIndex(
              (selectedImageIndex - 1 + images.length) % images.length,
            )}
            aria-label={t('galleryPage.previousImage')}
          >
            <ArrowRightIcon className="direction-arrow direction-arrow-left" />
          </button>
          <img
            src={images[selectedImageIndex].src}
            alt={t('galleryPage.imageAlt', { number: images[selectedImageIndex].index + 1 })}
          />
          <button
            type="button"
            className="gallery-page-lightbox-nav next"
            onClick={() => setSelectedImageIndex((selectedImageIndex + 1) % images.length)}
            aria-label={t('galleryPage.nextImage')}
          >
            <ArrowRightIcon className="direction-arrow" />
          </button>
        </div>
      )}
    </article>
  )
}
