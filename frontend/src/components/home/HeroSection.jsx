import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { resolveMediaUrl } from '../../api/client'
import { ArrowRightIcon } from '../icons/ArrowRightIcon'
import { getLocalizedPath, getManagedDestination } from '../../routes/localizedRoutes'

function splitTheatreName(name) {
  const match = name.match(/^(.*?)\s*["“](.+?)["”]$/)
  return match ? [match[1].trim(), `“${match[2]}”`] : [name, '']
}

export function HeroSection({ home, language }) {
  const { t } = useTranslation()
  const [mainName, quotedName] = splitTheatreName(home.theatreName)
  const sloganLines = home.heroSlogan.split(/\r?\n|\. /).map((line) => line.trim()).filter(Boolean)
  const background = resolveMediaUrl(home.heroBackground?.url)
  const buttonTarget = getManagedDestination(home.primaryButtonLink, language, 'shows')
  const buttonContent = <><span>{home.heroButtonText || t('home.viewProgram')}</span><span className="circle-arrow" aria-hidden="true"><ArrowRightIcon className="arrow-icon" /></span></>

  return (
    <section
      className="hero-section"
      style={background ? { '--hero-image': `url("${background}")` } : undefined}
      aria-labelledby="hero-title"
    >
      <div className="hero-section-inner">
        <div className="hero-copy">
          <h1 id="hero-title">
            <span>{mainName}</span>
            {quotedName && <strong>{quotedName}</strong>}
          </h1>
          <div className="hero-rule" aria-hidden="true" />
          <p>
            {sloganLines.map((line) => (
              <span key={line}>{line.endsWith('.') ? line : `${line}.`}</span>
            ))}
          </p>
          {home.heroSupportingText && <p className="hero-supporting-text">{home.heroSupportingText}</p>}
          {buttonTarget?.startsWith('http') ? <a href={buttonTarget} className="home-button" target="_blank" rel="noreferrer">{buttonContent}</a> : <Link to={buttonTarget || getLocalizedPath('shows', language)} className="home-button">{buttonContent}</Link>}
        </div>
      </div>
    </section>
  )
}
