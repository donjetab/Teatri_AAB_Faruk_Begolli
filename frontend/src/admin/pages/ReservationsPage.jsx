import { useEffect, useMemo, useState } from 'react'
import { adminApi } from '../api'
import { EmptyState, LoadingSkeleton, PageHeader, StatusBadge, Toast } from '../components/AdminUi'
import { useAdminDialog } from '../components/AdminDialog'
import { SeatingSchema } from '../../components/seating/SeatingSchema'
import { useAdminLanguage } from '../AdminLanguageContext'

const emptyFilters = { search: '', seat: '', section: '', row: '', reservationDate: '', confirmationStatus: '', status: '', source: '', sortBy: 'reservedAt', sortDirection: 'desc' }
const emptyCustomer = { fullName: '', countryPrefix: '+383', phone: '', email: '' }
const reservationSeatActions = new Set(['move', 'add', 'remove', 'confirm-seats'])
const messageOf = error => {
  const validation = error.response?.data?.errors
  if (validation) return Object.values(validation).flat().join(' ')
  return error.response?.data?.detail || error.response?.data?.title || 'The requested action could not be completed.'
}
const dateTime = value => new Intl.DateTimeFormat(localStorage.getItem('admin-ui-language') === 'sq' ? 'sq-AL' : 'en-GB', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))

