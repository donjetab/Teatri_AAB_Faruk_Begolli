import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import logoWhite from '../../assets/Teatri Logo/Teatri White.png'
import { getLocalizedPath } from '../../routes/localizedRoutes'
import { ArrowRightIcon } from '../icons/ArrowRightIcon'
import { DesktopNavigation } from './DesktopNavigation'
import { LanguageSwitcher } from './LanguageSwitcher'
import { MobileNavigation } from './MobileNavigation'

export function Header({ language, isScrolled = false, navigation }) {
  const { t } = useTranslation()

  return (
    <header className={`site-header${isScrolled ? ' scrolled' : ''}`}>
      <a href="#content" className="skip-link">
        {t('a11y.skipToContent')}
      </a>
      <div className="site-header-inner">
        <Link to={getLocalizedPath('home', language)} className="brand-link" aria-label={t('brand')}>
          <img src={logoWhite} alt={t('brand')} className="brand-logo" />
        </Link>
        <div className="header-actions">
          <DesktopNavigation language={language} navigation={navigation} />
          <Link to={getLocalizedPath('reserve', language)} className="reserve-button">
            <span>{navigation?.translations?.find(item => item.languageCode === language)?.reserveLabel || t('nav.reserveNow')}</span>
            <span className="circle-arrow" aria-hidden="true">
              <ArrowRightIcon className="arrow-icon" />
            </span>
          </Link>
          <LanguageSwitcher language={language} />
          <MobileNavigation language={language} navigation={navigation} />
        </div>
      </div>
    </header>
  )
}
