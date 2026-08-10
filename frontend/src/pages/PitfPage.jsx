import { useEffect, useRef, useState } from 'react'
import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { getHome } from '../api/home'
import { getPitf } from '../api/pitf'
import { resolveMediaUrl } from '../api/client'
import { ReservationBanner } from '../components/home/ReservationBanner'
import { ArrowRightIcon } from '../components/icons/ArrowRightIcon'
import pitfHeader from '../assets/pitf-header.jpg'
import pitfPicture from '../assets/pitf-pic.jpg'
import theatreIcon from '../assets/acting-icon-gold.png'
import smoke from '../assets/smoke_3.png'
import { getStaticPage } from '../api/staticPages'

export function PitfPage() {
  const { t } = useTranslation()
  const { language = 'sq' } = useParams()
  const [homeMeta, setHomeMeta] = useState(null)
  const [pitf, setPitf] = useState(null)
  const [pitfLoading, setPitfLoading] = useState(true)
  const [pageCopy, setPageCopy] = useState(null)
  const [editionsVisible, setEditionsVisible] = useState(false)
  const editionsRef = useRef(null)

  useEffect(() => {
    let controller
    const load = () => {
      controller?.abort()
      controller = new AbortController()
      setPitfLoading(true)
      Promise.all([getHome(language, controller.signal), getPitf(language, controller.signal)])
        .then(([home, page]) => { setHomeMeta(home); setPitf(page) })
        .catch(error => {
          if (error.name !== 'CanceledError' && error.code !== 'ERR_CANCELED') {
            setHomeMeta(null); setPitf(null)
          }
        })
        .finally(() => { if (!controller.signal.aborted) setPitfLoading(false) })
    }
    const refreshWhenVisible = () => {
      if (document.visibilityState === 'visible') load()
    }
    load()
    window.addEventListener('focus', load)
    document.addEventListener('visibilitychange', refreshWhenVisible)
    return () => {
      controller?.abort()
      window.removeEventListener('focus', load)
      document.removeEventListener('visibilitychange', refreshWhenVisible)
    }
  }, [language])
  useEffect(() => { const controller = new AbortController(); getStaticPage(language, 'pitf-introduction', controller.signal).then(setPageCopy).catch(() => setPageCopy(null)); return () => controller.abort() }, [language])

  useEffect(() => {
    const section = editionsRef.current
    if (!section) return undefined
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      setEditionsVisible(true); return undefined
    }
    const observer = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting) { setEditionsVisible(true); observer.disconnect() }
    }, { threshold: 0.12 })
    observer.observe(section)
    return () => observer.disconnect()
  }, [pitf])

  const pageImage = pitf?.imageUrl ? resolveMediaUrl(pitf.imageUrl) : pitfPicture
  const description = pitf?.description || `${t('pitfPage.intro.paragraph1')}\n\n${t('pitfPage.intro.paragraph2')}`
  const editions = pitf?.editions ?? []

  return <article className="pitf-page">
    <section className="pitf-page-hero about-hero" style={{ '--about-hero-image': `url("${resolveMediaUrl(pageCopy?.headerImageUrl) || pitfHeader}")` }} aria-labelledby="pitf-page-title">
      <img className="about-smoke" src={smoke} alt="" aria-hidden="true" />
      <div className="about-hero-content"><h1 id="pitf-page-title">{pageCopy?.title || 'PITF'}</h1><div className="about-hero-rule" aria-hidden="true"><span /><img src={theatreIcon} alt="" /><span /></div><p>{pageCopy?.subtitle || t('pitfPage.fullName')}</p></div>
    </section>
    <section className="pitf-page-about" aria-label={t('pitfPage.aboutLabel')}><div className="pitf-page-about-inner"><figure className="pitf-page-picture"><img src={pageImage} alt={pitf?.title || t('pitfPage.imageAlt')} loading="lazy" /></figure><div className="pitf-page-copy">{description.split(/\r?\n\r?\n/).filter(Boolean).map((paragraph, index) => <p key={index}>{paragraph}</p>)}</div></div></section>
    <section ref={editionsRef} className={`pitf-editions-section${editionsVisible ? ' is-visible' : ''}`} aria-labelledby="pitf-editions-title">
      <header className="pitf-editions-heading"><p>{t('pitfPage.editionsEyebrow')}</p><h2 id="pitf-editions-title">{t('pitfPage.editionsTitle')}</h2></header>
      <ol className="pitf-editions-grid">
        {pitfLoading && Array.from({ length: 5 }, (_, index) => (
          <li className="pitf-edition pitf-edition-skeleton" key={index} style={{ '--edition-index': index }} aria-hidden="true">
            <div><span className="pitf-edition-image" /><span className="pitf-edition-marker" /><strong>&nbsp;</strong><time>&nbsp;</time></div>
          </li>
        ))}
        {editions.map((edition, index) => {
          const content = <><div className="pitf-edition-image">{edition.imageUrl ? <img src={resolveMediaUrl(edition.imageUrl)} alt={edition.name} loading="lazy" /> : <span>{edition.name}</span>}</div><span className="pitf-edition-marker" aria-hidden="true" /><strong>{edition.editionNumber.toString().padStart(2, '0')}</strong><time dateTime={edition.year.toString()}>{edition.year}</time></>
          return <li className="pitf-edition" key={edition.id} style={{ '--edition-index': index }}>{edition.destinationUrl ? <a href={edition.destinationUrl} target="_blank" rel="noopener noreferrer" aria-label={`${edition.name}, ${edition.year}`}>{content}</a> : <div>{content}</div>}</li>
        })}
      </ol>
      {pitf?.buttonUrl && <a className="pitf-reserve-button" href={pitf.buttonUrl} target="_blank" rel="noreferrer"><span>{pitf.buttonText || t('pitfPage.learnMore')}</span><span className="circle-arrow" aria-hidden="true"><ArrowRightIcon className="arrow-icon" /></span></a>}
    </section>
    {homeMeta && <ReservationBanner home={homeMeta} />}
  </article>
}
