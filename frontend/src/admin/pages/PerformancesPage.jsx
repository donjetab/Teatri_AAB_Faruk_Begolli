import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { adminApi } from '../api'
import { LoadingSkeleton, PageHeader, StatusBadge, Toast } from '../components/AdminUi'
import { useAdminDialog } from '../components/AdminDialog'

const empty = { playKind: 'ours', showId: '', locationId: '', venueChoice: '', hall: '', newVenueNameSq: '', newVenueNameEn: '', newVenueAddressSq: '', newVenueAddressEn: '', startDateTimeUtc: '', ticketUrl: '', contactPhone: '', reservationMode: 'ExternalUrl', seatingTemplateId: '', status: 'Scheduled', isPublished: false, internalNotes: '', maxSeatsPerReservation: '', reservationOpensAtUtc: '', reservationClosesAtUtc: '', reservationsEnabled: true, reservationUnavailableMessage: '' }
const localValue = value => {
  if (!value) return ''
  const date = new Date(value)
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16)
}
const currentLocalMinute = () => {
  const now = new Date()
  return new Date(now.getTime() - now.getTimezoneOffset() * 60000).toISOString().slice(0, 16)
}
const formatAdminDateTime = value => new Intl.DateTimeFormat('en-GB', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
}).format(new Date(value))
const payload = form => ({
  ...form,
  showId: Number(form.showId),
  locationId: form.venueChoice && form.venueChoice !== 'add' ? Number(form.venueChoice) : null,
  hall: null,
  startDateTimeUtc: new Date(form.startDateTimeUtc).toISOString(),
  endDateTimeUtc: null,
  ticketUrl: form.ticketUrl?.trim() || null,
  contactPhone: form.contactPhone?.trim() || null,
  seatingTemplateId: form.reservationMode === 'Internal' ? Number(form.seatingTemplateId) : null,
  maxSeatsPerReservation: null,
  reservationOpensAtUtc: form.reservationOpensAtUtc ? new Date(form.reservationOpensAtUtc).toISOString() : null,
  reservationClosesAtUtc: form.reservationClosesAtUtc ? new Date(form.reservationClosesAtUtc).toISOString() : null,
})

