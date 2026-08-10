import { useEffect, useRef, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAdminAuth } from '../AuthContext'
import logo from '../../assets/Teatri Logo/Teatri White.png'
import { AdminDialogProvider } from './AdminDialog'
import { useAdminLanguage } from '../AdminLanguageContext'

const groups = [
  { label: 'Overview', items: [['Dashboard', '/admin', '⌂'], ['Reservations', '/admin/reservations', '▣'], ['Seating templates', '/admin/seating-templates', '◉'], ['Customers', '/admin/customers', '♙']] },
  { label: 'Content', items: [['Homepage', '/admin/homepage', '◇'], ['Shows / Plays', '/admin/shows', '◈'], ['Performances', '/admin/performances', '▦'], ['News', '/admin/news', '▤'], ['PITF', '/admin/pitf', '◉'], ['Gallery', '/admin/gallery', '▧'], ['Pages', '/admin/pages', '□']] },
  { label: 'Communication', items: [['Contact Messages', '/admin/messages', '✉'], ['Subscribers', '/admin/subscribers', '♧', true]] },
  { label: 'Website', items: [['Website Information', '/admin/website-information', '⚙'], ['Navigation & Footer', '/admin/navigation', '☷', true], ['Translations', '/admin/translations', '文'], ['SEO & Links', '/admin/seo', '↗'], ['Media Library', '/admin/media', '▣']] },
  { label: 'Administration', restricted: true, items: [['Users & Roles', '/admin/users', '♙'], ['Activity Log', '/admin/activity', '◷'], ['Backups & System', '/admin/backups', '▰'], ['Settings', '/admin/settings', '⚙']] },
]

const navigationGroups = [
  { label: 'Overview', items: [['Dashboard', '/admin', '\u2302']] },
  { label: 'Theatre operations', items: [['Reservations', '/admin/reservations', '\u25a3'], ['Our plays', '/admin/shows?guest=false', '\u25c8'], ['Guest plays', '/admin/shows?guest=true', '\u25c7'], ['Performances', '/admin/performances', '\u25a6'], ['Seating templates', '/admin/seating-templates', '\u25c9'], ['Customers', '/admin/customers', '\u2659']] },
  { label: 'Website content', items: [['Homepage', '/admin/homepage', '\u25c7'], ['News', '/admin/news', '\u25a4'], ['PITF', '/admin/pitf', '\u25c9'], ['Gallery', '/admin/gallery', '\u25a7'], ['Pages', '/admin/pages', '\u25a1']] },
  groups[2],
  groups[3],
  groups[4],
]

