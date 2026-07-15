import { useTranslation } from 'react-i18next'
import { resolveMediaUrl } from '../../api/client'
import { TheatreStatistics } from './TheatreStatistics'

export function AboutPreview({ home }) {
  const { t } = useTranslation()
  const aboutImage = resolveMediaUrl(home.aboutImage?.url)

  return (
    <section className="about-section" aria-labelledby="about-title">
      <div className="about-section-inner">
        <div className="about-visual" aria-label={home.aboutImage?.altText ?? t('home.aboutImage')}>
          {aboutImage && <img src={aboutImage} alt="" loading="lazy" onError={(event) => { event.currentTarget.hidden = true }} />}
          <div className="about-visual-overlay" />
          <p className="legacy-lockup" aria-hidden="true">
            <span>A Legacy.</span>
            <span>A Vision.</span>
            <span>A Theatre.</span>
            <i />
          </p>
        </div>

        <div className="about-copy">
          <h2 id="about-title">{t('home.aboutTitle')}</h2>
          <p>{home.aboutPreview}</p>
          <TheatreStatistics statistics={home.statistics} />
        </div>
      </div>
    </section>
  )
}
