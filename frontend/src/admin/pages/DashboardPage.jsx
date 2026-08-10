import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { adminApi } from '../api'
import { LoadingSkeleton, PageHeader, StatusBadge } from '../components/AdminUi'
import { useAdminLanguage } from '../AdminLanguageContext'

const metricIcons = { upcomingPerformances: '◷', unreadMessages: '✉', subscribers: '♧', bookingsToday: '▣', publishedPlays: '◇', draftPlays: '✎' }
const metricKeys = ['upcomingPerformances', 'unreadMessages', 'subscribers', 'bookingsToday', 'publishedPlays', 'draftPlays']
const quick = [['Create play', '/admin/shows/new'], ['Add performance', '/admin/performances'], ['Reservations', '/admin/reservations'], ['Read messages', '/admin/messages'], ['Gallery', '/admin/gallery'], ['Edit homepage', '/admin/homepage']]
const dateTime = (value, locale) => new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' }).format(new Date(value))

export function DashboardPage() {
  const { language, t } = useAdminLanguage()
  const locale = language === 'sq' ? 'sq-AL' : 'en-GB'
  const relative = value => { const days = Math.floor((Date.now() - new Date(value)) / 86400000); return days < 1 ? t('Today') : days === 1 ? t('Yesterday') : language === 'sq' ? `${days} ditë më parë` : `${days} days ago` }
  const [data, setData] = useState(null)
  const [error, setError] = useState('')
  useEffect(() => { adminApi.dashboard().then(setData).catch(() => setError('Dashboard data could not be loaded.')) }, [])
  const metrics = data ? metricKeys.map(key => data.metrics.find(item => item.key === key)).filter(Boolean) : []
  return <><PageHeader eyebrow="Overview" title="Good morning" description="What is happening at the theatre, at a glance." actions={<a className="admin-outline-button" href="#/sq" target="_blank" rel="noreferrer">{t('Open website')} ↗</a>} />
    {error && <div className="admin-form-error">{t(error)}</div>}{!data ? <LoadingSkeleton rows={8} /> : <>
      <section className="metric-grid dashboard-metrics">{metrics.map(m => <Link to={m.key === 'unreadMessages' ? '/admin/messages' : m.key === 'subscribers' ? '/admin/subscribers' : m.key === 'bookingsToday' ? '/admin/reservations' : m.key.includes('Play') || m.key.includes('play') ? '/admin/shows' : '/admin/performances'} className={`metric-card metric-${m.key}`} key={m.key}><i>{metricIcons[m.key]}</i><div><span>{t(m.label)}</span><strong>{m.value.toLocaleString(locale)}</strong></div><b>{t('View')} →</b></Link>)}</section>

      <section className="admin-panel dashboard-today-bookings"><header><div><span className="dashboard-kicker">{t('Today')}</span><h2>{t('New bookings today')}</h2><p>{t('Reservations received today and the performances they belong to.')}</p></div><strong>{data.todayBookings.length}</strong></header>
        {data.todayBookings.length ? <div className="dashboard-booking-list">{data.todayBookings.map(item => <Link to={`/admin/reservations?performanceId=${item.performanceId}`} key={item.id}><div className="dashboard-booking-time"><strong>{new Date(item.reservedAt).toLocaleTimeString(locale, { hour: '2-digit', minute: '2-digit' })}</strong><span>#{item.id}</span></div><div className="dashboard-booking-customer"><strong>{item.customerName}</strong><span>{item.phone} · {item.seatCount} {t(item.seatCount === 1 ? 'seat' : 'seats')}</span></div><div className="dashboard-booking-show"><span>{t('Play')}</span><strong>{item.showTitle}</strong><small>{dateTime(item.performanceStartsAt, locale)}</small></div><div className="dashboard-booking-status"><StatusBadge status={item.confirmationStatus} /><StatusBadge status={item.status} /></div><b>{t('View')} →</b></Link>)}</div> : <div className="dashboard-empty"><strong>{t('No new bookings today')}</strong><span>{t('New reservations will appear here together with their play and performance.')}</span></div>}
      </section>

      <section className="admin-panel dashboard-reservation-board"><header><div><span className="dashboard-kicker">{t('Live booking overview')}</span><h2>{t('Open reservations')}</h2><p>{t('Upcoming performances using the theatre seat map.')}</p></div><Link className="admin-outline-button" to="/admin/reservations">{t('Manage reservations')} →</Link></header>
        <div className="reservation-performance-list">{data.reservationPerformances.length ? data.reservationPerformances.map(item => { const percent = item.totalSeats ? Math.round(item.takenSeats / item.totalSeats * 100) : 0; return <Link to={`/admin/reservations?performanceId=${item.id}`} className="reservation-performance-row" key={item.id}><div className="performance-date"><strong>{new Date(item.startsAt).getDate()}</strong><span>{new Date(item.startsAt).toLocaleString(locale, { month: 'short' })}</span></div><div className="performance-main"><div><strong>{item.title}</strong><span>{dateTime(item.startsAt, locale)} · {item.venue || t('Theatre')}</span></div><div className="seat-progress"><span style={{ width: `${percent}%` }} /></div><small>{language === 'sq' ? `${item.takenSeats} nga ${item.totalSeats} ulëse të zëna` : `${item.takenSeats} of ${item.totalSeats} chairs taken`}</small></div><div className="performance-occupancy"><strong>{percent}%</strong><span>{item.reservationCount} {t('bookings')}</span><i className={item.isOpen ? 'open' : 'closed'}>{t(item.isOpen ? 'Open' : 'Closed')}</i></div></Link> }) : <div className="dashboard-empty"><strong>{t('No internal reservations are open')}</strong><span>{t('Publish an upcoming performance with a seating template to see occupancy here.')}</span><Link to="/admin/performances">{t('Set up a performance')} →</Link></div>}</div>
      </section>

      <div className="dashboard-activity-grid"><section className="admin-panel dashboard-feed"><header><div><span className="dashboard-kicker">{t('Inbox')}</span><h2>{t('Contact messages')}</h2></div><Link to="/admin/messages">{t('See all')} →</Link></header>{data.recentMessages.map(item => <Link to="/admin/messages" className={item.status === 'New' ? 'unread' : ''} key={item.id}><i>{item.title.slice(0, 1).toUpperCase()}</i><span><strong>{item.title}</strong><small>{item.subtitle} · {relative(item.date)}</small></span><StatusBadge status={item.status} /></Link>)}{!data.recentMessages.length && <p className="dashboard-empty-line">{t('No messages yet.')}</p>}</section>
      <section className="admin-panel dashboard-feed subscribers"><header><div><span className="dashboard-kicker">{t('Audience')}</span><h2>{t('Latest subscribers')}</h2></div><Link to="/admin/subscribers">{t('See all')} →</Link></header>{data.recentSubscribers.map(item => <Link to="/admin/subscribers" key={item.id}><i>♧</i><span><strong>{item.email}</strong><small>{t('Subscribed')} {relative(item.subscribedAt)}</small></span></Link>)}{!data.recentSubscribers.length && <p className="dashboard-empty-line">{t('No subscribers yet.')}</p>}</section>
      <section className="admin-panel dashboard-shortcuts"><span className="dashboard-kicker">{t('Shortcuts')}</span><h2>{t('Quick actions')}</h2><div className="quick-grid">{quick.map(([label, path]) => <Link key={label} to={path}>{t(label)}<span>→</span></Link>)}</div></section></div>
    </>}</>
}
