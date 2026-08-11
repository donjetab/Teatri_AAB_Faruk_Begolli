import { Outlet, useLocation, useParams } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Footer } from './Footer'
import { Header } from './Header'
import { getHome } from '../../api/home'
import { getNavigation } from '../../api/navigation'
import { getStaticPage } from '../../api/staticPages'
import { getLanguageFromPath, getLocalizedPath, getRouteKey } from '../../routes/localizedRoutes'
import { setPageMetadata, setStructuredData } from '../../utils/seo'

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
    reserve: {
      title: 'Rezervo biletën | Teatri AAB “Faruk Begolli”',
      description: 'Rezervoni biletën për shfaqjet e ardhshme të Teatrit AAB “Faruk Begolli”.',
    },
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
    reserve: {
      title: 'Reserve a ticket | AAB Theatre “Faruk Begolli”',
      description: 'Reserve a ticket for upcoming performances at AAB Theatre “Faruk Begolli”.',
    },
  },
}

const staticPageKeys = { about: 'about', shows: 'shows-introduction', news: 'news-introduction', pitf: 'pitf-introduction', gallery: 'gallery-introduction', contact: 'contact', location: 'contact', reserve: 'reservations' }

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

export function AppLayout() {
  const { i18n, t } = useTranslation()
  const params = useParams()
  const location = useLocation()
  const language = params.language ?? getLanguageFromPath(location.pathname)
  const routeKey = getRouteKey(location.pathname)
  const [homepageMeta, setHomepageMeta] = useState(null)
  const [navigation, setNavigation] = useState(null)
  const [managedPage, setManagedPage] = useState(null)
  const [showBackToTop, setShowBackToTop] = useState(false)
  const [isHeaderScrolled, setIsHeaderScrolled] = useState(false)

  useEffect(() => {
    window.scrollTo({ top: 0, left: 0, behavior: 'auto' })
  }, [location.pathname])

  useEffect(() => {
    const updateScrollState = () => {
      setShowBackToTop(window.scrollY > 300)
      setIsHeaderScrolled(window.scrollY > 16)
    }

    updateScrollState()
    window.addEventListener('scroll', updateScrollState, { passive: true })

    return () => window.removeEventListener('scroll', updateScrollState)
  }, [])

  const scrollToTop = () => {
    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches
    window.scrollTo({ top: 0, left: 0, behavior: prefersReducedMotion ? 'auto' : 'smooth' })
  }

  useEffect(() => {
    if (i18n.language !== language) {
      i18n.changeLanguage(language)
    }
    localStorage.setItem('aab-theatre-language', language)
    document.documentElement.lang = language
  }, [i18n, language])

  useEffect(() => {
    const controller = new AbortController()

    setHomepageMeta(null)
    getHome(language, controller.signal)
      .then((data) => {
        setHomepageMeta({
          theatreName: data.theatreName,
          address: data.address,
          phone: data.phone,
          email: data.email,
          reservationUrl: data.reservationUrl,
          facebookUrl: data.facebookUrl,
          instagramUrl: data.instagramUrl,
          facebookDisplayName: data.facebookDisplayName,
          instagramDisplayName: data.instagramDisplayName,
          logoUrl: data.logoUrl,
          footerLogoUrl: data.footerLogoUrl,
          footerCopyrightText: data.footerCopyrightText,
        })
      })
      .catch((error) => {
        if (error.name !== 'CanceledError' && error.code !== 'ERR_CANCELED') {
          setHomepageMeta(null)
        }
      })

    return () => controller.abort()
  }, [language])

  useEffect(() => {
    const controller = new AbortController()
    getNavigation(controller.signal).then(setNavigation).catch(() => setNavigation(null))
    return () => controller.abort()
  }, [])

  useEffect(() => {
    const pageKey = staticPageKeys[routeKey]
    if (!pageKey) { setManagedPage(null); return undefined }
    const controller = new AbortController()
    getStaticPage(language, pageKey, controller.signal).then(setManagedPage).catch(() => setManagedPage(null))
    return () => controller.abort()
  }, [language, routeKey])

  useEffect(() => {
    const content = seoContent[language]?.[routeKey] ?? seoContent.sq.home
    const origin = window.location.origin
    const basePath = import.meta.env.BASE_URL.replace(/\/$/, '')

    setPageMetadata({ title: managedPage?.seoTitle || content.title, description: managedPage?.seoDescription || content.description, image: managedPage?.socialSharingImageUrl })

    upsertLink('canonical', {
      href: `${origin}${basePath}${getLocalizedPath(routeKey, language)}`,
    })
    upsertLink('alternate', {
      hreflang: 'sq',
      href: `${origin}${basePath}${getLocalizedPath(routeKey, 'sq')}`,
    })
    upsertLink('alternate', {
      hreflang: 'en',
      href: `${origin}${basePath}${getLocalizedPath(routeKey, 'en')}`,
    })
    upsertLink('alternate', {
      hreflang: 'x-default',
      href: `${origin}${basePath}${getLocalizedPath(routeKey, 'sq')}`,
    })
  }, [language, managedPage, routeKey])

  useEffect(() => {
    if (!homepageMeta) return undefined
    return setStructuredData('theatre', {
      '@context': 'https://schema.org',
      '@type': 'PerformingArtsTheater',
      name: homepageMeta.theatreName,
      url: window.location.origin,
      address: homepageMeta.address,
      telephone: homepageMeta.phone,
      email: homepageMeta.email,
      sameAs: [homepageMeta.facebookUrl, homepageMeta.instagramUrl].filter(Boolean),
    })
  }, [homepageMeta])

  useEffect(() => {
    const main = document.querySelector('.site-main')
    if (!main || window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      return undefined
    }

    const revealSelector = [
      '.site-main > * > section',
      '.show-card',
      '.shows-page-card',
      '.news-card',
      '.gallery-page-item',
      '.about-gallery-item',
      '.reserve-show-card',
      '.contact-direct-grid > a',
    ].join(', ')

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add('is-revealed')
            observer.unobserve(entry.target)
          }
        })
      },
      { threshold: 0.01, rootMargin: '0px 0px -3% 0px' },
    )

    const revealVisibleElements = () => {
      main.querySelectorAll('.scroll-reveal:not(.is-revealed)').forEach((element) => {
        const bounds = element.getBoundingClientRect()
        if (bounds.top < window.innerHeight * 0.92 && bounds.bottom > 0) {
          element.classList.add('is-revealed')
          observer.unobserve(element)
        }
      })
    }

    const registerRevealElements = () => {
      const firstPageSection = main.querySelector('section')

      main.querySelectorAll(revealSelector).forEach((element, index) => {
        if (element.classList.contains('scroll-reveal')) {
          return
        }

        element.classList.add('scroll-reveal')
        element.style.setProperty('--reveal-index', index % 6)

        if (element === firstPageSection) {
          element.classList.add('is-revealed', 'reveal-immediate')
        } else {
          observer.observe(element)
        }
      })

      window.requestAnimationFrame(revealVisibleElements)
    }

    registerRevealElements()

    const mutationObserver = new MutationObserver(registerRevealElements)
    mutationObserver.observe(main, { childList: true, subtree: true })
    window.addEventListener('scroll', revealVisibleElements, { passive: true })
    window.addEventListener('resize', revealVisibleElements)

    return () => {
      mutationObserver.disconnect()
      observer.disconnect()
      window.removeEventListener('scroll', revealVisibleElements)
      window.removeEventListener('resize', revealVisibleElements)
    }
  }, [location.pathname])

  return (
    <div className={`site-shell site-shell-${routeKey}`}>
      <Header language={language} isScrolled={isHeaderScrolled} navigation={navigation} logoUrl={homepageMeta?.logoUrl} />
      <main className="site-main" id="content">
        <Outlet />
      </main>
      <Footer language={language} homepageMeta={homepageMeta} navigation={navigation} />
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
    </div>
  )
}