export function ReservationsPage() {
  useAdminLanguage()
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
  const [exportLayout, setExportLayout] = useState('reservation')

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
    const search = customerSearch.trim()
    if (search.length < 2) { setCustomers([]); return }
    const timer = setTimeout(() => adminApi.customers({ search, page: 1, pageSize: 8 }).then(result => setCustomers(result.items)).catch(() => setCustomers([])), 250)
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
    if (!action && seat.state === 'Available') {
      beginNew(seat.id)
      return
    }
    if (action === 'add' && owned) return
    const canEditReservationSeat = reservationSeatActions.has(action) && (owned || (action !== 'remove' && seat.state === 'Available'))
    const canChoose = ((action === 'new' || action === 'block') && seat.state === 'Available') || canEditReservationSeat
    if (canChoose && seat.state !== 'Disabled') { setSelectedSeatIds(current => current.includes(seat.id) ? current.filter(id => id !== seat.id) : [...current, seat.id]); return }
    if (seat.reservationId) { const reservation = data.items.find(item => item.id === seat.reservationId); if (reservation) selectReservation(reservation); else adminApi.reservation(seat.reservationId).then(selectReservation).catch(error => setToast({ message: messageOf(error), type: 'error' })); return }
    if (seat.state === 'AdminBlocked') { setSelectedBlock(seat); setSelectedReservationId(null); setSelectedReservationRecord(null); setSelectedSeatIds([seat.id]); setAction(null) }
  }
  const beginNew = (seatId = null) => { setAction('new'); setSelectedReservationId(null); setSelectedReservationRecord(null); setSelectedBlock(null); setSelectedSeatIds(seatId ? [seatId] : []); setCustomerId(''); setCustomerSearch(''); setCustomers([]); setCustomerForm(emptyCustomer); setComment('') }
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
      const format = { seatBased: exportLayout === 'seat' }
      const params = kind === 'all' ? { performanceId, ...format } : kind === 'confirmed' ? { performanceId, confirmedOnly: true, ...format } : { performanceId, ...filters, ...format }
      const blob = await adminApi.exportReservations(params); const url = URL.createObjectURL(blob); const link = document.createElement('a'); link.href = url; link.download = `reservations-${kind}-${exportLayout}.xlsx`; link.click(); URL.revokeObjectURL(url)
    } catch (requestError) { setToast({ message: messageOf(requestError), type: 'error' }) }
  }
  const exportSeatingReport = async () => {
    const reportWindow = window.open('', '_blank')
    if (!reportWindow) { setToast({ message: 'Allow pop-ups to create the printable seating report.', type: 'error' }); return }
    reportWindow.document.write('<p style="font-family:sans-serif;padding:24px">Preparing seating report…</p>')
    try {
      const svg = document.querySelector('.reservation-map-panel svg.seating-schema')
      if (!svg) throw new Error('The seating schema is unavailable.')
      const copy = svg.cloneNode(true)
      copy.setAttribute('xmlns', 'http://www.w3.org/2000/svg')
      const style = document.createElementNS('http://www.w3.org/2000/svg', 'style')
      style.textContent = [...document.styleSheets].flatMap(sheet => { try { return [...sheet.cssRules].map(rule => rule.cssText) } catch { return [] } }).join('\n')
      copy.prepend(style)
      const first = await adminApi.reservations({ performanceId, ...filters, page: 1, pageSize: 100 })
      const pages = Math.ceil(first.totalCount / 100)
      const remaining = pages > 1 ? await Promise.all(Array.from({ length: pages - 1 }, (_, index) => adminApi.reservations({ performanceId, ...filters, page: index + 2, pageSize: 100 }))) : []
      const records = [first, ...remaining].flatMap(result => result.items)
      const escape = value => String(value ?? '').replace(/[&<>"']/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[character])
      const performance = internalPerformances.find(item => String(item.id) === String(performanceId))
      const reportRow = (item, section, row, seats) => `<tr><td>${escape(item.customerName)}<small>${escape(item.phone)}${item.email ? `<br>${escape(item.email)}` : ''}</small></td><td>${escape(section)}</td><td>${escape(row)}</td><td>${escape(seats)}</td><td>${escape(dateTime(item.reservedAt))}</td><td>${escape(item.confirmationStatus)}</td><td>${escape(item.status)}</td><td>${escape(item.source === 'PublicWebsite' ? 'Public website' : 'Admin-created')}</td><td>${escape(item.adminComment || '—')}</td></tr>`
      const rows = records.flatMap(item => {
        const activeSeats = item.seatDetails.filter(seat => seat.isActive)
        const itemSeats = activeSeats.length ? activeSeats : item.seatDetails
        return exportLayout === 'seat'
          ? itemSeats.map(seat => reportRow(item, seat.section, seat.row, seat.label))
          : [reportRow(item, [...new Set(itemSeats.map(seat => seat.section))].join(', '), [...new Set(itemSeats.map(seat => seat.row))].join(', '), itemSeats.map(seat => seat.label).join(', '))]
      }).join('')
      reportWindow.document.open()
      reportWindow.document.write(`<!doctype html><html><head><meta charset="utf-8"><title>Seating report</title><style>@page{size:landscape;margin:12mm}*{box-sizing:border-box}body{margin:0;color:#171214;font:12px Arial,sans-serif}header{display:flex;justify-content:space-between;gap:20px;margin-bottom:14px;border-bottom:2px solid #78141d;padding-bottom:10px}h1{margin:0;font-size:24px;text-transform:uppercase}header p{margin:4px 0 0;color:#665b5e}.report-count{font-size:18px;font-weight:bold}.schema{height:62vh;min-height:420px;margin-bottom:18px;padding:10px;border:1px solid #cfc7c9;border-radius:10px;background:#110e0f}.schema svg{width:100%;height:100%;display:block}h2{margin:18px 0 8px;font-size:16px;text-transform:uppercase}table{width:100%;border-collapse:collapse;font-size:10px}th{background:#78141d;color:white;text-align:left}th,td{padding:7px;border:1px solid #d8d1d3;vertical-align:top}td small{display:block;margin-top:3px;color:#756b6d}tbody tr:nth-child(even){background:#f5f2f3}@media print{.schema{break-after:page}thead{display:table-header-group}tr{break-inside:avoid}}</style></head><body><header><div><h1>${escape(performance?.showTitle || 'Performance seating')}</h1><p>${escape(performance ? dateTime(performance.startDateTimeUtc) : '')} · ${escape(performance?.venue || performance?.hall || '')}</p></div><div class="report-count">${records.length} reservation${records.length === 1 ? '' : 's'}</div></header><div class="schema">${new XMLSerializer().serializeToString(copy)}</div><h2>Reservation records</h2><table><thead><tr><th>Customer</th><th>Seats</th><th>Reserved</th><th>Confirmation</th><th>Status</th><th>Source</th><th>Comment</th></tr></thead><tbody>${rows || '<tr><td colspan="7">No reservations match the selected filters.</td></tr>'}</tbody></table><script>window.onload=()=>setTimeout(()=>window.print(),300)<\/script></body></html>`)
      reportWindow.document.close()
      const reportHeader = reportWindow.document.querySelector('thead tr')
      if (reportHeader) reportHeader.innerHTML = '<th>Customer</th><th>Section</th><th>Row</th><th>Seat</th><th>Reserved</th><th>Confirmation</th><th>Status</th><th>Source</th><th>Comment</th>'
      const emptyRow = reportWindow.document.querySelector('tbody td[colspan]')
      if (emptyRow) emptyRow.colSpan = 9
    } catch (reportError) {
      reportWindow.close()
      setToast({ message: reportError.message || 'The seating report could not be created.', type: 'error' })
    }
  }

  return <>
    <PageHeader eyebrow="Operations" title="Performance reservations" description="Manage customers, live seat availability, confirmations and reservation history." />

    <section className="open-reservations-panel">
      <header><div><span>Now accepting reservations</span><h2>Open performances</h2></div><strong>{openPerformances.length}</strong></header>
      {openPerformances.length ? <div className="open-reservation-cards">{openPerformances.map(item => <button key={item.id} className={String(item.id) === performanceId ? 'active' : ''} onClick={() => choosePerformance(String(item.id))}><span>{item.showTitle}</span><strong>{dateTime(item.startDateTimeUtc)}</strong><small>{item.venue || item.hall || 'Venue to be announced'}</small><i>Open</i></button>)}</div> : <p>No internal performances are accepting reservations right now.</p>}
    </section>

    <section className="admin-panel reservation-performance-picker without-venue">
      <label><span>Play</span><select value={playId} onChange={event => { setPlayId(event.target.value); setPerformanceId('') }}><option value="">All plays</option>{plays.map(([id, title]) => <option value={id} key={id}>{title}</option>)}</select></label>
      <label><span>Performance date & time</span><select value={performanceId} onChange={event => choosePerformance(event.target.value)}><option value="">Select an internal-reservation performance</option>{performanceOptions.map(item => <option value={item.id} key={item.id}>{dateTime(item.startDateTimeUtc)} · {item.venue || item.hall || 'No venue'}</option>)}</select></label>
    </section>

    {!performanceId ? <section className="admin-panel"><EmptyState title="Select a performance" text="Choose a play, venue and performance date to load its live seating and reservations." /></section> : <>
      {error && <section className="admin-panel admin-request-error" role="alert"><div>!</div><h2>Reservations could not be loaded</h2><p>{error}</p><button className="admin-primary-button" onClick={() => refresh(page)}>Try again</button></section>}
      {loading && !layout ? <LoadingSkeleton rows={6} /> : layout && <div className="reservation-workspace">
        <section className="admin-panel reservation-map-panel">
          <header><div><span>Live seating</span><h2>{layout.name}</h2></div><div className="reservation-map-actions">{selectedSeatIds.length > 0 && action && <button className="reservation-clear-seats" type="button" onClick={() => setSelectedSeatIds([])}>Clear {selectedSeatIds.length}</button>}<button className="admin-primary-button" onClick={() => beginNew()}>New reservation</button><button className="admin-outline-button" onClick={beginBlock}>Block seats</button></div></header>
          <div className="seat-state-legend"><span className="available">Available</span><span className="unconfirmed">Public · unconfirmed</span><span className="admin-reserved">Admin reservation</span><span className="confirmed">Confirmed</span><span className="blocked">Admin-blocked</span><span className="disabled">Disabled</span></div>
          <SeatingSchema schema={layout} seats={seats} selectedIds={selectedSeatIds} editor onSeatClick={seatClick} />
          <div className="reservation-selection-summary"><span>{selectedSeatIds.length ? `${selectedSeatIds.length} seat(s) selected` : 'Click an occupied seat for details, or select an available chair to start a reservation.'}</span>{selectedSeatIds.length > 0 && action && <button type="button" onClick={() => setSelectedSeatIds([])}>Clear selection</button>}</div>
        </section>

        <aside className="admin-panel reservation-detail-panel">
          {action === 'new' ? <div className="new-reservation-flow"><header><div><span>Admin action</span><h2>New reservation</h2></div><i>{selectedSeatIds.length}</i></header><div className="reservation-mode-switch reservation-customer-mode-switch"><button type="button" className={customerMode === 'new' ? 'active' : ''} onClick={() => { setCustomerMode('new'); setCustomerId(''); setCustomerSearch(''); setCustomers([]) }}>New customer</button><button type="button" className={customerMode === 'existing' ? 'active' : ''} onClick={() => setCustomerMode('existing')}>Existing customer</button></div>{customerMode === 'existing' ? <section className="reservation-customer-search"><label><span>Find a customer</span><input value={customerSearch} onChange={event => { setCustomerSearch(event.target.value); setCustomerId('') }} placeholder="Type at least 2 characters…" /></label>{customerSearch.trim().length < 2 ? <p className="reservation-search-prompt">Search by name, phone number, or email. Only the first 8 matches will be shown.</p> : <div className="reservation-customer-results">{customers.length ? customers.map(item => <button type="button" className={String(item.id) === String(customerId) ? 'is-selected' : ''} onClick={() => setCustomerId(String(item.id))} key={item.id}><i>{item.fullName.slice(0, 1).toUpperCase()}</i><span><strong>{item.fullName}</strong><small>{item.phone}{item.email ? ` · ${item.email}` : ''}</small></span><b>{String(item.id) === String(customerId) ? '✓' : '→'}</b></button>) : <p>No matching customers found.</p>}</div>}</section> : <><CustomerFields value={customerForm} onChange={setCustomerForm} />{!newCustomerEmailValid && <p className="reservation-action-hint">Enter a complete email address or leave the optional email field empty.</p>}</>}<section className="new-reservation-note"><label><span>Private note <small>Optional</small></span><textarea placeholder="Add information for the theatre team…" value={comment} onChange={event => setComment(event.target.value)} /></label></section><div className={`new-reservation-seat-summary ${selectedSeatIds.length ? 'ready' : ''}`}><i>{selectedSeatIds.length ? '✓' : '!'}</i><span><strong>{selectedSeatIds.length ? `${selectedSeatIds.length} seat${selectedSeatIds.length === 1 ? '' : 's'} selected` : 'No seats selected'}</strong><small>{selectedSeatIds.length ? 'Ready to create the reservation' : 'Select white available chairs from the seating map'}</small></span></div><button className="new-reservation-submit" disabled={saving || !selectedSeatIds.length || (customerMode === 'existing' ? !customerId : !customerForm.fullName || !customerForm.phone.trim() || !newCustomerEmailValid)} onClick={createReservation}><span>{customerMode === 'new' ? 'Create customer & reservation' : 'Create reservation'}</span><i>→</i></button><button className="new-reservation-cancel" onClick={() => { setAction(null); setSelectedSeatIds([]) }}>Cancel and close</button></div>
            : action === 'block' ? <><header><span>Admin action</span><h2>Block seats</h2></header><p>Blocked seats have no customer and appear unavailable publicly.</p><label><span>Reason or comment</span><textarea value={comment} onChange={event => setComment(event.target.value)} /></label><button className="admin-primary-button" disabled={saving || !selectedSeatIds.length} onClick={blockSeats}>Block {selectedSeatIds.length || ''} seat(s)</button><button className="admin-text-button" onClick={() => { setAction(null); setSelectedSeatIds([]) }}>Cancel</button></>
              : selectedBlock ? <><header><span>Admin-blocked</span><h2>{selectedBlock.section} {selectedBlock.row}-{selectedBlock.label}</h2></header><p>This seat is intentionally unavailable and has no customer reservation.</p>{selectedBlock.allocationComment && <div className="reservation-block-comment"><span>Admin comment</span><p>{selectedBlock.allocationComment}</p></div>}<button className="admin-danger-button" onClick={releaseBlock}>Release blocked seat</button></>
                : selectedReservation ? <ReservationDetails reservation={selectedReservation} action={action} selectedSeatIds={selectedSeatIds} comment={comment} setComment={setComment} saving={saving} onBeginSeatAction={mode => { setAction(mode); setSelectedSeatIds(selectedReservation.activeSeatIds) }} onSaveSeats={(confirm = false) => updateReservation({ seatIds: selectedSeatIds, ...(confirm ? { confirmationStatus: 'Confirmed' } : {}) }, confirm ? 'Seats reviewed and reservation confirmed.' : 'Reserved seats updated.')} onCancelSeats={() => { setAction(null); setSelectedSeatIds(selectedReservation.activeSeatIds) }} onComment={() => updateReservation({ adminComment: comment }, 'Comment updated.')} onConfirm={() => selectedReservation.confirmationStatus === 'Confirmed' ? updateReservation({ confirmationStatus: 'Unconfirmed' }, 'Reservation unconfirmed.') : (() => { setAction('confirm-seats'); setSelectedSeatIds(selectedReservation.activeSeatIds) })()} onStatus={changeStatus} onDelete={deleteReservation} onEditCustomer={() => setCustomerForm({ fullName: selectedReservation.customerName, countryPrefix: '', phone: selectedReservation.phone, email: selectedReservation.email ?? '' })} customerForm={customerForm} setCustomerForm={setCustomerForm} onSaveCustomer={saveCustomer} />
                  : <EmptyState title="No selection" text="Select a reservation row or occupied seat to view customer and reservation details." />}
        </aside>
      </div>}

      <section className="admin-panel reservation-data-tools">
        <header><div><span>Reservation records</span><h2>Filter and export data</h2></div><div className="reservation-export-actions"><label className="reservation-export-format"><span>Excel layout</span><select value={exportLayout} onChange={event => setExportLayout(event.target.value)}><option value="reservation">One row per reservation</option><option value="seat">One row per seat</option></select></label><button className="admin-outline-button" onClick={() => download('all')}>Export all Excel</button><button className="admin-outline-button" onClick={() => download('filtered')}>Filtered Excel</button><button className="admin-primary-button" onClick={exportSeatingReport}>Schema + reservations</button></div></header>
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
        <button className="admin-text-button reservation-clear-filters" onClick={() => { setFilters(emptyFilters); setPage(1) }}>Clear all filters</button>
      </section>

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
  const activeSeatDetails = reservation.seatDetails.filter(seat => seat.isActive)
  const displayedSeatDetails = activeSeatDetails.length ? activeSeatDetails : reservation.seatDetails
  const sections = [...new Set(displayedSeatDetails.map(seat => seat.section).filter(Boolean))].join(', ')
  const seatLabels = displayedSeatDetails.map(seat => `${seat.row ? `${seat.row}-` : ''}${seat.label}`).join(', ')
  const seatActionCopy = {
    move: ['Move seats', `Select exactly ${originalCount} seats. The reservation must keep its current seat count.`, selectedSeatIds.length === originalCount],
    add: ['Add seats', `Keep the existing seats and select at least one more.`, selectedSeatIds.length > originalCount],
    remove: ['Remove seats', `Deselect one or more existing seats. At least one seat must remain.`, selectedSeatIds.length > 0 && selectedSeatIds.length < originalCount],
    'confirm-seats': ['Review seats before confirmation', 'Check the final number of attendees and adjust their seats before confirming.', selectedSeatIds.length > 0],
  }[action]

  return <>
    <header><div><span>Reservation #{reservation.id}</span><h2>{reservation.customerName}</h2></div><div className="reservation-status-line"><StatusBadge status={reservation.confirmationStatus} /><StatusBadge status={reservation.status} /></div></header>
    {editingCustomer ? <><CustomerFields value={customerForm} onChange={setCustomerForm} /><button className="admin-primary-button" disabled={saving} onClick={() => { onSaveCustomer(); setEditingCustomer(false) }}>Save customer</button><button className="admin-text-button" onClick={() => setEditingCustomer(false)}>Cancel</button></> : <div className="reservation-customer-card"><i>{reservation.customerName.slice(0, 1).toUpperCase()}</i><div><strong>{reservation.customerName}</strong><span>{reservation.phone}</span><span>{reservation.email || 'No email provided'}</span></div><button className="reservation-edit-customer" onClick={() => { onEditCustomer(); setEditingCustomer(true) }}>Edit</button></div>}
    <dl><div><dt>Section</dt><dd>{sections || '—'}</dd></div><div><dt>Seats</dt><dd className="reservation-seat-list">{seatLabels || 'None active'}</dd></div><div><dt>Reserved</dt><dd>{dateTime(reservation.reservedAt)}</dd></div><div><dt>Source</dt><dd>{reservation.source === 'PublicWebsite' ? 'Public website' : 'Admin-created'}</dd></div></dl>
    <section className="reservation-seat-tools"><h3>Seat management</h3>{seatAction ? <div className="reservation-seat-edit"><h3>{seatActionCopy[0]}</h3><p>{seatActionCopy[1]}</p><strong>{selectedSeatIds.length} seat(s) selected</strong><button className="admin-primary-button" disabled={saving || !seatActionCopy[2]} onClick={() => onSaveSeats(action === 'confirm-seats')}>{action === 'confirm-seats' ? 'Confirm with these seats' : `Save ${action}`}</button><button className="admin-text-button" onClick={onCancelSeats}>Cancel</button></div> : <div className="reservation-seat-action-buttons"><button disabled={reservation.status !== 'Active'} onClick={() => onBeginSeatAction('move')}><i>↔</i><span>Move</span></button><button disabled={reservation.status !== 'Active'} onClick={() => onBeginSeatAction('add')}><i>＋</i><span>Add</span></button><button className="remove" disabled={reservation.status !== 'Active' || originalCount <= 1} onClick={() => onBeginSeatAction('remove')}><i>−</i><span>Remove</span></button></div>}</section>
    <section className="reservation-comment-box"><label><span>Private admin note</span><textarea placeholder="Add a note about this reservation…" value={comment} onChange={event => setComment(event.target.value)} /></label><button disabled={saving} onClick={onComment}>Save note</button></section>
    <section className="reservation-decision-actions"><button className={`reservation-confirm-button ${reservation.confirmationStatus === 'Confirmed' ? 'make-unconfirmed' : 'make-confirmed'}`} disabled={saving || reservation.status !== 'Active' || seatAction} onClick={onConfirm}><span>{reservation.confirmationStatus === 'Confirmed' ? 'Mark as unconfirmed' : 'Review seats & confirm'}</span><i>{reservation.confirmationStatus === 'Confirmed' ? '!' : '✓'}</i></button>{reservation.status === 'Active' ? <button className="reservation-cancel-button" disabled={seatAction} onClick={() => onStatus('Cancelled')}>Cancel reservation</button> : <button className="reservation-reactivate-button" onClick={() => onStatus('Active')}>Reactivate reservation</button>}</section>
    <section className="reservation-danger-zone"><span>Permanent action</span><button disabled={saving} onClick={onDelete}>Delete reservation</button></section>
  </>
}
