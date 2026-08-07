import { useEffect, useMemo, useRef, useState } from 'react'
import { Link, useLocation, useParams, useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { getStaticPage } from '../api/staticPages'
import { getNews } from '../api/news'
import { getHome } from '../api/home'
import { resolveMediaUrl } from '../api/client'
import { getLocalizedPath } from '../routes/localizedRoutes'
import { ReservationBanner } from '../components/home/ReservationBanner'
import newsHeader from '../assets/news-header.jpg'
import smoke from '../assets/smoke_3.png'
import theatreIcon from '../assets/acting-icon-gold.png'
import { ArrowRightIcon } from '../components/icons/ArrowRightIcon'
import { LoadingState } from '../components/ui/LoadingState'
import { ErrorState } from '../components/ui/ErrorState'

const PAGE_SIZE = 9

function VideoThumbnail({ src }) {
  const ref = useRef(null)
  const revealFrame = () => {
    const video = ref.current
    if (!video || !Number.isFinite(video.duration) || video.duration <= 0) return
    video.currentTime = Math.min(0.5, video.duration / 10)
  }
  return <video ref={ref} className="news-card-video" src={src} muted playsInline preload="auto" onLoadedMetadata={revealFrame} aria-hidden="true" />
}

function getPaginationItems(currentPage, pageCount) {
  if (pageCount <= 7) {
    return Array.from({ length: pageCount }, (_, index) => index + 1)
  }
  if (currentPage <= 4) {
    return [1, 2, 3, 4, 5, 'end-ellipsis', pageCount]
  }
  if (currentPage >= pageCount - 3) {
    return [1, 'start-ellipsis', pageCount - 4, pageCount - 3, pageCount - 2, pageCount - 1, pageCount]
  }
  return [1, 'start-ellipsis', currentPage - 1, currentPage, currentPage + 1, 'end-ellipsis', pageCount]
}

export function NewsPage() {
  const { t, i18n } = useTranslation()
  const { language = 'sq' } = useParams()
  const location = useLocation()
  const [searchParams, setSearchParams] = useSearchParams()
  const [articles, setArticles] = useState([])
  const [home, setHome] = useState(null)
  const [status, setStatus] = useState('loading')
  const [pageCopy, setPageCopy] = useState(null)
  const query = searchParams.get('q') ?? ''
  const requestedPage = Number.parseInt(searchParams.get('page') ?? '1', 10)
  const currentPage = Number.isFinite(requestedPage) && requestedPage > 0 ? requestedPage : 1

  useEffect(() => { const controller = new AbortController(); getStaticPage(language, 'news-introduction', controller.signal).then(setPageCopy).catch(() => setPageCopy(null)); return () => controller.abort() }, [language])

  useEffect(() => {
    const controller = new AbortController()
    setStatus('loading')
    getNews(language, controller.signal)
      .then((data) => {
        setArticles(data)
        setStatus('success')
      })
      .catch((error) => {
        if (error.name !== 'CanceledError' && error.code !== 'ERR_CANCELED') {
          setStatus('error')
        }
      })
    getHome(language, controller.signal).then(setHome).catch(() => setHome(null))
    return () => controller.abort()
  }, [language])

  const locale = i18n.language === 'sq' ? 'sq-AL' : 'en-GB'
  const filteredArticles = useMemo(
    () => {
      const normalizedQuery = query.trim().toLocaleLowerCase(locale)
      if (!normalizedQuery) {
        return articles
      }
      return articles.filter((article) =>
        `${article.title} ${article.summary}`.toLocaleLowerCase(locale).includes(normalizedQuery),
      )
    },
    [articles, locale, query],
  )
  const pageCount = Math.max(1, Math.ceil(filteredArticles.length / PAGE_SIZE))
  const safeCurrentPage = Math.min(currentPage, pageCount)
  const visibleArticles = filteredArticles.slice(
    (safeCurrentPage - 1) * PAGE_SIZE,
    safeCurrentPage * PAGE_SIZE,
  )
  const paginationItems = getPaginationItems(safeCurrentPage, pageCount)

  function updateQuery(event) {
    const nextParams = new URLSearchParams(searchParams)
    const nextQuery = event.target.value
    if (nextQuery) {
      nextParams.set('q', nextQuery)
    } else {
      nextParams.delete('q')
    }
    nextParams.delete('page')
    setSearchParams(nextParams, { replace: true })
  }

  function goToPage(page) {
    const nextParams = new URLSearchParams(searchParams)
    if (page === 1) {
      nextParams.delete('page')
    } else {
      nextParams.set('page', page.toString())
    }
    setSearchParams(nextParams)
    document.querySelector('.news-list-section')?.scrollIntoView({ behavior: 'smooth' })
  }

  return (
    <article className="news-page">
      <section
        className="news-page-hero page-hero"
        style={{ '--page-hero-image': `url("${resolveMediaUrl(pageCopy?.headerImageUrl) || newsHeader}")` }}
        aria-labelledby="news-page-title"
      >
        <img className="page-hero-smoke" src={smoke} alt="" aria-hidden="true" />
        <div className="page-hero-content">
          <h1 id="news-page-title">{pageCopy?.title || t('newsPage.heroTitle')}</h1>
          <div className="page-hero-rule" aria-hidden="true">
            <span />
            <img src={theatreIcon} alt="" aria-hidden="true" />
            <span />
          </div>
          <p>{pageCopy?.subtitle || t('newsPage.heroSubtitle')}</p>
          <label className="news-search">
            <span className="sr-only">{t('newsPage.searchLabel')}</span>
            <input
              type="search"
              value={query}
              onChange={updateQuery}
              placeholder={t('newsPage.searchPlaceholder')}
            />
            <svg viewBox="0 0 24 24" aria-hidden="true">
              <circle cx="11" cy="11" r="6.5" />
              <path d="m16 16 4 4" />
            </svg>
          </label>
        </div>
      </section>

      <section className="news-list-section" aria-labelledby="news-list-title">
        <h2 id="news-list-title" className="sr-only">{t('newsPage.latestTitle')}</h2>

        {status === 'loading' && <LoadingState />}
        {status === 'error' && <ErrorState message={t('newsPage.loadError')} />}
        {status === 'success' && (
          <>
            <div className="news-card-grid">
              {visibleArticles.map((article, index) => {
                const content = (
                  <>
                    <div className="news-card-image">
                      {(article.cardThumbnailUrl || article.coverUrl) ? (
                        article.cardThumbnailUrl ? (
                          <img className="news-card-image-cover news-card-custom-thumbnail" src={resolveMediaUrl(article.cardThumbnailUrl)} alt="" loading="lazy" />
                        ) : article.coverMimeType?.startsWith('video/') ? (
                          <><VideoThumbnail src={resolveMediaUrl(article.coverUrl)} /><span className="news-card-play" aria-hidden="true">▶</span></>
                        ) : (
                          <>
                            <img
                              className="news-card-image-backdrop"
                              src={resolveMediaUrl(article.coverUrl)}
                              alt=""
                              loading="lazy"
                              aria-hidden="true"
                            />
                            <img
                              className="news-card-image-cover"
                              src={resolveMediaUrl(article.coverUrl)}
                              alt=""
                              loading="lazy"
                            />
                          </>
                        )
                      ) : (
                        <span className="news-card-placeholder" aria-hidden="true">AAB</span>
                      )}
                      {article.isExternal && <span className="news-external-badge">{t('newsPage.external')}</span>}
                    </div>
                    <div className="news-card-copy">
                      <time dateTime={article.publishedAt}>
                        {new Intl.DateTimeFormat(locale, {
                          day: '2-digit',
                          month: 'long',
                          year: 'numeric',
                        }).format(new Date(article.publishedAt))}
                      </time>
                      <h3>{article.title}</h3>
                      <p>{article.summary}</p>
                      <span className="news-card-more">
                        {article.isExternal ? t('newsPage.openSource') : t('newsPage.readMore')}
                        <ArrowRightIcon />
                      </span>
                    </div>
                  </>
                )

                return (
                  <article className={`news-card${index === 0 ? ' featured' : ''}`} key={article.id}>
                    {article.isExternal ? (
                      <a href={article.externalUrl} target="_blank" rel="noreferrer">{content}</a>
                    ) : (
                      <Link
                        to={`${getLocalizedPath('news', language)}/${article.slug}`}
                        state={{ newsListPath: `${location.pathname}${location.search}` }}
                      >
                        {content}
                      </Link>
                    )}
                  </article>
                )
              })}
            </div>
            {filteredArticles.length === 0 && (
              <p className="news-no-results">{t('newsPage.noResults')}</p>
            )}
            {pageCount > 1 && (
              <nav className="news-pagination" aria-label={t('newsPage.paginationLabel')}>
                <button
                  type="button"
                  className="news-pagination-arrow"
                  disabled={safeCurrentPage === 1}
                  onClick={() => goToPage(safeCurrentPage - 1)}
                  aria-label={t('newsPage.previousPage')}
                >
                  <ArrowRightIcon className="direction-arrow direction-arrow-left" />
                </button>
                {paginationItems.map((item) => (
                  typeof item === 'number' ? (
                    <button
                      type="button"
                      className={item === safeCurrentPage ? 'active' : ''}
                      aria-current={item === safeCurrentPage ? 'page' : undefined}
                      aria-label={t('newsPage.goToPage', { page: item })}
                      onClick={() => goToPage(item)}
                      key={item}
                    >
                      {item}
                    </button>
                  ) : (
                    <span className="news-pagination-ellipsis" aria-hidden="true" key={item}>…</span>
                  )
                ))}
                <button
                  type="button"
                  className="news-pagination-arrow"
                  disabled={safeCurrentPage === pageCount}
                  onClick={() => goToPage(safeCurrentPage + 1)}
                  aria-label={t('newsPage.nextPage')}
                >
                  <ArrowRightIcon className="direction-arrow" />
                </button>
              </nav>
            )}
          </>
        )}
      </section>
      {/* {home && <ReservationBanner home={home} />} */}
    </article>
  )
}
