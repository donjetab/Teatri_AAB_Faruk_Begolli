import { useEffect, useMemo, useState } from 'react'
import { adminApi } from '../api'
import { EmptyState, LoadingSkeleton, PageHeader, StatusBadge, Toast } from '../components/AdminUi'
import { useAdminDialog } from '../components/AdminDialog'
import { SeatingSchema } from '../../components/seating/SeatingSchema'

const emptyFilters = { search: '', seat: '', section: '', row: '', reservationDate: '', confirmationStatus: '', status: '', source: '', sortBy: 'reservedAt', sortDirection: 'desc' }
const emptyCustomer = { fullName: '', countryPrefix: '+383', phone: '', email: '' }
const reservationSeatActions = new Set(['move', 'add', 'remove', 'confirm-seats'])
const messageOf = error => {
  const validation = error.response?.data?.errors
  if (validation) return Object.values(validation).flat().join(' ')
  return error.response?.data?.detail || error.response?.data?.title || 'The requested action could not be completed.'
}
const dateTime = value => new Intl.DateTimeFormat('en-GB', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))

export function ReservationsPage() {
  const dialog = useAdminDialog()
  const [performances, setPerformances] = useState([])
  const [playId, setPlayId] = useState('')
  const [performanceId, setPerformanceId] = useState('')
  const [layout, setLayout] = useState(null)
  const [seats, setSeats] = useState([])
  const [data, setData] = useState({ items: [], totalCount: 0, page: 1, pageSize: 25 })
  const [filters, setFilters] = useState(emptyFilters)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [toast, setToast] = useState(null)
  const [selectedReservationId, setSelectedReservationId] = useState(null)
  const [selectedReservationRecord, setSelectedReservationRecord] = useState(null)
  const [selectedBlock, setSelectedBlock] = useState(null)
  const [selectedSeatIds, setSelectedSeatIds] = useState([])
  const [action, setAction] = useState(null)
  const [customers, setCustomers] = useState([])
  const [customerSearch, setCustomerSearch] = useState('')
  const [customerMode, setCustomerMode] = useState('existing')
  const [customerId, setCustomerId] = useState('')
  const [customerForm, setCustomerForm] = useState(emptyCustomer)
  const [comment, setComment] = useState('')
  const [saving, setSaving] = useState(false)

  const selectedReservation = selectedReservationRecord ?? data.items.find(item => item.id === selectedReservationId) ?? null
  const internalPerformances = useMemo(() => performances.filter(item => item.reservationMode === 'Internal'), [performances])
  const plays = useMemo(() => [...new Map(internalPerformances.map(item => [item.showId, item.showTitle])).entries()], [internalPerformances])
  const performanceOptions = internalPerformances.filter(item => !playId || String(item.showId) === playId)
  const openPerformances = useMemo(() => {
    const now = Date.now()
    return internalPerformances.filter(item => item.status === 'Scheduled' && item.isPublished && item.reservationsEnabled && new Date(item.startDateTimeUtc).getTime() > now && (!item.reservationOpensAtUtc || new Date(item.reservationOpensAtUtc).getTime() <= now) && (!item.reservationClosesAtUtc || new Date(item.reservationClosesAtUtc).getTime() >= now))
  }, [internalPerformances])
  const totalPages = Math.max(1, Math.ceil(data.totalCount / data.pageSize))
  const newCustomerEmailValid = !customerForm.email.trim() || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(customerForm.email.trim())

  useEffect(() => {
    adminApi.performances({ pageSize: 100 }).then(result => setPerformances(result.items)).catch(error => setError(messageOf(error)))
  }, [])
  useEffect(() => {
    const timer = setTimeout(() => adminApi.customers({ search: customerSearch, pageSize: 100 }).then(result => setCustomers(result.items)).catch(() => setCustomers([])), 200)
    return () => clearTimeout(timer)
  }, [customerSearch])

  const queryParams = (requestedPage = page) => Object.fromEntries(Object.entries({ performanceId, ...filters, page: requestedPage, pageSize: 25 }).filter(([, value]) => value !== ''))
  const refresh = async (requestedPage = page) => {
    if (!performanceId) return
    setLoading(true); setError('')
    try {
      const [nextLayout, nextSeats, reservations] = await Promise.all([
        adminApi.performanceLayout(performanceId), adminApi.reservationSeats(performanceId), adminApi.reservations(queryParams(requestedPage)),
      ])
      setLayout(nextLayout); setSeats(nextSeats); setData(reservations); setPage(reservations.page)
    } catch (requestError) { setError(messageOf(requestError)) }
    finally { setLoading(false) }
  }
  useEffect(() => { if (performanceId) void refresh(1); else { setLayout(null); setSeats([]); setData({ items: [], totalCount: 0, page: 1, pageSize: 25 }) } }, [performanceId, filters.search, filters.seat, filters.section, filters.row, filters.reservationDate, filters.confirmationStatus, filters.status, filters.source, filters.sortBy, filters.sortDirection])

  const choosePerformance = value => {
    setPerformanceId(value); setPage(1); setAction(null); setSelectedSeatIds([]); setSelectedReservationId(null); setSelectedReservationRecord(null); setSelectedBlock(null)
    const item = internalPerformances.find(option => String(option.id) === value)
    if (item) setPlayId(String(item.showId))
  }
  const selectReservation = reservation => {
    setSelectedReservationId(reservation.id); setSelectedReservationRecord(reservation); setSelectedBlock(null); setAction(null); setSelectedSeatIds(reservation.activeSeatIds); setComment(reservation.adminComment ?? '')
  }
  const seatClick = seat => {
    const owned = selectedReservation?.activeSeatIds.includes(seat.id)
    if (action === 'add' && owned) return
    const canEditReservationSeat = reservationSeatActions.has(action) && (owned || (action !== 'remove' && seat.state === 'Available'))
    const canChoose = ((action === 'new' || action === 'block') && seat.state === 'Available') || canEditReservationSeat
    if (canChoose && seat.state !== 'Disabled') { setSelectedSeatIds(current => current.includes(seat.id) ? current.filter(id => id !== seat.id) : [...current, seat.id]); return }
    if (seat.reservationId) { const reservation = data.items.find(item => item.id === seat.reservationId); if (reservation) selectReservation(reservation); else adminApi.reservation(seat.reservationId).then(selectReservation).catch(error => setToast({ message: messageOf(error), type: 'error' })); return }
    if (seat.state === 'AdminBlocked') { setSelectedBlock(seat); setSelectedReservationId(null); setSelectedReservationRecord(null); setSelectedSeatIds([seat.id]); setAction(null) }
  }
  const beginNew = () => { setAction('new'); setSelectedReservationId(null); setSelectedReservationRecord(null); setSelectedBlock(null); setSelectedSeatIds([]); setCustomerId(''); setCustomerForm(emptyCustomer); setComment('') }
  const beginBlock = () => { setAction('block'); setSelectedReservationId(null); setSelectedReservationRecord(null); setSelectedBlock(null); setSelectedSeatIds([]); setComment('') }
  const run = async (work, success) => {
    setSaving(true); setError('')
    try {
      await work(); setToast({ message: success, type: 'success' }); setAction(null); setSelectedSeatIds([]); await refresh(page)
      if (selectedReservationId) { const updated = await adminApi.reservation(selectedReservationId); setSelectedReservationRecord(updated); setSelectedSeatIds(updated.activeSeatIds) }
    }
    catch (requestError) { setToast({ message: messageOf(requestError), type: 'error' }) }
    finally { setSaving(false) }
  }
  const createReservation = () => run(() => adminApi.createReservation({ customerId: customerMode === 'existing' ? Number(customerId) : null, customer: customerMode === 'new' ? { ...customerForm, fullName: customerForm.fullName.trim(), phone: customerForm.phone.trim(), email: customerForm.email.trim() || null } : null, seatIds: selectedSeatIds, comment }), 'Reservation created.')
  const blockSeats = () => run(() => adminApi.blockSeats(performanceId, { seatIds: selectedSeatIds, comment }), 'Seats blocked.')
  const updateReservation = (payload, success) => run(() => adminApi.updateReservation(selectedReservation.id, payload), success)
  const changeStatus = async status => {
    const releasing = status === 'Released' || status === 'Cancelled'
    if (releasing && !await dialog.confirm({ title: `${status} reservation?`, message: 'The seats will immediately become available publicly. Reservation history will be retained.', confirmLabel: status, danger: true })) return
    updateReservation({ status }, `Reservation ${status.toLowerCase()}.`)
  }
  const deleteReservation = async () => {
    if (!await dialog.confirm({ title: 'Permanently delete reservation?', message: `Reservation #${selectedReservation.id} and all of its seat history will be permanently deleted. This cannot be undone.`, confirmLabel: 'Delete permanently', danger: true })) return
    setSaving(true)
    try {
      await adminApi.deleteReservation(selectedReservation.id)
      setSelectedReservationId(null); setSelectedReservationRecord(null); setSelectedSeatIds([]); setAction(null)
      setToast({ message: 'Reservation permanently deleted.', type: 'success' }); await refresh(page)
    } catch (requestError) { setToast({ message: messageOf(requestError), type: 'error' }) }
    finally { setSaving(false) }
  }
  const releaseBlock = async () => {
    if (!await dialog.confirm({ title: 'Release blocked seat?', message: `Seat ${selectedBlock.section} ${selectedBlock.row}-${selectedBlock.label} will immediately become publicly available.`, confirmLabel: 'Release seat', danger: true })) return
    run(() => adminApi.releaseBlock(selectedBlock.allocationId), 'Blocked seat released.'); setSelectedBlock(null)
  }
  const saveCustomer = () => run(() => adminApi.updateCustomer(selectedReservation.customerId, customerForm), 'Customer updated.')
  const download = async (kind) => {
    try {
      const params = kind === 'all' ? { performanceId } : kind === 'confirmed' ? { performanceId, confirmedOnly: true } : { performanceId, ...filters }
      const blob = await adminApi.exportReservations(params); const url = URL.createObjectURL(blob); const link = document.createElement('a'); link.href = url; link.download = `reservations-${kind}.csv`; link.click(); URL.revokeObjectURL(url)
    } catch (requestError) { setToast({ message: messageOf(requestError), type: 'error' }) }
  }

  return <>
    <PageHeader eyebrow="Operations" title="Performance reservations" description="Manage customers, live seat availability, confirmations and reservation history." actions={<div className="reservation-export-actions"><button className="admin-outline-button" disabled={!performanceId} onClick={() => download('all')}>Export all</button><button className="admin-outline-button" disabled={!performanceId} onClick={() => download('filtered')}>Export filtered</button><button className="admin-primary-button" disabled={!performanceId} onClick={() => download('confirmed')}>Confirmed attendees</button></div>} />

    <section className="open-reservations-panel">
      <header><div><span>Now accepting reservations</span><h2>Open performances</h2></div><strong>{openPerformances.length}</strong></header>
      {openPerformances.length ? <div className="open-reservation-cards">{openPerformances.map(item => <button key={item.id} className={String(item.id) === performanceId ? 'active' : ''} onClick={() => choosePerformance(String(item.id))}><span>{item.showTitle}</span><strong>{dateTime(item.startDateTimeUtc)}</strong><small>{item.venue || item.hall || 'Venue to be announced'}</small><i>Open</i></button>)}</div> : <p>No internal performances are accepting reservations right now.</p>}
    </section>

    <section className="admin-panel reservation-performance-picker without-venue">
      <label><span>Play</span><select value={playId} onChange={event => { setPlayId(event.target.value); setPerformanceId('') }}><option value="">All plays</option>{plays.map(([id, title]) => <option value={id} key={id}>{title}</option>)}</select></label>
      <label><span>Performance date & time</span><select value={performanceId} onChange={event => choosePerformance(event.target.value)}><option value="">Select an internal-reservation performance</option>{performanceOptions.map(item => <option value={item.id} key={item.id}>{dateTime(item.startDateTimeUtc)} · {item.venue || item.hall || 'No venue'}</option>)}</select></label>
    </section>

    {!performanceId ? <section className="admin-panel"><EmptyState title="Select a performance" text="Choose a play, venue and performance date to load its live seating and reservations." /></section> : <>
      <section className="admin-panel reservation-filter-panel">
        <div className="reservation-filters">
          <label><span>Customer or phone</span><input value={filters.search} onChange={event => setFilters({ ...filters, search: event.target.value })} /></label>
          <label><span>Seat</span><input value={filters.seat} onChange={event => setFilters({ ...filters, seat: event.target.value })} /></label>
          <label><span>Section</span><input value={filters.section} onChange={event => setFilters({ ...filters, section: event.target.value })} /></label>
          <label><span>Row</span><input value={filters.row} onChange={event => setFilters({ ...filters, row: event.target.value })} /></label>
          <label><span>Reservation date</span><input type="date" value={filters.reservationDate} onChange={event => setFilters({ ...filters, reservationDate: event.target.value })} /></label>
          <label><span>Confirmation</span><select value={filters.confirmationStatus} onChange={event => setFilters({ ...filters, confirmationStatus: event.target.value })}><option value="">All</option><option>Unconfirmed</option><option>Confirmed</option></select></label>
          <label><span>Status</span><select value={filters.status} onChange={event => setFilters({ ...filters, status: event.target.value })}><option value="">All</option><option>Active</option><option>Released</option><option>Cancelled</option></select></label>
          <label><span>Source</span><select value={filters.source} onChange={event => setFilters({ ...filters, source: event.target.value })}><option value="">All</option><option value="PublicWebsite">Public website</option><option value="Admin">Admin-created</option></select></label>
          <label><span>Sort</span><select value={`${filters.sortBy}:${filters.sortDirection}`} onChange={event => { const [sortBy, sortDirection] = event.target.value.split(':'); setFilters({ ...filters, sortBy, sortDirection }) }}><option value="reservedAt:desc">Newest first</option><option value="reservedAt:asc">Oldest first</option><option value="customer:asc">Customer A–Z</option><option value="customer:desc">Customer Z–A</option><option value="phone:asc">Phone</option><option value="seat:asc">Seat</option><option value="section:asc">Section</option><option value="row:asc">Row</option><option value="confirmation:asc">Confirmation</option><option value="status:asc">Status</option><option value="source:asc">Source</option></select></label>
        </div>
        <button className="admin-text-button" onClick={() => { setFilters(emptyFilters); setPage(1) }}>Clear all filters</button>
      </section>

      {error && <section className="admin-panel admin-request-error" role="alert"><div>!</div><h2>Reservations could not be loaded</h2><p>{error}</p><button className="admin-primary-button" onClick={() => refresh(page)}>Try again</button></section>}
      {loading && !layout ? <LoadingSkeleton rows={6} /> : layout && <div className="reservation-workspace">
        <section className="admin-panel reservation-map-panel">
          <header><div><span>Live seating</span><h2>{layout.name}</h2></div><div className="reservation-map-actions"><button className="admin-primary-button" onClick={beginNew}>New reservation</button><button className="admin-outline-button" onClick={beginBlock}>Block seats</button></div></header>
          <div className="seat-state-legend"><span className="available">Available</span><span className="unconfirmed">Public · unconfirmed</span><span className="admin-reserved">Admin reservation</span><span className="confirmed">Confirmed</span><span className="blocked">Admin-blocked</span><span className="disabled">Disabled</span></div>
          <SeatingSchema schema={layout} seats={seats} selectedIds={selectedSeatIds} editor onSeatClick={seatClick} />
          <p className="reservation-selection-summary">{selectedSeatIds.length ? `${selectedSeatIds.length} seat(s) selected` : 'Click an occupied seat for details, or begin an action to select available seats.'}</p>
        </section>

        <aside className="admin-panel reservation-detail-panel">
          {action === 'new' ? <><header><span>Admin action</span><h2>New reservation</h2></header><div className="reservation-mode-switch reservation-customer-mode-switch"><button type="button" className={customerMode === 'new' ? 'active' : ''} onClick={() => { setCustomerMode('new'); setCustomerId('') }}>Create customer</button><button type="button" className={customerMode === 'existing' ? 'active' : ''} onClick={() => setCustomerMode('existing')}>Choose existing customer</button></div>{customerMode === 'existing' ? <><label><span>Search the customer list</span><input value={customerSearch} onChange={event => setCustomerSearch(event.target.value)} placeholder="Name, phone or email" /></label><div className="reservation-customer-results">{customers.length ? customers.map(item => <button type="button" className={String(item.id) === String(customerId) ? 'is-selected' : ''} onClick={() => setCustomerId(String(item.id))} key={item.id}><strong>{item.fullName}</strong><span>{item.phone}{item.email ? ` · ${item.email}` : ''}</span></button>) : <p>No customers match this search.</p>}</div></> : <><CustomerFields value={customerForm} onChange={setCustomerForm} />{!newCustomerEmailValid && <p className="reservation-action-hint">Enter a complete email address or leave the optional email field empty.</p>}</>}<label><span>Admin comment</span><textarea value={comment} onChange={event => setComment(event.target.value)} /></label>{!selectedSeatIds.length && <p className="reservation-action-hint">Select at least one white available chair from the seating map.</p>}<button className="admin-primary-button" disabled={saving || !selectedSeatIds.length || (customerMode === 'existing' ? !customerId : !customerForm.fullName || !customerForm.phone.trim() || !newCustomerEmailValid)} onClick={createReservation}>{customerMode === 'new' ? 'Create customer & reservation' : 'Create reservation'}</button><button className="admin-text-button" onClick={() => { setAction(null); setSelectedSeatIds([]) }}>Cancel</button></>
            : action === 'block' ? <><header><span>Admin action</span><h2>Block seats</h2></header><p>Blocked seats have no customer and appear unavailable publicly.</p><label><span>Reason or comment</span><textarea value={comment} onChange={event => setComment(event.target.value)} /></label><button className="admin-primary-button" disabled={saving || !selectedSeatIds.length} onClick={blockSeats}>Block {selectedSeatIds.length || ''} seat(s)</button><button className="admin-text-button" onClick={() => { setAction(null); setSelectedSeatIds([]) }}>Cancel</button></>
              : selectedBlock ? <><header><span>Admin-blocked</span><h2>{selectedBlock.section} {selectedBlock.row}-{selectedBlock.label}</h2></header><p>This seat is intentionally unavailable and has no customer reservation.</p>{selectedBlock.allocationComment && <div className="reservation-block-comment"><span>Admin comment</span><p>{selectedBlock.allocationComment}</p></div>}<button className="admin-danger-button" onClick={releaseBlock}>Release blocked seat</button></>
                : selectedReservation ? <ReservationDetails reservation={selectedReservation} action={action} selectedSeatIds={selectedSeatIds} comment={comment} setComment={setComment} saving={saving} onBeginSeatAction={mode => { setAction(mode); setSelectedSeatIds(selectedReservation.activeSeatIds) }} onSaveSeats={(confirm = false) => updateReservation({ seatIds: selectedSeatIds, ...(confirm ? { confirmationStatus: 'Confirmed' } : {}) }, confirm ? 'Seats reviewed and reservation confirmed.' : 'Reserved seats updated.')} onCancelSeats={() => { setAction(null); setSelectedSeatIds(selectedReservation.activeSeatIds) }} onComment={() => updateReservation({ adminComment: comment }, 'Comment updated.')} onConfirm={() => selectedReservation.confirmationStatus === 'Confirmed' ? updateReservation({ confirmationStatus: 'Unconfirmed' }, 'Reservation unconfirmed.') : (() => { setAction('confirm-seats'); setSelectedSeatIds(selectedReservation.activeSeatIds) })()} onStatus={changeStatus} onDelete={deleteReservation} onEditCustomer={() => setCustomerForm({ fullName: selectedReservation.customerName, countryPrefix: '', phone: selectedReservation.phone, email: selectedReservation.email ?? '' })} customerForm={customerForm} setCustomerForm={setCustomerForm} onSaveCustomer={saveCustomer} />
                  : <EmptyState title="No selection" text="Select a reservation row or occupied seat to view customer and reservation details." />}
        </aside>
      </div>}

      <section className="admin-panel reservation-table-panel">
        <header><div><span>Reservation records</span><h2>{data.totalCount} result(s)</h2></div></header>
        {!loading && !data.items.length ? <EmptyState title="No reservations found" text="There are no reservations matching the current filters." /> : <div className="admin-table-wrap"><table className="admin-table reservation-table"><thead><tr><th>Customer</th><th>Reserved seats</th><th>Section / row</th><th>Reservation date</th><th>Confirmation</th><th>Status</th><th>Source</th><th>Comment</th></tr></thead><tbody>{data.items.map(item => <tr className={item.id === selectedReservationId ? 'selected' : ''} key={item.id} onClick={() => selectReservation(item)}><td><strong>{item.customerName}</strong><small>{item.phone}</small><small>{item.email || 'No email'}</small></td><td>{item.seatDetails.map(seat => seat.label).join(', ') || '—'}</td><td>{[...new Set(item.seatDetails.map(seat => `${seat.section} / ${seat.row}`))].join(', ') || '—'}</td><td>{dateTime(item.reservedAt)}</td><td><StatusBadge status={item.confirmationStatus} /></td><td><StatusBadge status={item.status} /></td><td>{item.source === 'PublicWebsite' ? 'Public website' : 'Admin-created'}</td><td>{item.adminComment || '—'}</td></tr>)}</tbody></table></div>}
        <footer className="reservation-pagination"><button disabled={page <= 1 || loading} onClick={() => refresh(page - 1)}>Previous</button><span>Page {page} of {totalPages}</span><button disabled={page >= totalPages || loading} onClick={() => refresh(page + 1)}>Next</button></footer>
      </section>
    </>}
    {toast && <Toast message={toast.message} type={toast.type} onClose={() => setToast(null)} />}
  </>
}

