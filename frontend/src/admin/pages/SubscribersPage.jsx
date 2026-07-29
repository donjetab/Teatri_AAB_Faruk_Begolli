import { useEffect, useState } from 'react'
import { adminApi } from '../api'
import { useAdminDialog } from '../components/AdminDialog'
import { LoadingSkeleton, PageHeader, StatusBadge, Toast } from '../components/AdminUi'

export function SubscribersPage() {
  const dialog = useAdminDialog()
  const [filters, setFilters] = useState({ search: '', active: '' }); const [data, setData] = useState(null); const [toast, setToast] = useState('')
  const load = () => adminApi.subscribers(Object.fromEntries(Object.entries(filters).filter(([,v]) => v !== ''))).then(setData)
  useEffect(() => { const timer = setTimeout(() => { void load() }, 180); return () => clearTimeout(timer) }, [filters.search, filters.active])
  const exportCsv = async () => { const blob = await adminApi.exportSubscribers(); const url = URL.createObjectURL(blob); const link = document.createElement('a'); link.href = url; link.download = `theatre-subscribers-${new Date().toISOString().slice(0,10)}.csv`; link.click(); URL.revokeObjectURL(url); setToast('Subscriber export downloaded and logged.') }
  const remove = async item => { if (!await dialog.confirm({ title: 'Delete subscriber data?', message: `${item.email} will be permanently removed from the subscriber list.`, confirmLabel: 'Delete subscriber', danger: true })) return; await adminApi.deleteSubscriber(item.id); setToast('Subscriber data deleted.'); load() }
  return <><PageHeader eyebrow="Communication" title="Newsletter Subscribers" description="Manage consented newsletter contacts and privacy requests." actions={<button className="admin-outline-button" onClick={exportCsv}>Export CSV</button>} />
    <section className="admin-panel"><div className="communication-summary"><input placeholder="Search email address…" value={filters.search} onChange={e => setFilters({ ...filters, search: e.target.value })} /><select value={filters.active} onChange={e => setFilters({ ...filters, active: e.target.value })}><option value="">All subscribers</option><option value="true">Active</option><option value="false">Unsubscribed</option></select><span>{data?.totalCount ?? 0} records</span></div></section>
    {!data ? <LoadingSkeleton /> : <section className="admin-panel"><div className="admin-table-wrap"><table className="admin-table"><thead><tr><th>Email</th><th>Status</th><th>Language</th><th>Subscribed</th><th>Unsubscribed</th><th>Source</th><th /></tr></thead><tbody>{data.items.map(item => <tr key={item.id}><td>{item.email}</td><td><StatusBadge status={item.isActive ? 'Active' : 'Unsubscribed'} /></td><td>{item.preferredLanguageCode.toUpperCase()}</td><td>{new Date(item.subscribedAt).toLocaleDateString()}</td><td>{item.unsubscribedAt ? new Date(item.unsubscribedAt).toLocaleDateString() : '—'}</td><td>{item.source ?? 'Unknown'}</td><td><button className="admin-text-button" onClick={() => remove(item)}>Delete data</button></td></tr>)}</tbody></table></div></section>}<Toast message={toast} onClose={() => setToast('')} /></>
}
