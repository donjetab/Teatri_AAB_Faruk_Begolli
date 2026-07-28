import { useEffect, useState } from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { getNewsArticle } from '../api/news'
import { resolveMediaUrl } from '../api/client'
import { getLocalizedPath } from '../routes/localizedRoutes'
import { LoadingState } from '../components/ui/LoadingState'
import { ErrorState } from '../components/ui/ErrorState'

export function NewsDetailPage() {
  const { t, i18n } = useTranslation()
  const { language = 'sq', slug } = useParams()
  const location = useLocation()
  const [article, setArticle] = useState(null)
  const [status, setStatus] = useState('loading')

  useEffect(() => {
    const controller = new AbortController()
    getNewsArticle(language, slug, controller.signal)
      .then((data) => {
        setArticle(data)
        setStatus('success')
      })
      .catch((error) => {
        if (error.name !== 'CanceledError' && error.code !== 'ERR_CANCELED') {
          setStatus('error')
        }
      })
    return () => controller.abort()
  }, [language, slug])

  if (status === 'loading') {
    return <section className="news-detail-state"><LoadingState /></section>
  }
  if (status === 'error' || !article) {
    return <section className="news-detail-state"><ErrorState message={t('newsPage.articleLoadError')} /></section>
  }

  const locale = i18n.language === 'sq' ? 'sq-AL' : 'en-GB'
  const bodyParagraphs = article.content.split(/\r?\n+/).filter(Boolean)
  const media = article.media.filter((item) => !item.isCover)
  const videos = media.filter((item) => item.mimeType.startsWith('video/'))
  const galleryImages = media.filter((item) => item.mimeType.startsWith('image/'))
  const newsListPath = location.state?.newsListPath ?? getLocalizedPath('news', language)

  return (
    <article className="news-detail-page">
      <header className="news-detail-header">
        <Link to={newsListPath}>
          {t('newsPage.backToNews')}
        </Link>
        <h1>{article.title}</h1>
        <time dateTime={article.publishedAt}>
          {new Intl.DateTimeFormat(locale, {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
          }).format(new Date(article.publishedAt))}
        </time>
      </header>

      {article.coverUrl && (
        <figure className="news-detail-cover">
          <img
            className="news-detail-cover-backdrop"
            src={resolveMediaUrl(article.coverUrl)}
            alt=""
            aria-hidden="true"
          />
          <img
            className="news-detail-cover-image"
            src={resolveMediaUrl(article.coverUrl)}
            alt={article.title}
          />
        </figure>
      )}

      <div className="news-detail-body">
        {bodyParagraphs.map((paragraph, index) => <p key={`${index}-${paragraph.slice(0, 20)}`}>{paragraph}</p>)}
      </div>

      {videos.length > 0 && (
        <section className="news-detail-videos" aria-label={t('newsPage.articleVideos')}>
          {videos.map((item) => (
            <video key={item.id} controls preload="metadata">
              <source src={resolveMediaUrl(item.url)} type={item.mimeType} />
              {t('showDetail.videoUnsupported')}
            </video>
          ))}
        </section>
      )}

      {galleryImages.length > 0 && (
        <section className="news-detail-gallery" aria-label={t('newsPage.articleGallery')}>
          {galleryImages.map((item) => (
            <figure key={item.id}>
              <img src={resolveMediaUrl(item.url)} alt={item.altText} loading="lazy" />
              {item.caption && <figcaption>{item.caption}</figcaption>}
            </figure>
          ))}
        </section>
      )}

      <footer className="news-detail-ending">
        <Link to={newsListPath}>
          <span aria-hidden="true">←</span>
          {t('newsPage.backToNews')}
        </Link>
      </footer>
    </article>
  )
}
