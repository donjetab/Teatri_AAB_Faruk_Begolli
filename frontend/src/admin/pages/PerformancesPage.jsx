import { useEffect, useState } from 'react'
import { adminApi } from '../api'
import { LoadingSkeleton, PageHeader, StatusBadge, Toast } from '../components/AdminUi'

const empty = { showId: '', locationId: '', hall: '', startDateTimeUtc: '', endDateTimeUtc: '', ticketUrl: '', contactPhone: '', status: 'Scheduled', isPublished: false, internalNotes: '' }
const localValue = value => value ? new Date(value).toISOString().slice(0, 16) : ''
const payload = form => ({ ...form, showId: Number(form.showId), locationId: form.locationId ? Number(form.locationId) : null, startDateTimeUtc: new Date(form.startDateTimeUtc).toISOString(), endDateTimeUtc: form.endDateTimeUtc ? new Date(form.endDateTimeUtc).toISOString() : null })

export function PerformancesPage() {
  const [data, setData] = useState(null); const [filters, setFilters] = useState({ showId: '', locationId: '', status: '' })
  const [view, setView] = useState('table'); const [editing, setEditing] = useState(null); const [form, setForm] = useState(empty); const [toast, setToast] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const load = () => {
    setLoading(true)
    setError('')
    return adminApi.performances(Object.fromEntries(Object.entries(filters).filter(([, v]) => v)))
      .then(setData)
      .catch(response => {
        setData(null)
        const detail = response.response?.data?.detail
        setError(detail || 'Performance data could not be loaded. Restart the backend so its latest migration and API are active.')
      })
      .finally(() => setLoading(false))
  }
  useEffect(() => {
    void load()
  }, [filters.showId, filters.locationId, filters.status])
  const openNew = () => { setEditing('new'); setForm(empty) }
  const openEdit = item => { setEditing(item.id); setForm({ ...item, locationId: item.locationId ?? '', startDateTimeUtc: localValue(item.startDateTimeUtc), endDateTimeUtc: localValue(item.endDateTimeUtc), hall: item.hall ?? '', ticketUrl: item.ticketUrl ?? '', contactPhone: item.contactPhone ?? '', internalNotes: item.internalNotes ?? '' }) }
  const save = async e => { e.preventDefault(); if (editing === 'new') await adminApi.createPerformance(payload(form)); else await adminApi.savePerformance(editing, payload(form)); setEditing(null); setToast('Performance saved.'); load() }
  const duplicate = async id => { await adminApi.duplicatePerformance(id); setToast('Performance duplicated one week later as a draft.'); load() }
  const remove = async item => { if (!window.confirm(`Delete the draft performance for “${item.showTitle}”?`)) return; await adminApi.deletePerformance(item.id); setToast('Draft performance deleted.'); load() }
  const days = (() => {
    const byDay = new Map()
    for (const item of data?.items ?? []) { const key = item.startDateTimeUtc.slice(0, 10); byDay.set(key, [...(byDay.get(key) ?? []), item]) }
    return [...byDay.entries()]
  })()
  return <><PageHeader eyebrow="Content" title="Performances" description="Schedule and publish performance dates without reservation or seat calculations." actions={<button className="admin-primary-button" onClick={openNew}>Add performance</button>} />
    <section className="admin-panel"><div className="performance-toolbar"><div className="show-filters"><label><span>Play</span><select value={filters.showId} onChange={e => setFilters({ ...filters, showId: e.target.value })}><option value="">All plays</option>{data?.shows?.map(x => <option value={x.id} key={x.id}>{x.label}</option>)}</select></label><label><span>Venue</span><select value={filters.locationId} onChange={e => setFilters({ ...filters, locationId: e.target.value })}><option value="">All venues</option>{data?.locations?.map(x => <option value={x.id} key={x.id}>{x.label}</option>)}</select></label><label><span>Status</span><select value={filters.status} onChange={e => setFilters({ ...filters, status: e.target.value })}><option value="">All statuses</option><option>Scheduled</option><option>SoldOut</option><option>Postponed</option><option>Cancelled</option><option>Completed</option></select></label></div><div className="view-switch"><button className={view === 'table' ? 'active' : ''} onClick={() => setView('table')}>Table</button><button className={view === 'calendar' ? 'active' : ''} onClick={() => setView('calendar')}>Calendar</button></div></div></section>
    {error ? <section className="admin-panel admin-request-error" role="alert"><div>!</div><h2>Performances are unavailable</h2><p>{error}</p><button className="admin-primary-button" onClick={load}>Try again</button></section> : loading ? <LoadingSkeleton /> : !data ? null : view === 'table' ? <section className="admin-panel"><div className="admin-table-wrap"><table className="admin-table"><thead><tr><th>Play</th><th>Date & time</th><th>Venue</th><th>Availability</th><th>Publication</th><th /></tr></thead><tbody>{data.items.map(item => <tr key={item.id}><td><strong>{item.showTitle}</strong></td><td>{new Date(item.startDateTimeUtc).toLocaleString()}</td><td>{[item.venue, item.hall].filter(Boolean).join(' · ') || '—'}</td><td><StatusBadge status={item.status} /></td><td><StatusBadge status={item.isPublished ? 'Published' : 'Draft'} /></td><td><div className="table-actions"><button onClick={() => openEdit(item)}>Edit</button><button onClick={() => duplicate(item.id)}>Duplicate</button>{!item.isPublished && <button onClick={() => remove(item)}>Delete</button>}</div></td></tr>)}</tbody></table></div></section>
      : <section className="performance-calendar">{days.map(([day, items]) => <article key={day}><header><strong>{new Date(`${day}T12:00:00`).toLocaleDateString(undefined, { weekday: 'short', day: 'numeric', month: 'short' })}</strong><span>{items.length} event{items.length === 1 ? '' : 's'}</span></header>{items.map(item => <button onClick={() => openEdit(item)} key={item.id}><time>{new Date(item.startDateTimeUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</time><span>{item.showTitle}<small>{item.venue ?? item.hall ?? 'Venue not set'}</small></span><StatusBadge status={item.status} /></button>)}</article>)}</section>}
    {editing && <div className="admin-modal-backdrop" onMouseDown={e => e.target === e.currentTarget && setEditing(null)}><section className="admin-modal" role="dialog" aria-modal="true"><div className="admin-modal-heading"><div><span>Performance</span><h2>{editing === 'new' ? 'Add performance' : 'Edit performance'}</h2></div><button onClick={() => setEditing(null)}>×</button></div><form className="admin-form" onSubmit={save}><div className="form-grid"><label>Play *<select required value={form.showId} onChange={e => setForm({ ...form, showId: e.target.value })}><option value="">Select play</option>{data.shows.map(x => <option value={x.id} key={x.id}>{x.label}</option>)}</select></label><label>Venue<select value={form.locationId} onChange={e => setForm({ ...form, locationId: e.target.value })}><option value="">No venue</option>{data.locations.map(x => <option value={x.id} key={x.id}>{x.label}</option>)}</select></label><label>Start *<input required type="datetime-local" value={form.startDateTimeUtc} onChange={e => setForm({ ...form, startDateTimeUtc: e.target.value })} /></label><label>End<input type="datetime-local" value={form.endDateTimeUtc} onChange={e => setForm({ ...form, endDateTimeUtc: e.target.value })} /></label><label>Hall<input value={form.hall} onChange={e => setForm({ ...form, hall: e.target.value })} /></label><label>Status<select value={form.status} onChange={e => setForm({ ...form, status: e.target.value })}><option>Scheduled</option><option>SoldOut</option><option>Postponed</option><option>Cancelled</option><option>Completed</option></select></label><label>Ticket URL<input type="url" value={form.ticketUrl} onChange={e => setForm({ ...form, ticketUrl: e.target.value })} /></label><label>Contact phone<input value={form.contactPhone} onChange={e => setForm({ ...form, contactPhone: e.target.value })} /></label><label className="full">Internal notes<textarea rows="4" value={form.internalNotes} onChange={e => setForm({ ...form, internalNotes: e.target.value })} /></label><label className="admin-switch-row full"><input type="checkbox" checked={form.isPublished} onChange={e => setForm({ ...form, isPublished: e.target.checked })} /> Published on the website</label></div><div className="admin-modal-actions"><button type="button" className="admin-text-button" onClick={() => setEditing(null)}>Cancel</button><button className="admin-primary-button">Save performance</button></div></form></section></div>}
    <Toast message={toast} onClose={() => setToast('')} /></>
}
