import { useEffect, useState } from 'react'
import { adminApi } from '../api'
import { EmptyState, LoadingSkeleton, PageHeader, StatusBadge, Toast } from '../components/AdminUi'
import { useAdminDialog } from '../components/AdminDialog'

const initialFilters = { search: '', showId: '', reservationFrom: '', reservationTo: '', sortBy: 'lastReservation', sortDirection: 'desc', page: 1, pageSize: 25 }
const saveBlob = (blob, name) => { const url = URL.createObjectURL(blob); const link = document.createElement('a'); link.href = url; link.download = name; link.click(); URL.revokeObjectURL(url) }
const formatDate = value => value ? new Intl.DateTimeFormat('en-GB', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : '—'
const messageOf = error => error.response?.data?.detail || error.response?.data?.title || 'The requested action could not be completed.'

export function CustomersPage() {
  const dialog = useAdminDialog()
  const [filters, setFilters] = useState(initialFilters)
  const [data, setData] = useState({ items: [], totalCount: 0 })
  const [shows, setShows] = useState([])
  const [selected, setSelected] = useState(null)
  const [editing, setEditing] = useState(false)
  const [form, setForm] = useState({ fullName: '', phone: '', email: '' })
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [toast, setToast] = useState(null)

  const load = () => { setLoading(true); setError(''); adminApi.customers(filters).then(setData).catch(() => setError('Customers could not be loaded.')).finally(() => setLoading(false)) }
  useEffect(() => { const timer = setTimeout(load, 200); return () => clearTimeout(timer) }, [filters])
  useEffect(() => { adminApi.shows({ page: 1, pageSize: 100 }).then(result => setShows(result.items ?? [])).catch(() => setShows([])) }, [])
  const open = (id, shouldEdit = false) => adminApi.customer(id).then(customer => { setSelected(customer); setForm({ fullName: customer.fullName, phone: customer.phone, email: customer.email ?? '' }); setEditing(shouldEdit) }).catch(() => setError('Customer history could not be loaded.'))
  const patch = value => setFilters(current => ({ ...current, ...value, page: value.page ?? 1 }))
  const exportData = async customerId => saveBlob(await adminApi.exportCustomers(customerId ? { customerId } : { search: filters.search, showId: filters.showId || undefined, reservationFrom: filters.reservationFrom || undefined, reservationTo: filters.reservationTo || undefined }), customerId ? `customer-${customerId}-history.csv` : 'customers.csv')
  const saveCustomer = async () => {
    if (!form.fullName.trim() || !form.phone.trim()) return
    setSaving(true)
    try { await adminApi.updateCustomer(selected.id, { ...form, countryPrefix: '' }); setToast({ message: 'Customer updated.', type: 'success' }); setEditing(false); await open(selected.id); load() }
    catch (requestError) { setToast({ message: messageOf(requestError), type: 'error' }) }
    finally { setSaving(false) }
  }
  const deleteCustomer = async customer => {
    const reservationCount = customer.reservations?.length ?? customer.totalReservations ?? 0
    if (!await dialog.confirm({ title: 'Permanently delete customer?', message: `${customer.fullName} and ${reservationCount} reservation record(s) will be permanently deleted. Their active seats will be released. This cannot be undone.`, confirmLabel: 'Delete permanently', danger: true })) return
    setSaving(true)
    try { await adminApi.deleteCustomer(customer.id); setSelected(null); setToast({ message: 'Customer and reservation records deleted.', type: 'success' }); load() }
    catch (requestError) { setToast({ message: messageOf(requestError), type: 'error' }) }
    finally { setSaving(false) }
  }

  return <>
    <PageHeader eyebrow="Reservations" title="All-time customers" description="Customer contacts, reservation activity and retained history." actions={<button className="admin-outline-button" onClick={() => exportData()}>Export filtered</button>} />
    <section className="admin-panel customers-panel">
      <div className="customers-toolbar">
        <label><span>Search customers</span><input placeholder="Name, phone or email" value={filters.search} onChange={event => patch({ search: event.target.value })} /></label>
        <label><span>Play</span><select value={filters.showId} onChange={event => patch({ showId: event.target.value })}><option value="">All plays</option>{shows.map(show => <option key={show.id} value={show.id}>{show.titleSq || show.titleEn}</option>)}</select></label>
        <label><span>Reserved from</span><input type="date" value={filters.reservationFrom} onChange={event => patch({ reservationFrom: event.target.value })} /></label>
        <label><span>Reserved to</span><input type="date" value={filters.reservationTo} onChange={event => patch({ reservationTo: event.target.value })} /></label>
        <label><span>Sort by</span><select value={filters.sortBy} onChange={event => patch({ sortBy: event.target.value })}><option value="lastReservation">Most recent</option><option value="firstReservation">First reservation</option><option value="customer">Customer name</option><option value="phone">Phone</option><option value="email">Email</option></select></label>
        <button className="admin-text-button" onClick={() => setFilters(initialFilters)}>Clear filters</button>
      </div>
      {error && <div className="admin-request-error" role="alert"><p>{error}</p></div>}
      {loading ? <LoadingSkeleton rows={5} /> : !data.items.length ? <EmptyState title="No customers found" text="No customers match the current filters." /> : <div className="admin-table-wrap"><table className="admin-table customers-table"><thead><tr><th>Customer</th><th>Contact</th><th>First reservation</th><th>Most recent</th><th>Reservations</th><th>Seats</th><th>Actions</th></tr></thead><tbody>{data.items.map(customer => <tr key={customer.id} onClick={() => open(customer.id)}><td><strong>{customer.fullName}</strong>{customer.anonymizedAt && <small>Anonymized</small>}</td><td><span>{customer.phone}</span><small>{customer.email || 'No email'}</small></td><td>{formatDate(customer.firstReservationDate)}</td><td>{formatDate(customer.mostRecentReservationDate)}</td><td><span className="customer-count-badge">{customer.totalReservations}</span></td><td><span className="customer-count-badge seats">{customer.totalReservedSeats}</span></td><td><div className="customer-row-actions"><button className="admin-outline-button" onClick={event => { event.stopPropagation(); open(customer.id, true) }}>Edit</button><button className="admin-danger-button" onClick={event => { event.stopPropagation(); void deleteCustomer(customer) }}>Delete</button></div></td></tr>)}</tbody></table></div>}
      <footer className="customers-pagination"><button disabled={filters.page <= 1} onClick={() => patch({ page: filters.page - 1 })}>Previous</button><span>Page {filters.page} · {data.totalCount} customers</span><button disabled={filters.page * filters.pageSize >= data.totalCount} onClick={() => patch({ page: filters.page + 1 })}>Next</button></footer>
    </section>
    {selected && <div className="admin-modal-backdrop" onMouseDown={event => event.target === event.currentTarget && setSelected(null)}><section className="admin-modal customer-detail-modal"><header><div><p className="admin-eyebrow">Customer profile</p><h2>{selected.fullName}</h2><p>{selected.phone} · {selected.email || 'No email'}</p></div><button onClick={() => setSelected(null)} aria-label="Close">×</button></header>
      <div className="customer-profile-summary"><div><span>Reservations</span><strong>{selected.reservations.length}</strong></div><div><span>First seen</span><strong>{formatDate(selected.createdAt)}</strong></div><div><span>Total seats</span><strong>{selected.reservations.reduce((total, reservation) => total + reservation.seats.length, 0)}</strong></div></div>
      {editing ? <div className="customer-edit-form"><label><span>Full name</span><input value={form.fullName} onChange={event => setForm({ ...form, fullName: event.target.value })} /></label><label><span>Phone</span><input value={form.phone} onChange={event => setForm({ ...form, phone: event.target.value })} /></label><label><span>Email</span><input type="email" value={form.email} onChange={event => setForm({ ...form, email: event.target.value })} /></label><div><button className="admin-primary-button" disabled={saving || !form.fullName.trim() || !form.phone.trim()} onClick={saveCustomer}>Save changes</button><button className="admin-text-button" onClick={() => setEditing(false)}>Cancel</button></div></div> : <div className="customer-modal-actions"><button className="admin-primary-button" onClick={() => setEditing(true)}>Edit customer</button><button className="admin-outline-button" onClick={() => exportData(selected.id)}>Export history</button><button className="admin-danger-button" onClick={() => void deleteCustomer(selected)}>Delete customer</button></div>}
      <div className="customer-history-heading"><div><p className="admin-eyebrow">Reservation history</p><h3>{selected.reservations.length} record(s)</h3></div></div>
      <div className="admin-table-wrap"><table className="admin-table"><thead><tr><th>Play / performance</th><th>Venue</th><th>Seats</th><th>Confirmation</th><th>Status</th><th>Source / comment</th></tr></thead><tbody>{selected.reservations.map(reservation => <tr key={reservation.reservationId}><td><strong>{reservation.play}</strong><small>{formatDate(reservation.performanceDate)}</small></td><td>{reservation.venue}</td><td>{reservation.seats.join(', ') || '—'}</td><td><StatusBadge status={reservation.confirmationStatus} /></td><td><StatusBadge status={reservation.status} /></td><td>{reservation.source}<small>{reservation.adminComment || 'No comment'}</small></td></tr>)}</tbody></table></div>
    </section></div>}
    {toast && <Toast message={toast.message} type={toast.type} onClose={() => setToast(null)} />}
  </>
}
