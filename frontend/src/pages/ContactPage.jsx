import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { apiClient, resolveMediaUrl } from '../api/client'
import { getHome } from '../api/home'
import { ReservationBanner } from '../components/home/ReservationBanner'
import contactHeader from '../assets/Kolegji-AAB.jpg'
import smoke from '../assets/smoke_3.png'
import theatreIcon from '../assets/acting-icon-gold.png'
import { getStaticPage } from '../api/staticPages'

const theatreMapEmbedUrl = 'https://www.google.com/maps/embed?pb=!1m14!1m8!1m3!1d11740.15791744908!2d21.112945!3d42.639323!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x13549f005d591c87%3A0x2473b114eef9fd14!2zVGVhdHJpIEFBQiDigJxGYXJ1ayBCZWdvbGxp4oCd!5e0!3m2!1sen!2sus!4v1785141268194!5m2!1sen!2sus'
const theatreMapUrl = 'https://www.google.com/maps/search/?api=1&query=42.6389837%2C21.1126562'

function InstagramIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <rect x="3" y="3" width="18" height="18" rx="5" />
      <circle cx="12" cy="12" r="4" />
      <circle cx="17.5" cy="6.5" r="1" className="filled" />
    </svg>
  )
}

function FacebookIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path className="filled" d="M14.2 21v-8h2.7l.4-3h-3.1V8.1c0-.9.3-1.5 1.6-1.5h1.7V3.9c-.3 0-1.3-.1-2.5-.1-2.5 0-4.2 1.5-4.2 4.4V10H8v3h2.8v8h3.4Z" />
    </svg>
  )
}

function PhoneIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path className="filled" d="M6.6 2.8 9.4 7a1.5 1.5 0 0 1-.2 1.8l-1.5 1.5a15.5 15.5 0 0 0 6 6l1.5-1.5a1.5 1.5 0 0 1 1.8-.2l4.2 2.8a1.5 1.5 0 0 1 .7 1.5V21a1.5 1.5 0 0 1-1.5 1.5C10 22.5 1.5 14 1.5 3.6A1.5 1.5 0 0 1 3 2.1h2.1a1.5 1.5 0 0 1 1.5.7Z" />
    </svg>
  )
}

