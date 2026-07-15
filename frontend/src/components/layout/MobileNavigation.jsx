import { useEffect, useState } from 'react'
import { NavLink } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { getLocalizedPath } from '../../routes/localizedRoutes'

const navItems = ['home', 'about', 'shows', 'news', 'pitf', 'gallery', 'contact']

export function MobileNavigation({ language }) {
  const { t } = useTranslation()
  const [isOpen, setIsOpen] = useState(false)

  useEffect(() => {
    function closeOnEscape(event) {
      if (event.key === 'Escape') setIsOpen(false)
    }

    document.addEventListener('keydown', closeOnEscape)
    return () => document.removeEventListener('keydown', closeOnEscape)
  }, [])

  return (
    <div className="mobile-nav">
      <button
        type="button"
        className="mobile-menu-button"
        aria-expanded={isOpen}
        aria-controls="mobile-navigation-panel"
        onClick={() => setIsOpen((value) => !value)}
      >
        <span className="menu-line" />
        <span className="menu-line" />
        <span className="menu-line" />
        <span className="sr-only">{t('a11y.menu')}</span>
      </button>
      <div id="mobile-navigation-panel" className={isOpen ? 'mobile-panel open' : 'mobile-panel'}>
        <nav aria-label={t('a11y.mobileNavigation')}>
          {navItems.map((item) => (
            <NavLink
              key={item}
              to={getLocalizedPath(item, language)}
              end={item === 'home'}
              className={({ isActive }) => (isActive ? 'mobile-nav-link active' : 'mobile-nav-link')}
              onClick={() => setIsOpen(false)}
            >
              {t(`nav.${item}`)}
            </NavLink>
          ))}
        </nav>
      </div>
    </div>
  )
}
