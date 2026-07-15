import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { resolveMediaUrl } from '../../api/client'
import { getLocalizedPath } from '../../routes/localizedRoutes'

function splitTheatreName(name) {
  const match = name.match(/^(.*?)\s*["“](.+?)["”]$/)
  return match ? [match[1].trim(), `“${match[2]}”`] : [name, '']
}

export function HeroSection({ home, language }) {
  const { t } = useTranslation()
  const [mainName, quotedName] = splitTheatreName(home.theatreName)
  const sloganLines = home.heroSlogan.split(/\r?\n|\. /).map((line) => line.trim()).filter(Boolean)
  const background = resolveMediaUrl(home.heroBackground?.url)

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
          <Link to={getLocalizedPath('shows', language)} className="home-button">
            <span>{t('home.viewProgram')}</span>
            <span className="circle-arrow" aria-hidden="true">→</span>
          </Link>
        </div>
      </div>
    </section>
  )
}
