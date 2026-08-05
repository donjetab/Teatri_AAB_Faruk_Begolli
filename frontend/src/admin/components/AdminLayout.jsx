import { useEffect, useRef, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAdminAuth } from '../AuthContext'
import logo from '../../assets/theatre-icon.png'
import { AdminDialogProvider } from './AdminDialog'

const groups = [
  { label: 'Overview', items: [['Dashboard', '/admin', '⌂'], ['Reservations', '/admin/reservations', '▣']] },
  { label: 'Content', items: [['Homepage', '/admin/homepage', '◇'], ['Shows / Plays', '/admin/shows', '◈'], ['Performances', '/admin/performances', '▦'], ['News', '/admin/news', '▤'], ['PITF', '/admin/pitf', '◉'], ['Gallery', '/admin/gallery', '▧'], ['Pages', '/admin/pages', '□']] },
  { label: 'Communication', items: [['Contact Messages', '/admin/messages', '✉'], ['Subscribers', '/admin/subscribers', '♧', true]] },
  { label: 'Website', items: [['Website Information', '/admin/website-information', '⚙'], ['Navigation & Footer', '/admin/navigation', '☷', true], ['Translations', '/admin/translations', '文'], ['SEO & Links', '/admin/seo', '↗'], ['Media Library', '/admin/media', '▣']] },
  { label: 'Administration', restricted: true, items: [['Users & Roles', '/admin/users', '♙'], ['Activity Log', '/admin/activity', '◷'], ['Backups & System', '/admin/backups', '▰'], ['Settings', '/admin/settings', '⚙']] },
]

export function AdminLayout() {
  const [drawer, setDrawer] = useState(false)
  const [profileOpen, setProfileOpen] = useState(false)
  const profileRef = useRef(null)
  const { user, logout } = useAdminAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const title = groups.flatMap(g => g.items).find(([, path]) => path === location.pathname)?.[0] ?? 'Dashboard'
  const visibleGroups = groups.filter(g => !g.restricted || user?.role === 'SuperAdmin').map(group => ({ ...group, items: group.items.filter(([, , , superOnly]) => !superOnly || user?.role === 'SuperAdmin') }))
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
      <nav>{visibleGroups.map(group => <section key={group.label}><h2>{group.label}</h2>{group.items.map(([label, path, icon]) => <NavLink end={path === '/admin'} to={path} onClick={() => setDrawer(false)} key={path}><i>{icon}</i>{label}</NavLink>)}</section>)}</nav>
      {/* <blockquote>“Teatri është pasqyra e shoqërisë.”<cite>— Anton Zako Çajupi</cite></blockquote> */}
    </aside>
    {drawer && <button className="admin-scrim" aria-label="Close menu" onClick={() => setDrawer(false)} />}
    <div className="admin-workspace">
      <header className="admin-topbar">
        <button className="admin-menu" onClick={() => setDrawer(true)}>☰</button>
        <label className="admin-search">⌕<input placeholder="Search the theatre archive…" /></label>
        <a className="admin-outline-button" href="#/sq" target="_blank">Open website →</a>
        {/* <button className="admin-primary-button">Create content</button> */}
        <div className="admin-profile-menu" ref={profileRef}>
          <button className="admin-profile" type="button" aria-haspopup="menu" aria-expanded={profileOpen} onClick={() => setProfileOpen(open => !open)}>
            <span>{user?.displayName?.slice(0, 2).toUpperCase()}</span>
            <div><strong>{user?.displayName}</strong><small>{user?.role === 'SuperAdmin' ? 'Super Admin' : 'Content Editor'}</small></div>
            <svg className={profileOpen ? 'rotated' : ''} viewBox="0 0 20 20" aria-hidden="true"><path d="m5 7.5 5 5 5-5" /></svg>
          </button>
          {profileOpen && <div className="admin-profile-dropdown" role="menu">
            <button role="menuitem" onClick={() => { setProfileOpen(false); navigate('/admin/account') }}>
              <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M15.5 7.5 19 4m-2-2 3 3M13 10l-8.5 8.5L4 21l2.5-.5L15 12m-2-2 2 2" /></svg>
              <span><strong>Change password</strong><small>Update your account security</small></span>
            </button>
            <button className="danger" role="menuitem" onClick={signOut}>
              <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M10 5H5v14h5M14 8l4 4-4 4m4-4H9" /></svg>
              <span><strong>Log out</strong><small>End this admin session</small></span>
            </button>
          </div>}
        </div>
      </header>
      <div className="admin-breadcrumb">Admin <span>/</span> {title}</div>
      <main className="admin-main"><AdminDialogProvider><Outlet /></AdminDialogProvider></main>
    </div>
  </div>
}