function CustomerFields({ value, onChange }) {
  return <div className="customer-fields"><label><span>Full name</span><input value={value.fullName} onChange={event => onChange({ ...value, fullName: event.target.value })} /></label><div><label><span>Country prefix</span><input value={value.countryPrefix} onChange={event => onChange({ ...value, countryPrefix: event.target.value })} /></label><label><span>Phone</span><input value={value.phone} onChange={event => onChange({ ...value, phone: event.target.value })} /></label></div><label><span>Email (optional)</span><input type="email" value={value.email} onChange={event => onChange({ ...value, email: event.target.value })} /></label></div>
}

function ReservationDetails({ reservation, action, selectedSeatIds, comment, setComment, saving, onBeginSeatAction, onSaveSeats, onCancelSeats, onComment, onConfirm, onStatus, onDelete, onEditCustomer, customerForm, setCustomerForm, onSaveCustomer }) {
  const [editingCustomer, setEditingCustomer] = useState(false)
  const originalCount = reservation.activeSeatIds.length
  const seatAction = reservationSeatActions.has(action)
  const seatActionCopy = {
    move: ['Move seats', `Select exactly ${originalCount} seats. The reservation must keep its current seat count.`, selectedSeatIds.length === originalCount],
    add: ['Add seats', `Keep the existing seats and select at least one more.`, selectedSeatIds.length > originalCount],
    remove: ['Remove seats', `Deselect one or more existing seats. At least one seat must remain.`, selectedSeatIds.length > 0 && selectedSeatIds.length < originalCount],
    'confirm-seats': ['Review seats before confirmation', 'Check the final number of attendees and adjust their seats before confirming.', selectedSeatIds.length > 0],
  }[action]

  return <>
    <header><span>Reservation #{reservation.id}</span><h2>{reservation.customerName}</h2></header>
    {editingCustomer ? <><CustomerFields value={customerForm} onChange={setCustomerForm} /><button className="admin-primary-button" disabled={saving} onClick={() => { onSaveCustomer(); setEditingCustomer(false) }}>Save customer</button><button className="admin-text-button" onClick={() => setEditingCustomer(false)}>Cancel</button></> : <div className="reservation-customer-card"><strong>{reservation.customerName}</strong><span>{reservation.phone}</span><span>{reservation.email || 'No email provided'}</span><button className="admin-text-button" onClick={() => { onEditCustomer(); setEditingCustomer(true) }}>Edit customer</button></div>}
    <dl><div><dt>Seats</dt><dd>{reservation.seats.join(', ') || 'None active'}</dd></div><div><dt>Reserved</dt><dd>{dateTime(reservation.reservedAt)}</dd></div><div><dt>Source</dt><dd>{reservation.source === 'PublicWebsite' ? 'Public website' : 'Admin-created'}</dd></div></dl>
    {seatAction ? <div className="reservation-seat-edit"><h3>{seatActionCopy[0]}</h3><p>{seatActionCopy[1]}</p><strong>{selectedSeatIds.length} seat(s) selected</strong><button className="admin-primary-button" disabled={saving || !seatActionCopy[2]} onClick={() => onSaveSeats(action === 'confirm-seats')}>{action === 'confirm-seats' ? 'Confirm with these seats' : `Save ${action}`}</button><button className="admin-text-button" onClick={onCancelSeats}>Cancel</button></div> : <div className="reservation-seat-action-buttons"><button className="admin-outline-button" disabled={reservation.status !== 'Active'} onClick={() => onBeginSeatAction('move')}>Move seats</button><button className="admin-outline-button" disabled={reservation.status !== 'Active'} onClick={() => onBeginSeatAction('add')}>Add seats</button><button className="admin-outline-button" disabled={reservation.status !== 'Active' || originalCount <= 1} onClick={() => onBeginSeatAction('remove')}>Remove seats</button></div>}
    <label><span>Admin comment</span><textarea value={comment} onChange={event => setComment(event.target.value)} /></label><button className="admin-outline-button" disabled={saving} onClick={onComment}>Save comment</button>
    <div className="reservation-action-grid"><button className="admin-primary-button" disabled={saving || reservation.status !== 'Active' || seatAction} onClick={onConfirm}>{reservation.confirmationStatus === 'Confirmed' ? 'Unconfirm' : 'Review seats & confirm'}</button>{reservation.status === 'Active' ? <button className="admin-danger-button" disabled={seatAction} onClick={() => onStatus('Cancelled')}>Cancel reservation</button> : <button className="admin-primary-button" onClick={() => onStatus('Active')}>Reactivate reservation</button>}</div>
    <button className="admin-danger-button reservation-delete-button" disabled={saving} onClick={onDelete}>Delete permanently</button>
    <div className="reservation-status-line"><StatusBadge status={reservation.confirmationStatus} /><StatusBadge status={reservation.status} /></div>
  </>
}
