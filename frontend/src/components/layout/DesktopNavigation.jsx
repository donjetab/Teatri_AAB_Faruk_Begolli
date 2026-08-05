import { NavLink } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { getLocalizedPath } from '../../routes/localizedRoutes'

const navItems = ['home', 'about', 'shows', 'news', 'pitf', 'gallery', 'contact']

export function DesktopNavigation({ language, navigation }) {
  const { t } = useTranslation()
  const labels = navigation?.translations?.find(item => item.languageCode === language)?.labels
  const items = navigation?.items?.filter(item => item.showInHeader).sort((a, b) => a.sortOrder - b.sortOrder).map(item => item.routeKey) ?? navItems

  return (
    <nav className="desktop-nav" aria-label={t('a11y.primaryNavigation')}>
      {items.map((item) => (
        <NavLink
          key={item}
          to={getLocalizedPath(item, language)}
          end={item === 'home'}
          className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}
        >
          {labels?.[item] || t(`nav.${item}`)}
        </NavLink>
      ))}
    </nav>
  )
}
