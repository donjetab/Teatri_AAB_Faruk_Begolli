import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { adminApi } from '../api'
import { DataTable, LoadingSkeleton, PageHeader, StatusBadge } from '../components/AdminUi'

const metricIcons = { publishedPlays: '◈', draftPlays: '✎', archivedPlays: '□', upcomingPerformances: '▦', publishedNews: '▤', draftNews: '▧', unreadMessages: '✉', subscribers: '♧', missingSq: '文', missingEn: '文', brokenLinks: '↗', unusedMedia: '▱' }
const quick = [['Add New Play', '/admin/shows'], ['Add Performance', '/admin/performances'], ['Add News Article', '/admin/news'], ['Upload Media', '/admin/media'], ['Add PITF Edition', '/admin/pitf'], ['Edit Homepage', '/admin/homepage'], ['Review Messages', '/admin/messages'], ['Preview Website', '/sq']]
const itemColumns = [{ key: 'title', label: 'Title' }, { key: 'date', label: 'Date', render: r => r.date ? new Date(r.date).toLocaleDateString() : '—' }, { key: 'status', label: 'Status', render: r => <StatusBadge status={r.status} /> }]

export function DashboardPage() {
  const [data, setData] = useState(null); const [error, setError] = useState('')
  useEffect(() => { adminApi.dashboard().then(setData).catch(() => setError('Dashboard data could not be loaded.')) }, [])
  return <><PageHeader eyebrow="Overview" title="Dashboard" description="A live view of public website content and items needing attention." actions={<a className="admin-outline-button" href="#/sq" target="_blank">Preview Website ↗</a>} />
    {error && <div className="admin-form-error">{error}</div>}{!data ? <LoadingSkeleton rows={8} /> : <>
      <section className="metric-grid">{data.metrics.map(m => <article className="metric-card" key={m.key}><i>{metricIcons[m.key]}</i><div><span>{m.label}</span><strong>{m.value.toLocaleString()}</strong></div></article>)}</section>
      <div className="dashboard-grid"><section className="admin-panel wide"><h2>Upcoming performances</h2><DataTable columns={itemColumns} rows={data.upcomingPerformances} emptyText="No upcoming performances are scheduled." /></section>
      <section className="admin-panel"><h2>Quick actions</h2><div className="quick-grid">{quick.map(([label, path]) => <Link key={label} to={path}>{label}<span>→</span></Link>)}</div></section>
      <section className="admin-panel"><h2>Recently edited plays</h2><DataTable columns={itemColumns} rows={data.recentlyEditedPlays} /></section>
      <section className="admin-panel"><h2>Recent news</h2><DataTable columns={itemColumns} rows={data.recentNews} /></section>
      <section className="admin-panel"><h2>Recent contact messages</h2><DataTable columns={itemColumns} rows={data.recentMessages} /></section>
      <section className="admin-panel"><h2>Recent admin activity</h2><DataTable columns={itemColumns} rows={data.recentActivity} emptyText="Activity will appear after administrators begin making changes." /></section></div>
    </>}</>
}
