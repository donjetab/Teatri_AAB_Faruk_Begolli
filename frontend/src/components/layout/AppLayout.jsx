import { Outlet, useLocation, useParams } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Footer } from './Footer'
import { Header } from './Header'
import { getLanguageFromPath, getLocalizedPath, getRouteKey } from '../../routes/localizedRoutes'

const seoContent = {
  sq: {
    home: {
      title: 'Teatri AAB “Faruk Begolli”',
      description: 'Faqja zyrtare e Teatrit AAB “Faruk Begolli” në Kosovë.',
    },
    about: {
      title: 'Për Ne | Teatri AAB “Faruk Begolli”',
      description: 'Mësoni më shumë për Teatrin AAB “Faruk Begolli” në Kosovë.',
    },
    shows: {
      title: 'Shfaqjet | Teatri AAB “Faruk Begolli”',
      description: 'Programi dhe shfaqjet e Teatrit AAB “Faruk Begolli”.',
    },
    news: {
      title: 'Lajme | Teatri AAB “Faruk Begolli”',
      description: 'Lajmet dhe njoftimet më të fundit nga Teatri AAB “Faruk Begolli”.',
    },
    pitf: {
      title: 'PITF | Teatri AAB “Faruk Begolli”',
      description: 'Prishtina International Theatre Festival pranë Teatrit AAB “Faruk Begolli”.',
    },
    gallery: {
      title: 'Galeria | Teatri AAB “Faruk Begolli”',
      description: 'Fotografi dhe momente nga Teatri AAB “Faruk Begolli”.',
    },
    contact: {
      title: 'Kontakti | Teatri AAB “Faruk Begolli”',
      description: 'Kontaktoni Teatrin AAB “Faruk Begolli” në Kosovë.',
    },
<<<<<<< HEAD
=======
    reserve: {
      title: 'Rezervo biletën | Teatri AAB “Faruk Begolli”',
      description: 'Rezervoni biletën për shfaqjet e ardhshme të Teatrit AAB “Faruk Begolli”.',
    },
>>>>>>> 6f8afa3 (front pages almost done)
  },
  en: {
    home: {
      title: 'AAB Theatre “Faruk Begolli”',
      description: 'Official website of AAB Theatre “Faruk Begolli” in Kosovo.',
    },
    about: {
      title: 'About | AAB Theatre “Faruk Begolli”',
      description: 'Learn more about AAB Theatre “Faruk Begolli” in Kosovo.',
    },
    shows: {
      title: 'Shows | AAB Theatre “Faruk Begolli”',
      description: 'Program and performances at AAB Theatre “Faruk Begolli”.',
    },
    news: {
      title: 'News | AAB Theatre “Faruk Begolli”',
      description: 'Latest news and updates from AAB Theatre “Faruk Begolli”.',
    },
    pitf: {
      title: 'PITF | AAB Theatre “Faruk Begolli”',
      description: 'Prishtina International Theatre Festival at AAB Theatre “Faruk Begolli”.',
    },
    gallery: {
      title: 'Gallery | AAB Theatre “Faruk Begolli”',
      description: 'Photos and moments from AAB Theatre “Faruk Begolli”.',
    },
    contact: {
      title: 'Contact | AAB Theatre “Faruk Begolli”',
      description: 'Contact AAB Theatre “Faruk Begolli” in Kosovo.',
    },
<<<<<<< HEAD
=======
    reserve: {
      title: 'Reserve a ticket | AAB Theatre “Faruk Begolli”',
      description: 'Reserve a ticket for upcoming performances at AAB Theatre “Faruk Begolli”.',
    },
>>>>>>> 6f8afa3 (front pages almost done)
  },
}

function upsertLink(rel, attributes) {
  const selector = Object.entries({ rel, ...attributes })
    .filter(([key]) => key !== 'href')
    .map(([key, value]) => `[${key}="${value}"]`)
    .join('')
  let link = document.head.querySelector(`link[rel="${rel}"]${selector}`)

  if (!link) {
    link = document.createElement('link')
    link.rel = rel
    Object.entries(attributes).forEach(([key, value]) => {
      if (key !== 'href') {
        link.setAttribute(key, value)
      }
    })
    document.head.appendChild(link)
  }

  link.href = attributes.href
}

function setMetaDescription(description) {
  let meta = document.head.querySelector('meta[name="description"]')
  if (!meta) {
    meta = document.createElement('meta')
    meta.name = 'description'
    document.head.appendChild(meta)
  }
  meta.content = description
}

export function AppLayout() {
<<<<<<< HEAD
  const { i18n } = useTranslation()
=======
  const { i18n, t } = useTranslation()
>>>>>>> 6f8afa3 (front pages almost done)
  const params = useParams()
  const location = useLocation()
  const language = params.language ?? getLanguageFromPath(location.pathname)
  const routeKey = getRouteKey(location.pathname)
  const [homepageMeta, setHomepageMeta] = useState(null)
<<<<<<< HEAD
=======
  const [showBackToTop, setShowBackToTop] = useState(false)

  useEffect(() => {
    window.scrollTo({ top: 0, left: 0, behavior: 'auto' })
  }, [location.pathname])

  useEffect(() => {
    const updateBackToTopVisibility = () => setShowBackToTop(window.scrollY > 300)

    updateBackToTopVisibility()
    window.addEventListener('scroll', updateBackToTopVisibility, { passive: true })

    return () => window.removeEventListener('scroll', updateBackToTopVisibility)
  }, [])

  const scrollToTop = () => {
    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches
    window.scrollTo({ top: 0, left: 0, behavior: prefersReducedMotion ? 'auto' : 'smooth' })
  }
>>>>>>> 6f8afa3 (front pages almost done)

  useEffect(() => {
    if (i18n.language !== language) {
      i18n.changeLanguage(language)
    }
    localStorage.setItem('aab-theatre-language', language)
    document.documentElement.lang = language
  }, [i18n, language])

  useEffect(() => {
    setHomepageMeta(null)
  }, [language])

  useEffect(() => {
    const content = seoContent[language]?.[routeKey] ?? seoContent.sq.home
    const origin = window.location.origin

    document.title = content.title
    setMetaDescription(content.description)

    upsertLink('canonical', {
      href: `${origin}${getLocalizedPath(routeKey, language)}`,
    })
    upsertLink('alternate', {
      hreflang: 'sq',
      href: `${origin}${getLocalizedPath(routeKey, 'sq')}`,
    })
    upsertLink('alternate', {
      hreflang: 'en',
      href: `${origin}${getLocalizedPath(routeKey, 'en')}`,
    })
    upsertLink('alternate', {
      hreflang: 'x-default',
      href: `${origin}${getLocalizedPath(routeKey, 'sq')}`,
    })
  }, [language, routeKey])

  return (
    <div className={`site-shell site-shell-${routeKey}`}>
      <Header language={language} reservationUrl={homepageMeta?.reservationUrl} />
      <main className="site-main" id="content">
        <Outlet context={{ setHomepageMeta }} />
      </main>
      <Footer language={language} homepageMeta={homepageMeta} />
<<<<<<< HEAD
=======
      <button
        type="button"
        className={`back-to-top${showBackToTop ? ' visible' : ''}`}
        onClick={scrollToTop}
        aria-label={t('a11y.backToTop')}
        tabIndex={showBackToTop ? 0 : -1}
      >
        <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
          <path d="M5 15l7-7 7 7" />
        </svg>
      </button>
>>>>>>> 6f8afa3 (front pages almost done)
    </div>
  )
}
