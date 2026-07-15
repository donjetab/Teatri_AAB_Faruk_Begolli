import { NavLink } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { getLocalizedPath } from '../../routes/localizedRoutes'

const navItems = ['home', 'about', 'shows', 'news', 'pitf', 'gallery', 'contact']

export function DesktopNavigation({ language }) {
  const { t } = useTranslation()

  return (
    <nav className="desktop-nav" aria-label={t('a11y.primaryNavigation')}>
      {navItems.map((item) => (
        <NavLink
          key={item}
          to={getLocalizedPath(item, language)}
          end={item === 'home'}
          className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}
        >
          {t(`nav.${item}`)}
        </NavLink>
      ))}
    </nav>
  )
}