export function PerformancesPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const dialog = useAdminDialog()
  const [data, setData] = useState(null)
  const [filters, setFilters] = useState({ showId: '', locationId: '', status: '' })
  const [view, setView] = useState('table')
  const [calendarCursor, setCalendarCursor] = useState(() => new Date())
  const [editing, setEditing] = useState(null)
  const [form, setForm] = useState(empty)
  const [toast, setToast] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [savingVenue, setSavingVenue] = useState(false)
  const [scheduleConflicts, setScheduleConflicts] = useState([])
  const [bookingError, setBookingError] = useState('')
  const [dateError, setDateError] = useState('')
  const [venueManagerOpen, setVenueManagerOpen] = useState(false)
  const [venueList, setVenueList] = useState([])
  const [venueEdit, setVenueEdit] = useState(null)
  const [venueError, setVenueError] = useState('')
  const [seatingTemplates, setSeatingTemplates] = useState([])

  const load = () => {
    setLoading(true)
    setError('')
    return adminApi.performances(Object.fromEntries(Object.entries(filters).filter(([, value]) => value)))
      .then(setData)
      .catch(response => {
        setData(null)
        setError(response.response?.data?.detail || 'Performance data could not be loaded. Restart the backend so its latest migration and API are active.')
      })
      .finally(() => setLoading(false))
  }

  useEffect(() => { void load() }, [filters.showId, filters.locationId, filters.status])
  useEffect(() => { adminApi.seatingTemplates().then(setSeatingTemplates).catch(() => setSeatingTemplates([])) }, [])
  useEffect(() => {
    if (!editing || !form.startDateTimeUtc || !form.venueChoice || form.venueChoice === 'add') {
      setScheduleConflicts([])
      return
    }
    let active = true
    const timer = setTimeout(() => {
      adminApi.performanceConflicts({
        startDateTimeUtc: new Date(form.startDateTimeUtc).toISOString(),
        locationId: Number(form.venueChoice),
        excludeId: editing === 'new' ? undefined : editing,
      }).then(items => { if (active) setScheduleConflicts(items) })
        .catch(() => { if (active) setScheduleConflicts([]) })
    }, 250)
    return () => { active = false; clearTimeout(timer) }
  }, [editing, form.startDateTimeUtc, form.venueChoice, form.hall])

  const openNew = () => { setScheduleConflicts([]); setBookingError(''); setDateError(''); setEditing('new'); setForm(empty) }
  const openEdit = item => {
    setEditing(item.id)
    setScheduleConflicts([])
    setBookingError('')
    setDateError('')
    setForm({
      ...item,
      playKind: data?.shows?.find(show => show.id === item.showId)?.isGuestPerformance ? 'guest' : 'ours',
      locationId: item.locationId ?? '',
      venueChoice: item.locationId ? String(item.locationId) : '',
      startDateTimeUtc: localValue(item.startDateTimeUtc),
      hall: item.hall ?? '',
      ticketUrl: item.ticketUrl ?? '',
      contactPhone: item.contactPhone ?? '',
      internalNotes: item.internalNotes ?? '',
      reservationMode: item.reservationMode ?? 'ExternalUrl',
      seatingTemplateId: item.seatingTemplateId ? String(item.seatingTemplateId) : '',
      maxSeatsPerReservation: item.maxSeatsPerReservation ? String(item.maxSeatsPerReservation) : '',
      reservationOpensAtUtc: localValue(item.reservationOpensAtUtc),
      reservationClosesAtUtc: localValue(item.reservationClosesAtUtc),
      reservationsEnabled: item.reservationsEnabled ?? true,
      reservationUnavailableMessage: item.reservationUnavailableMessage ?? '',
    })
  }
  useEffect(() => {
    const requestedId = Number(searchParams.get('performance'))
    if (!requestedId || editing) return
    adminApi.performance(requestedId)
      .then(item => { openEdit(item); setSearchParams({}, { replace: true }) })
      .catch(() => { setToast('The selected performance could not be found.'); setSearchParams({}, { replace: true }) })
  }, [searchParams, editing])
  const save = async event => {
    event.preventDefault()
    const originalStart = editing === 'new' ? null : data?.items.find(item => item.id === editing)?.startDateTimeUtc
    const startChanged = !originalStart || new Date(form.startDateTimeUtc).getTime() !== new Date(originalStart).getTime()
    if (startChanged && new Date(form.startDateTimeUtc) < new Date()) {
      setDateError('Choose the current time or a future date. A performance cannot be moved into the past.')
      return
    }
    setDateError('')
    const bookingIsOptional = ['SoldOut', 'Completed', 'Cancelled'].includes(form.status) || new Date(form.startDateTimeUtc) < new Date()
    if (!bookingIsOptional && form.reservationMode === 'ExternalUrl' && !form.ticketUrl.trim() && !form.contactPhone.trim()) {
      setBookingError('Add either a reservation link or a contact phone number.')
      return
    }
    if (form.reservationMode === 'Internal' && !form.seatingTemplateId) { setBookingError('Select a seating template for internal reservations.'); return }
    setBookingError('')
    let performanceForm = form
    if (form.venueChoice === 'add') {
      const venue = await addVenue()
      if (!venue) return
      performanceForm = { ...form, venueChoice: String(venue.id), locationId: venue.id, hall: '' }
    }
    if (editing === 'new') await adminApi.createPerformance(payload(performanceForm))
    else await adminApi.savePerformance(editing, payload(performanceForm))
    setEditing(null)
    setToast('Performance saved.')
    load()
  }
  const addVenue = async () => {
    if (!form.newVenueNameSq.trim() || !form.newVenueNameEn.trim() || !form.newVenueAddressSq.trim() || !form.newVenueAddressEn.trim()) return null
    setSavingVenue(true)
    try {
      const venue = await adminApi.createVenue({ nameSq: form.newVenueNameSq, nameEn: form.newVenueNameEn, addressSq: form.newVenueAddressSq, addressEn: form.newVenueAddressEn })
      setData(current => ({ ...current, locations: [...current.locations, venue] }))
      setForm(current => ({ ...current, venueChoice: String(venue.id), locationId: venue.id, newVenueNameSq: '', newVenueNameEn: '', newVenueAddressSq: '', newVenueAddressEn: '', hall: '' }))
      setToast('Venue added and selected.')
      return venue
    } finally {
      setSavingVenue(false)
    }
  }
  const openVenueManager = async () => {
    setVenueError('')
    setVenueEdit(null)
    setVenueManagerOpen(true)
    setVenueList(await adminApi.venues())
  }
  const saveVenue = async () => {
    if (!venueEdit || !venueEdit.nameSq.trim() || !venueEdit.nameEn.trim() || !venueEdit.addressSq.trim() || !venueEdit.addressEn.trim()) {
      setVenueError('Complete both language names and addresses.')
      return
    }
    try {
      const savedVenue = await adminApi.saveVenue(venueEdit.id, venueEdit)
      setVenueList(current => current.map(item => item.id === savedVenue.id ? savedVenue : item))
      setData(current => ({ ...current, locations: current.locations.map(item => item.id === savedVenue.id ? { ...item, label: savedVenue.nameSq } : item) }))
      setVenueEdit(null)
      setVenueError('')
      setToast('Venue updated.')
    } catch (error) {
      setVenueError(error.response?.data?.detail ?? error.response?.data?.title ?? 'The venue could not be updated.')
    }
  }
  const removeVenue = async venue => {
    if (!await dialog.confirm({ title: 'Delete venue?', message: `“${venue.nameSq}” will be permanently removed from the venue list.`, confirmLabel: 'Delete venue', danger: true })) return
    try {
      await adminApi.deleteVenue(venue.id)
      setVenueList(current => current.filter(item => item.id !== venue.id))
      setData(current => ({ ...current, locations: current.locations.filter(item => item.id !== venue.id) }))
      setVenueEdit(current => current?.id === venue.id ? null : current)
      setToast('Venue deleted.')
    } catch (error) {
      setVenueError(error.response?.data?.detail ?? error.response?.data?.title ?? 'The venue could not be deleted.')
    }
  }
  const duplicate = async id => { await adminApi.duplicatePerformance(id); setToast('Performance duplicated one week later as a draft.'); load() }
  const remove = async item => {
    if (!await dialog.confirm({ title: 'Delete performance and all associated data?', message: `The performance for “${item.showTitle}” on ${formatAdminDateTime(item.startDateTimeUtc)}, all reservations, seat allocations and seating history will be permanently removed. This cannot be undone.`, confirmLabel: 'Delete everything', danger: true })) return
    try { await adminApi.deletePerformance(item.id); setToast('Performance and associated reservation data deleted.'); load() }
    catch (requestError) { setError(requestError.response?.data?.detail ?? requestError.response?.data?.title ?? 'The performance could not be deleted.') }
  }
  const calendar = useMemo(() => {
    const year = calendarCursor.getFullYear()
    const month = calendarCursor.getMonth()
    const firstDay = new Date(year, month, 1)
    const mondayOffset = (firstDay.getDay() + 6) % 7
    const gridStart = new Date(year, month, 1 - mondayOffset)
    const events = new Map()
    for (const item of data?.items ?? []) {
      const date = new Date(item.startDateTimeUtc)
      const key = `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`
      events.set(key, [...(events.get(key) ?? []), item])
    }
    const cells = Array.from({ length: 42 }, (_, index) => {
      const date = new Date(gridStart)
      date.setDate(gridStart.getDate() + index)
      const key = `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`
      return { date, events: events.get(key) ?? [], isCurrentMonth: date.getMonth() === month }
    })
    return { year, month, cells }
  }, [calendarCursor, data])
  const moveCalendar = direction => setCalendarCursor(current => new Date(current.getFullYear(), current.getMonth() + direction, 1))
  const performancePlayOptions = (data?.shows ?? []).filter(item => form.playKind === 'guest' ? item.isGuestPerformance : !item.isGuestPerformance)

  return <>
    <PageHeader eyebrow="Content" title="Performances" description="Schedule and publish performance dates without reservation or seat calculations." actions={<><button className="admin-outline-button" onClick={openVenueManager}>Manage venues</button><button className="admin-primary-button" onClick={openNew}>Add performance</button></>} />
    <section className="admin-panel">
      <div className="performance-toolbar">
        <div className="show-filters">
          <label><span>Play</span><select value={filters.showId} onChange={event => setFilters({ ...filters, showId: event.target.value })}><option value="">All plays</option>{data?.shows?.map(item => <option value={item.id} key={item.id}>{item.label}</option>)}</select></label>
          <label><span>Venue</span><select value={filters.locationId} onChange={event => setFilters({ ...filters, locationId: event.target.value })}><option value="">All venues</option>{data?.locations?.map(item => <option value={item.id} key={item.id}>{item.label}</option>)}</select></label>
          <label><span>Status</span><select value={filters.status} onChange={event => setFilters({ ...filters, status: event.target.value })}><option value="">All statuses</option><option>Scheduled</option><option>SoldOut</option><option>Postponed</option><option>Cancelled</option><option>Completed</option></select></label>
        </div>
        <div className="view-switch"><button className={view === 'table' ? 'active' : ''} onClick={() => setView('table')}>Table</button><button className={view === 'calendar' ? 'active' : ''} onClick={() => setView('calendar')}>Calendar</button></div>
      </div>
    </section>
    {error ? <section className="admin-panel admin-request-error" role="alert"><div>!</div><h2>Performances are unavailable</h2><p>{error}</p><button className="admin-primary-button" onClick={load}>Try again</button></section>
      : loading ? <LoadingSkeleton />
        : !data ? null
          : view === 'table' ? <section className="admin-panel"><div className="admin-table-wrap"><table className="admin-table"><thead><tr><th>Play</th><th>Type</th><th>Date & time</th><th>Venue</th><th>Availability</th><th>Publication</th><th /></tr></thead><tbody>{data.items.map(item => <tr className={item.isGuestPerformance ? 'guest-performance-row' : ''} key={item.id}><td><strong>{item.showTitle}</strong></td><td><span className={`performance-type-chip ${item.isGuestPerformance ? 'guest' : 'ours'}`}>{item.isGuestPerformance ? 'Guest play' : 'Our production'}</span></td><td>{formatAdminDateTime(item.startDateTimeUtc)}</td><td>{[item.venue, item.hall].filter(Boolean).join(' · ') || '—'}</td><td><StatusBadge status={item.status} /></td><td><StatusBadge status={item.isPublished ? 'Published' : 'Draft'} /></td><td><div className="table-actions"><button onClick={() => openEdit(item)}>Edit</button><button onClick={() => duplicate(item.id)}>Duplicate</button><button className="danger" onClick={() => remove(item)}>Delete</button></div></td></tr>)}</tbody></table></div></section>
            : <section className="performance-month-calendar">
              <header className="calendar-month-toolbar">
                <div><span>Performance calendar</span><h2>{new Date(calendar.year, calendar.month, 1).toLocaleDateString(undefined, { month: 'long', year: 'numeric' })}</h2></div>
                <div><label className="calendar-month-picker"><span className="sr-only">Choose month</span><input type="month" value={`${calendar.year}-${String(calendar.month + 1).padStart(2, '0')}`} onChange={event => { const [year, month] = event.target.value.split('-').map(Number); if (year && month) setCalendarCursor(new Date(year, month - 1, 1)) }} /></label><button type="button" onClick={() => moveCalendar(-1)} aria-label="Previous month">←</button><button type="button" className="calendar-today" onClick={() => setCalendarCursor(new Date())}>Today</button><button type="button" onClick={() => moveCalendar(1)} aria-label="Next month">→</button></div>
              </header>
              <div className="calendar-weekdays">{['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'].map(day => <span key={day}>{day}</span>)}</div>
              <div className="calendar-month-grid">{calendar.cells.map(({ date, events, isCurrentMonth }) => {
                const today = new Date()
                const isToday = date.toDateString() === today.toDateString()
                return <article className={`${isCurrentMonth ? '' : 'outside-month'}${isToday ? ' today' : ''}`} key={date.toISOString()}>
                  <header><time dateTime={date.toISOString().slice(0, 10)}>{date.getDate()}</time>{events.length > 0 && <span>{events.length}</span>}</header>
                  <div>{events.map(item => <button type="button" className={`${item.isGuestPerformance ? 'guest-performance ' : ''}${item.status === 'SoldOut' ? 'sold-out' : item.status === 'Postponed' ? 'postponed' : item.status === 'Cancelled' ? 'cancelled' : item.status === 'Completed' ? 'completed' : ''}`} onClick={() => openEdit(item)} key={item.id}><time>{new Date(item.startDateTimeUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</time><strong>{item.showTitle}</strong><small>{item.isGuestPerformance ? 'Guest play · ' : ''}{item.status === 'Postponed' ? 'Postponed · new date pending' : item.status === 'Cancelled' ? 'Cancelled · original date' : item.venue ?? item.hall ?? 'Venue not set'}</small></button>)}</div>
                </article>
              })}</div>
            </section>}
    {editing && <div className="admin-modal-backdrop" onMouseDown={event => event.target === event.currentTarget && setEditing(null)}>
      <section className="admin-modal" role="dialog" aria-modal="true">
        <div className="admin-modal-heading"><div><span>Performance</span><h2>{editing === 'new' ? 'Add performance' : 'Edit performance'}</h2></div><button onClick={() => setEditing(null)}>×</button></div>
        {dateError && <div className="performance-booking-error" role="alert"><div>!</div><span><strong>Invalid performance date</strong><small>{dateError}</small></span></div>}
        {bookingError && <div className="performance-booking-error" role="alert"><div>!</div><span><strong>Booking information required</strong><small>{bookingError}</small></span></div>}
        {scheduleConflicts.length > 0 && <div className="performance-conflict-warning" role="alert"><div>!</div><span><strong>Possible scheduling conflict</strong>{scheduleConflicts.map(item => <small key={item.id}>{item.showTitle} is scheduled at {new Date(item.startDateTimeUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} in {[item.venue, item.hall].filter(Boolean).join(' · ')}.</small>)}</span></div>}
        <form className="admin-form" onSubmit={save}>
          <div className="form-grid">
            <div className="reservation-mode-switch full" aria-label="Play type"><button type="button" className={form.playKind === 'ours' ? 'active' : ''} onClick={() => setForm({ ...form, playKind: 'ours', showId: '' })}>Our plays</button><button type="button" className={form.playKind === 'guest' ? 'active' : ''} onClick={() => setForm({ ...form, playKind: 'guest', showId: '' })}>Guest plays</button></div>
            <label>Play *<select required value={form.showId} onChange={event => setForm({ ...form, showId: event.target.value })}><option value="">Select {form.playKind === 'guest' ? 'guest play' : 'our play'}</option>{performancePlayOptions.map(item => <option value={item.id} key={item.id}>{item.label}</option>)}</select></label>
            <label>Venue<select value={form.venueChoice} onChange={event => setForm({ ...form, venueChoice: event.target.value, locationId: event.target.value === 'add' ? '' : event.target.value, seatingTemplateId: '', hall: '', newVenueNameSq: '', newVenueNameEn: '', newVenueAddressSq: '', newVenueAddressEn: '' })}><option value="">No venue</option>{data.locations.map(item => <option value={item.id} key={item.id}>{item.label}</option>)}<option value="add">+ Add a reusable venue</option></select></label>
            <label>Start (DD/MM/YYYY) *<input required type="datetime-local" lang="en-GB" min={editing === 'new' || new Date(data?.items.find(item => item.id === editing)?.startDateTimeUtc ?? 0) >= new Date() ? currentLocalMinute() : undefined} value={form.startDateTimeUtc} onChange={event => { setForm({ ...form, startDateTimeUtc: event.target.value }); setDateError('') }} /></label>
            {form.venueChoice === 'add' && <div className="add-venue-fields full"><label>Venue name — Albanian *<input required value={form.newVenueNameSq} onChange={event => setForm({ ...form, newVenueNameSq: event.target.value })} placeholder="Teatri Kombëtar i Kosovës" /></label><label>Venue name — English *<input required value={form.newVenueNameEn} onChange={event => setForm({ ...form, newVenueNameEn: event.target.value })} placeholder="National Theatre of Kosovo" /></label><label>Address — Albanian *<input required value={form.newVenueAddressSq} onChange={event => setForm({ ...form, newVenueAddressSq: event.target.value })} placeholder="Prishtinë, Kosovë" /></label><label>Address — English *<input required value={form.newVenueAddressEn} onChange={event => setForm({ ...form, newVenueAddressEn: event.target.value })} placeholder="Pristina, Kosovo" /></label><button type="button" className="admin-outline-button" disabled={savingVenue || !form.newVenueNameSq.trim() || !form.newVenueNameEn.trim() || !form.newVenueAddressSq.trim() || !form.newVenueAddressEn.trim()} onClick={addVenue}>{savingVenue ? 'Adding…' : 'Add venue to dropdown'}</button></div>}
            <label>Status<select value={form.status} onChange={event => { const status = event.target.value; setForm({ ...form, status, isPublished: status === 'Postponed' || status === 'Cancelled' ? true : form.isPublished }); if (status === 'SoldOut') setBookingError('') }}><option>Scheduled</option><option>SoldOut</option><option>Postponed</option><option>Cancelled</option><option>Completed</option></select></label>
            <label>Reservation method<select value={form.reservationMode} onChange={event => setForm({ ...form, reservationMode: event.target.value })}><option value="ExternalUrl">Reservation link or phone</option><option value="Internal">Internal seating reservation</option></select></label>
            {form.reservationMode === 'Internal' && <label>Seating template<select required value={form.seatingTemplateId} onChange={event => setForm({ ...form, seatingTemplateId: event.target.value })}><option value="">Select template</option>{seatingTemplates.filter(x => x.isActive && (!form.venueChoice || x.locationId === Number(form.venueChoice))).map(x => <option key={x.id} value={x.id}>{x.venue} · {x.name}</option>)}</select></label>}
            {form.reservationMode === 'Internal' && <><label>Reservations open<input type="datetime-local" value={form.reservationOpensAtUtc} onChange={event => setForm({ ...form, reservationOpensAtUtc: event.target.value })} /></label><label>Reservations close<input type="datetime-local" value={form.reservationClosesAtUtc} onChange={event => setForm({ ...form, reservationClosesAtUtc: event.target.value })} /></label><label className="admin-switch-row"><input type="checkbox" checked={form.reservationsEnabled} onChange={event => setForm({ ...form, reservationsEnabled: event.target.checked })} /> Reservations enabled</label><label className="full">Public unavailable message<textarea rows="2" maxLength="500" value={form.reservationUnavailableMessage} onChange={event => setForm({ ...form, reservationUnavailableMessage: event.target.value })} placeholder="Optional message shown while reservations are unavailable" /></label></>}
            {form.status !== 'SoldOut' && form.reservationMode === 'ExternalUrl' && <><label>Reservation link (optional)<input type="url" value={form.ticketUrl} onChange={event => { setForm({ ...form, ticketUrl: event.target.value }); if (event.target.value.trim() || form.contactPhone.trim()) setBookingError('') }} placeholder="https://…" /></label><label>Reservation phone (optional)<input type="tel" value={form.contactPhone} onChange={event => { setForm({ ...form, contactPhone: event.target.value }); if (event.target.value.trim() || form.ticketUrl.trim()) setBookingError('') }} placeholder="+383…" /></label><p className="full admin-field-help">Provide at least one: a reservation link or a phone number.</p></>}
            {form.status !== 'SoldOut' && <label>Contact phone<input value={form.contactPhone} onChange={event => setForm({ ...form, contactPhone: event.target.value })} placeholder="Optional contact number" /></label>}
            {form.status === 'SoldOut' && <div className="performance-sold-out-note full"><strong>Sold out</strong><span>Booking links and phone controls will be hidden on the public website.</span></div>}
            <label className="full">Internal notes<textarea rows="4" value={form.internalNotes} onChange={event => setForm({ ...form, internalNotes: event.target.value })} /></label>
            <label className="admin-switch-row full"><input type="checkbox" checked={form.isPublished} onChange={event => setForm({ ...form, isPublished: event.target.checked })} /> Published on the website</label>
          </div>
          <div className="admin-modal-actions"><button type="button" className="admin-text-button" onClick={() => setEditing(null)}>Cancel</button><button className="admin-primary-button">Save performance</button></div>
        </form>
      </section>
    </div>}
    {venueManagerOpen && <div className="admin-modal-backdrop" onMouseDown={event => event.target === event.currentTarget && setVenueManagerOpen(false)}>
      <section className="admin-modal venue-manager-modal" role="dialog" aria-modal="true">
        <div className="admin-modal-heading"><div><span>Website venues</span><h2>Manage venues</h2></div><button onClick={() => setVenueManagerOpen(false)}>×</button></div>
        {venueError && <div className="admin-form-error">{venueError}</div>}
        <div className="venue-manager-list">{venueList.map(venue => <article key={venue.id}>
          {venueEdit?.id === venue.id ? <div className="venue-manager-edit"><label>Albanian name<input value={venueEdit.nameSq} onChange={event => setVenueEdit({ ...venueEdit, nameSq: event.target.value })} /></label><label>English name<input value={venueEdit.nameEn} onChange={event => setVenueEdit({ ...venueEdit, nameEn: event.target.value })} /></label><label>Albanian address<input value={venueEdit.addressSq} onChange={event => setVenueEdit({ ...venueEdit, addressSq: event.target.value })} /></label><label>English address<input value={venueEdit.addressEn} onChange={event => setVenueEdit({ ...venueEdit, addressEn: event.target.value })} /></label><div><button className="admin-text-button" onClick={() => setVenueEdit(null)}>Cancel</button><button className="admin-primary-button" onClick={saveVenue}>Save venue</button></div></div>
            : <><div><strong>{venue.nameSq}</strong><span>{venue.addressSq}</span><small>{venue.nameEn} · {venue.addressEn}</small></div><span className="venue-use-count">{venue.performanceCount} performance{venue.performanceCount === 1 ? '' : 's'}</span><div className="table-actions"><button onClick={() => { setVenueError(''); setVenueEdit({ ...venue }) }}>Edit</button><button className="danger" onClick={() => removeVenue(venue)}>Delete</button></div></>}
        </article>)}</div>
        {!venueList.length && <div className="admin-empty"><strong>No venues</strong><p>Add the first venue from the Add Performance form.</p></div>}
      </section>
    </div>}
    <Toast message={toast} onClose={() => setToast('')} />
  </>
}