export function AdminLayout() {
  const [drawer, setDrawer] = useState(false)
  const [profileOpen, setProfileOpen] = useState(false)
  const profileRef = useRef(null)
  const { user, logout } = useAdminAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const { language, setLanguage, t } = useAdminLanguage()
  const title = navigationGroups.flatMap(g => g.items).find(([, path]) => {
    const [pathname, query] = path.split('?')
    if (pathname !== location.pathname) return false
    if (!query) return true
    const [key, value] = query.split('=')
    return new URLSearchParams(location.search).get(key) === value
  })?.[0] ?? 'Dashboard'
  const visibleGroups = navigationGroups.filter(g => !g.restricted || user?.role === 'SuperAdmin').map(group => ({ ...group, items: group.items.filter(([, , , superOnly]) => !superOnly || user?.role === 'SuperAdmin') }))
  const signOut = async () => { await logout(); navigate('/admin/login') }
  useEffect(() => {
    let robots = document.head.querySelector('meta[name="robots"]')
    if (!robots) { robots = document.createElement('meta'); robots.name = 'robots'; document.head.appendChild(robots) }
    robots.content = 'noindex, nofollow'
  }, [])

  useEffect(() => {
    const closeOnOutsideClick = (event) => {
      if (!profileRef.current?.contains(event.target)) setProfileOpen(false)
    }
    const closeOnEscape = (event) => {
      if (event.key === 'Escape') setProfileOpen(false)
    }
    document.addEventListener('pointerdown', closeOnOutsideClick)
    document.addEventListener('keydown', closeOnEscape)
    return () => {
      document.removeEventListener('pointerdown', closeOnOutsideClick)
      document.removeEventListener('keydown', closeOnEscape)
    }
  }, [])

  return <div className="admin-shell">
    <aside className={`admin-sidebar ${drawer ? 'open' : ''}`}>
      <div className="admin-brand"><img src={logo} alt="" /><div><strong>TEATRI AAB</strong><span>ADMIN PANEL</span></div><button className="admin-drawer-close" onClick={() => setDrawer(false)}>×</button></div>
      <nav>{visibleGroups.map(group => <section key={group.label}><h2>{t(group.label)}</h2>{group.items.map(([label, path, icon]) => <NavLink end={path === '/admin'} className={({ isActive }) => { const query = path.split('?')[1]; if (!isActive || !query) return isActive ? 'active' : ''; const [key, value] = query.split('='); return new URLSearchParams(location.search).get(key) === value ? 'active' : '' }} to={path} onClick={() => setDrawer(false)} key={path}><i>{icon}</i>{t(label)}</NavLink>)}</section>)}</nav>
      {/* <blockquote>“Teatri është pasqyra e shoqërisë.”<cite>— Anton Zako Çajupi</cite></blockquote> */}
    </aside>
    {drawer && <button className="admin-scrim" aria-label="Close menu" onClick={() => setDrawer(false)} />}
    <div className="admin-workspace">
      <header className="admin-topbar">
        <button className="admin-menu" onClick={() => setDrawer(true)}>☰</button>
        <label className="admin-search">⌕<input placeholder={t('Search the theatre archive…')} /></label>
        <div className={`admin-language-switch language-${language}`} role="group" aria-label={t('Admin panel language')}>
          <span className="admin-language-slider" aria-hidden="true" />
          <button type="button" aria-pressed={language === 'en'} className={language === 'en' ? 'active' : ''} onClick={() => setLanguage('en')}>EN</button>
          <button type="button" aria-pressed={language === 'sq'} className={language === 'sq' ? 'active' : ''} onClick={() => setLanguage('sq')}>SQ</button>
        </div>
        <a className="admin-outline-button" href="#/sq" target="_blank">{t('Open website')} →</a>
        {/* <button className="admin-primary-button">Create content</button> */}
        <div className="admin-profile-menu" ref={profileRef}>
          <button className="admin-profile" type="button" aria-haspopup="menu" aria-expanded={profileOpen} onClick={() => setProfileOpen(open => !open)}>
            <span>{user?.displayName?.slice(0, 2).toUpperCase()}</span>
            <div><strong>{user?.displayName}</strong><small>{t(user?.role === 'SuperAdmin' ? 'Super Admin' : 'Content Editor')}</small></div>
            <svg className={profileOpen ? 'rotated' : ''} viewBox="0 0 20 20" aria-hidden="true"><path d="m5 7.5 5 5 5-5" /></svg>
          </button>
          {profileOpen && <div className="admin-profile-dropdown" role="menu">
            <button role="menuitem" onClick={() => { setProfileOpen(false); navigate('/admin/account') }}>
              <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M15.5 7.5 19 4m-2-2 3 3M13 10l-8.5 8.5L4 21l2.5-.5L15 12m-2-2 2 2" /></svg>
              <span><strong>{t('Change password')}</strong><small>{t('Update your account security')}</small></span>
            </button>
            <button className="danger" role="menuitem" onClick={signOut}>
              <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M10 5H5v14h5M14 8l4 4-4 4m4-4H9" /></svg>
              <span><strong>{t('Log out')}</strong><small>{t('End this admin session')}</small></span>
            </button>
          </div>}
        </div>
      </header>
      <div className="admin-breadcrumb">Admin <span>/</span> {t(title)}</div>
      <main className="admin-main"><AdminDialogProvider><Outlet /></AdminDialogProvider></main>
    </div>
  </div>
}
