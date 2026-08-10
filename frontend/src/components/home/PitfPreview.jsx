import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import curtain from '../../assets/curtain.png'
import goldLines from '../../assets/decorative-gold-lines.png'
import pitfHomepageImage from '../../assets/pitf-pic.jpg'
import pitfWordImage from '../../assets/PITF-fading.png'
import { ArrowRightIcon } from '../icons/ArrowRightIcon'
import { resolveMediaUrl } from '../../api/client'
import { defaultLanguage, getManagedDestination, languages } from '../../routes/localizedRoutes'

export function PitfPreview({ pitf, title, description, buttonText, destinationUrl, image }) {
  const { t } = useTranslation()
  const { language: languageParam } = useParams()
  const language = languages.includes(languageParam) ? languageParam : defaultLanguage
  const buttonTarget = getManagedDestination(destinationUrl, language, 'pitf')
  const featureImage = resolveMediaUrl(image?.url) || pitfHomepageImage
  const heading = title || pitf?.title || ''
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
            src={featureImage}
            alt={pitf.image?.altText ?? pitf.title}
            loading="lazy"
          />
          <img className="pitf-gold-lines" src={goldLines} alt="" loading="lazy" aria-hidden="true" />
        </div>

        <div className="pitf-copy">
          <h2 id="pitf-title">
            {heading.split(/\s+/).filter(Boolean).map((word, index) => <span className="pitf-title-word" key={`${word}-${index}`}><i>{word[0]}</i>{word.slice(1)}</span>)}
          </h2>
          <p>{description || pitf.shortDescription}</p>
          {buttonTarget.startsWith('http') ? <a href={buttonTarget} target="_blank" rel="noreferrer" className="home-button">
            <span>{buttonText || t('home.pitfProgram')}</span>
            <span className="circle-arrow" aria-hidden="true">
              <ArrowRightIcon className="arrow-icon" />
            </span>
          </a> : <Link to={buttonTarget} className="home-button"><span>{buttonText || t('home.pitfProgram')}</span><span className="circle-arrow" aria-hidden="true"><ArrowRightIcon className="arrow-icon" /></span></Link>}
        </div>
      </div>
    </section>
  )
}
