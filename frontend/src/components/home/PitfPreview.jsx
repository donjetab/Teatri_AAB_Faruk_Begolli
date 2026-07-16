import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import curtain from '../../assets/curtain.png'
import goldLines from '../../assets/decorative-gold-lines.png'
import pitfHomepageImage from '../../assets/hp_pitf.jpg'
import pitfWordImage from '../../assets/PITF-fading.png'
import { getLocalizedPath } from '../../routes/localizedRoutes'

export function PitfPreview({ pitf, language }) {
  const { t } = useTranslation()
  if (!pitf) {
    return null
  }

  return (
    <section className="pitf-section" aria-labelledby="pitf-title">
      <img className="pitf-curtain" src={curtain} alt="" loading="lazy" aria-hidden="true" />
      <div className="pitf-inner">
        <div className="pitf-image-wrap">
          <img className="pitf-ghost" src={pitfWordImage} alt="" loading="lazy" aria-hidden="true" />
          <img
            className="pitf-main-image"
            src={pitfHomepageImage}
            alt={pitf.image?.altText ?? pitf.title}
            loading="lazy"
          />
          <img className="pitf-gold-lines" src={goldLines} alt="" loading="lazy" aria-hidden="true" />
        </div>

        <div className="pitf-copy">
          <h2 id="pitf-title">
            <span>Prishtina International</span>
            <span>Theatre Festival</span>
          </h2>
          <p>{pitf.shortDescription}</p>
          <Link to={getLocalizedPath('pitf', language)} className="home-button">
            <span>{t('home.pitfProgram')}</span>
            <span className="circle-arrow" aria-hidden="true">→</span>
          </Link>
        </div>
      </div>
    </section>
  )
}
