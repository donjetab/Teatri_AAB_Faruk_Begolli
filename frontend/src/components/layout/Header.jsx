import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import logoWhite from '../../assets/Teatri Logo/Teatri White.png'
import { getLocalizedPath } from '../../routes/localizedRoutes'
import { DesktopNavigation } from './DesktopNavigation'
import { LanguageSwitcher } from './LanguageSwitcher'
import { MobileNavigation } from './MobileNavigation'

export function Header({ language, reservationUrl }) {
  const { t } = useTranslation()

  return (
    <header className="site-header">
      <a href="#content" className="skip-link">
        {t('a11y.skipToContent')}
      </a>
      <div className="site-header-inner">
        <Link to={getLocalizedPath('home', language)} className="brand-link" aria-label={t('brand')}>
          <img src={logoWhite} alt={t('brand')} className="brand-logo" />
        </Link>
        <div className="header-actions">
          <DesktopNavigation language={language} />
          <LanguageSwitcher language={language} />
          <a href={reservationUrl ?? getLocalizedPath('shows', language)} className="reserve-button">
            <span>{t('nav.reserveNow')}</span>
            <span className="circle-arrow" aria-hidden="true">→</span>
          </a>
          <MobileNavigation language={language} />
        </div>
      </div>
    </header>
  )
}