export function ContactPage() {
  const { t } = useTranslation()
  const { language = 'sq' } = useParams()
  const [home, setHome] = useState(null)
  const [pageCopy, setPageCopy] = useState(null)
  const [status, setStatus] = useState('idle')
  const [form, setForm] = useState({ name: '', email: '', subject: '', message: '' })

  useEffect(() => {
    const controller = new AbortController()
    getHome(language, controller.signal).then(setHome).catch(() => setHome(null))
    return () => controller.abort()
  }, [language])
  useEffect(() => { const controller = new AbortController(); getStaticPage(language, 'contact', controller.signal).then(setPageCopy).catch(() => setPageCopy(null)); return () => controller.abort() }, [language])

  function updateField(event) {
    setForm((current) => ({ ...current, [event.target.name]: event.target.value }))
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setStatus('loading')

    try {
      await apiClient.post('/api/contact', { ...form, languageCode: language })
      setForm({ name: '', email: '', subject: '', message: '' })
      setStatus('success')
    } catch {
      setStatus('error')
    }
  }

  const address = home?.address ?? t('contactPage.addressFallback')
  const phone = home?.phone ?? '+383 48 999 000'

  return (
    <article className="contact-page">
      <section
        className="contact-page-hero page-hero"
        style={{ '--page-hero-image': `url("${resolveMediaUrl(pageCopy?.headerImageUrl) || contactHeader}")` }}
        aria-labelledby="contact-page-title"
      >
        <img className="page-hero-smoke" src={smoke} alt="" aria-hidden="true" />
        <div className="page-hero-content">
          <h1 id="contact-page-title">{pageCopy?.title || t('contactPage.heroTitle')}</h1>
          <div className="page-hero-rule" aria-hidden="true">
            <span />
            <img src={theatreIcon} alt="" aria-hidden="true" />
            <span />
          </div>
          <p>{pageCopy?.subtitle || t('contactPage.heroSubtitle')}</p>
        </div>
      </section>

      <section className="contact-main-section">
        <div className="contact-main-grid">
          <section className="contact-location-card" aria-labelledby="contact-location-title">
            <h2 id="contact-location-title">{t('contactPage.location')}</h2>
            <iframe
              title={t('contactPage.mapTitle')}
              src={pageCopy?.mapEmbedUrl || theatreMapEmbedUrl}
              loading="lazy"
              referrerPolicy="strict-origin-when-cross-origin"
              allowFullScreen
            />
            <a
              className="contact-address"
              href={pageCopy?.mapLinkUrl || theatreMapUrl}
              target="_blank"
              rel="noreferrer"
            >
              <span aria-hidden="true">⌖</span>
              <span>{address}</span>
            </a>
          </section>

          <section className="contact-form-section" aria-labelledby="contact-form-title">
            <h2 id="contact-form-title">{t('contactPage.sendMessage')}</h2>
            <form className="contact-form" onSubmit={handleSubmit}>
              <div className="contact-form-row">
                <label>
                  <span className="sr-only">{t('contactPage.name')}</span>
                  <input
                    name="name"
                    value={form.name}
                    onChange={updateField}
                    placeholder={t('contactPage.name')}
                    maxLength="160"
                    required
                  />
                </label>
                <label>
                  <span className="sr-only">{t('contactPage.email')}</span>
                  <input
                    type="email"
                    name="email"
                    value={form.email}
                    onChange={updateField}
                    placeholder={t('contactPage.email')}
                    maxLength="180"
                    required
                  />
                </label>
              </div>
              <label>
                <span className="sr-only">{t('contactPage.subject')}</span>
                <input
                  name="subject"
                  value={form.subject}
                  onChange={updateField}
                  placeholder={t('contactPage.subject')}
                  maxLength="220"
                  required
                />
              </label>
              <label>
                <span className="sr-only">{t('contactPage.message')}</span>
                <textarea
                  name="message"
                  value={form.message}
                  onChange={updateField}
                  placeholder={t('contactPage.message')}
                  maxLength="5000"
                  required
                />
              </label>
              <button type="submit" disabled={status === 'loading'}>
                {status === 'loading' ? t('contactPage.sending') : t('contactPage.send')}
              </button>
              {status === 'success' && <p className="contact-form-status success">{t('contactPage.success')}</p>}
              {status === 'error' && <p className="contact-form-status error">{t('contactPage.error')}</p>}
            </form>
          </section>
        </div>

        <section className="contact-direct-section" aria-labelledby="contact-direct-title">
          <h2 id="contact-direct-title">{t('contactPage.directTitle')}</h2>
          <div className="contact-direct-grid">
            <a href={home?.instagramUrl ?? 'https://www.instagram.com/aabtheatre'} target="_blank" rel="noreferrer">
              <InstagramIcon />
              <small>Instagram</small>
              <strong>{home?.instagramDisplayName || '@teatriaabfarukbegolli'}</strong>
              <span aria-hidden="true">⟶</span>
            </a>
            <a href={`tel:${phone.replace(/\s/g, '')}`}>
              <PhoneIcon />
              <small>{t('contactPage.phone')}</small>
              <strong>{phone}</strong>
              <span aria-hidden="true">⟶</span>
            </a>
            <a href={home?.facebookUrl ?? 'https://www.facebook.com/aabtheatre'} target="_blank" rel="noreferrer">
              <FacebookIcon />
              <small>Facebook</small>
              <strong>{home?.facebookDisplayName || 'Teatri AAB Faruk Begolli'}</strong>
              <span aria-hidden="true">⟶</span>
            </a>
          </div>
        </section>
      </section>

      {home && <ReservationBanner home={home} />}
    </article>
  )
}
