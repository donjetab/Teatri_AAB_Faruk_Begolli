import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { createReservation, getPerformanceSeats, holdPerformanceSeats, releasePerformanceHold } from '../api/performances'
import { SeatingSchema } from '../components/seating/SeatingSchema'
import { canSelectPublicSeat } from '../utils/reservationRules'

const countryCodes = ['+383', '+355', '+389', '+381', '+382', '+385', '+386', '+39', '+43', '+49', '+41', '+44', '+33', '+1', '+90']
const errorText = (error, fallback) => error.response?.data?.detail || fallback
const savedHold = key => {
  try {
    const value = JSON.parse(window.sessionStorage.getItem(key))
    if (!value?.hold?.holdToken || !Array.isArray(value.selected) || new Date(value.hold.expiresAt).getTime() <= Date.now()) return null
    return value
  } catch { return null }
}

export function InternalReservationPage() {
  const { t } = useTranslation()
  const { language = 'sq', performanceId } = useParams()
  const holdStorageKey = `reservation-hold:${language}:${performanceId}`
  const [layout, setLayout] = useState(null)
  const [selected, setSelected] = useState([])
  const [hold, setHold] = useState(null)
  const [form, setForm] = useState({ fullName: '', countryPrefix: '+383', phone: '', email: '' })
  const [errors, setErrors] = useState({})
  const [message, setMessage] = useState('')
  const [loading, setLoading] = useState(true)
  const [holding, setHolding] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [success, setSuccess] = useState(null)
  const holdTokenRef = useRef(null)
  const submittedRef = useRef(false)

  useLayoutEffect(() => {
    if (!success) return
    window.scrollTo({ top: 0, left: 0, behavior: 'auto' })
    document.documentElement.scrollTop = 0
    document.body.scrollTop = 0
  }, [success])

  const load = async (token = holdTokenRef.current) => {
    setLoading(true); setMessage('')
    try { setLayout(await getPerformanceSeats(language, performanceId, token)) }
    catch (error) { setLayout(null); setMessage(errorText(error, t('reservePage.internal.unavailable'))) }
    finally { setLoading(false) }
  }
  useEffect(() => {
    const abandoned = savedHold(holdStorageKey)
    window.sessionStorage.removeItem(holdStorageKey)
    holdTokenRef.current = null
    setHold(null)
    setSelected([])
    const reset = async () => {
      if (abandoned?.hold?.holdToken) {
        try { await releasePerformanceHold(language, abandoned.hold.holdToken) } catch { /* It will also expire automatically. */ }
      }
      await load(null)
    }
    void reset()
  }, [language, performanceId, holdStorageKey])
  useEffect(() => { holdTokenRef.current = hold?.holdToken ?? null }, [hold])
  useEffect(() => {
    if (hold?.holdToken && selected.length) window.sessionStorage.setItem(holdStorageKey, JSON.stringify({ hold, selected }))
    else window.sessionStorage.removeItem(holdStorageKey)
  }, [hold, selected, holdStorageKey])
  useEffect(() => {
    if (!hold?.expiresAt) return undefined
    const expire = () => { holdTokenRef.current = null; window.sessionStorage.removeItem(holdStorageKey); setHold(null); setSelected([]); setMessage(t('reservePage.internal.holdExpired')); void load(null) }
    const delay = Math.max(0, new Date(hold.expiresAt).getTime() - Date.now())
    const timer = window.setTimeout(expire, delay)
    return () => window.clearTimeout(timer)
  }, [hold?.expiresAt])

  const selectedSeats = useMemo(() => (layout?.seats ?? []).filter(seat => selected.includes(seat.id)).sort((a, b) => a.sectionOrder - b.sectionOrder || a.rowOrder - b.rowOrder || a.seatOrder - b.seatOrder), [layout, selected])
  const displaySeats = useMemo(() => (layout?.seats ?? []).map(seat => String(seat.section).trim().toUpperCase() === 'C' ? { ...seat, y: Number(seat.y) - 35 } : seat), [layout])
  const selectionLabel = seat => `${seat.section} · ${t('reservePage.internal.row')} ${seat.row} · ${t('reservePage.internal.seat')} ${seat.label}`
  const releaseHold = async () => {
    const token = holdTokenRef.current; holdTokenRef.current = null; window.sessionStorage.removeItem(holdStorageKey); setHold(null)
    if (token) try { await releasePerformanceHold(language, token) } catch { /* The hold expires automatically. */ }
  }
  const changeSelection = async seat => {
    if (holding || submitting || !canSelectPublicSeat(layout, seat, selected.length, selected.includes(seat.id))) return
    const next = selected.includes(seat.id) ? selected.filter(id => id !== seat.id) : [...selected, seat.id]
    if (layout.maxSeatsPerReservation && next.length > layout.maxSeatsPerReservation) { setMessage(`A maximum of ${layout.maxSeatsPerReservation} seats is allowed per reservation.`); return }
    setHolding(true); setMessage('')
    try {
      if (!next.length) { await releaseHold(); setSelected([]) }
      else { const nextHold = await holdPerformanceSeats(language, performanceId, next, holdTokenRef.current); holdTokenRef.current = nextHold.holdToken; setHold(nextHold); setSelected(next) }
    } catch (error) {
      await releaseHold(); setSelected([]); setMessage(errorText(error, t('reservePage.internal.seatConflict'))); await load(null)
    } finally { setHolding(false) }
  }
  const validate = () => {
    const next = {}; const name = form.fullName.trim(); const digits = form.phone.replace(/\D/g, ''); const prefix = form.countryPrefix.trim()
    if (name.length < 2) next.fullName = t('reservePage.internal.nameError')
    if (!/^\+\d{1,4}$/.test(prefix)) next.countryPrefix = t('reservePage.internal.prefixError')
    if (digits.length < 6 || digits.length > 14) next.phone = t('reservePage.internal.phoneError')
    if (form.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) next.email = t('reservePage.internal.emailError')
    if (!layout?.reservationsAvailable) next.seats = layout?.unavailableMessage || t('reservePage.internal.unavailable')
    else if (!selected.length) next.seats = t('reservePage.internal.seatsError')
    if (!holdTokenRef.current) next.seats = t('reservePage.internal.holdExpired')
    setErrors(next); return Object.keys(next).length === 0
  }
  const submit = async event => {
    event.preventDefault(); if (submittedRef.current || submitting || !validate()) return
    submittedRef.current = true; setSubmitting(true); setMessage('')
    const submittedSeats = selectedSeats.map(seat => ({ ...seat }))
    try {
      const result = await createReservation(language, performanceId, { ...form, fullName: form.fullName.trim(), email: form.email.trim() || null, seatIds: selected, holdToken: holdTokenRef.current })
      holdTokenRef.current = null; window.sessionStorage.removeItem(holdStorageKey); setHold(null); setSelected([]); setSuccess({ result, seats: submittedSeats, customer: { ...form } }); await load(null)
    } catch (error) {
      await releaseHold(); setSelected([]); setMessage(errorText(error, t('reservePage.internal.submitError'))); await load(null); submittedRef.current = false
    } finally { setSubmitting(false) }
  }
  const backPath = `/${language}/${language === 'sq' ? 'rezervo' : 'reserve'}`

  if (success && layout) return <article className="internal-reservation-page"><section className="reservation-success" role="status"><span className="reservation-success-mark">✓</span><p className="reservation-eyebrow">{t('reservePage.internal.successEyebrow')}</p><h1>{t('reservePage.internal.successTitle')}</h1><p>{t('reservePage.internal.successText')}</p><div className="reservation-success-details"><div><span>{t('reservePage.internal.play')}</span><strong>{layout.showTitle}</strong></div><div><span>{t('reservePage.internal.performance')}</span><strong>{new Intl.DateTimeFormat(language === 'sq' ? 'sq-AL' : 'en-GB', { dateStyle: 'full', timeStyle: 'short' }).format(new Date(layout.startsAt))}</strong></div><div><span>{t('reservePage.internal.venue')}</span><strong>{layout.venue}</strong></div><div><span>{t('reservePage.internal.reservedSeats')}</span><strong>{success.seats.map(selectionLabel).join(', ')}</strong></div><div><span>{t('reservePage.internal.customer')}</span><strong>{success.customer.fullName} · {success.customer.countryPrefix} {success.customer.phone}{success.customer.email ? ` · ${success.customer.email}` : ''}</strong></div></div><p className="reservation-confirmation-note">{t('reservePage.internal.confirmationNote')}</p><Link className="reserve-seat-button" to={backPath}>{t('reservePage.internal.backToPerformances')}</Link></section></article>

  return <article className="internal-reservation-page">
    <header className="internal-reservation-header"><Link to={backPath} onClick={() => void releaseHold()}>← {t('reservePage.internal.back')}</Link><p className="reservation-eyebrow">{t('reservePage.internal.eyebrow')}</p><h1>{layout?.showTitle ?? t('reservePage.internal.title')}</h1>{layout && <div className="internal-performance-meta"><span>{new Intl.DateTimeFormat(language === 'sq' ? 'sq-AL' : 'en-GB', { dateStyle: 'full', timeStyle: 'short' }).format(new Date(layout.startsAt))}</span><span>{layout.venue}</span></div>}</header>
    {loading && !layout ? <section className="reservation-public-state" aria-live="polite"><i /><i /><i /><p>{t('states.loading')}</p></section>
      : !layout ? <section className="reservation-public-state error"><h2>{t('reservePage.internal.unavailableTitle')}</h2><p>{message || t('reservePage.internal.unavailable')}</p><button onClick={() => load(null)}>{t('reservePage.internal.tryAgain')}</button></section>
        : <><div className="internal-reservation-grid">
          <section className="public-seating-panel" aria-labelledby="public-seating-title"><header><div><p className="reservation-eyebrow">{t('reservePage.internal.chooseSeats')}</p><h2 id="public-seating-title">{t('reservePage.internal.seating')}</h2></div></header><div className="public-seat-legend"><span className="available">{t('reservePage.internal.available')}</span><span className="selected">{t('reservePage.internal.selected')}</span><span className="unavailable">{t('reservePage.internal.unavailableSeat')}</span><span className="disabled">{t('reservePage.internal.disabled')}</span></div>{displaySeats.length ? <><div className={`public-schema-scroll${holding ? ' is-busy' : ''}`}><SeatingSchema schema={layout} seats={displaySeats} selectedIds={selected} showNumbers={false} onSeatClick={changeSelection} /></div><p className="schema-scroll-hint">{t('reservePage.internal.scrollHint')}</p></> : <div className="public-schema-empty">{t('reservePage.internal.noLayoutSeats')}</div>}</section>
          <aside className="public-reservation-sidebar"><section className="selected-seat-summary"><header><div><p className="reservation-eyebrow">{t('reservePage.internal.selection')}</p><h2>{t('reservePage.internal.selectedCount', { count: selectedSeats.length })}</h2></div>{selectedSeats.length > 0 && <button type="button" onClick={async () => { await releaseHold(); setSelected([]) }}>{t('reservePage.internal.clear')}</button>}</header>{selectedSeats.length ? <ul>{selectedSeats.map(seat => <li key={seat.id}><span>{selectionLabel(seat)}</span><button type="button" onClick={() => changeSelection(seat)} aria-label={`${t('reservePage.internal.remove')} ${selectionLabel(seat)}`}>×</button></li>)}</ul> : <p>{t('reservePage.internal.noSeats')}</p>}{errors.seats && <small className="reservation-field-error">{errors.seats}</small>}</section>
            <form className="public-reservation-form" onSubmit={submit} noValidate><p className="reservation-eyebrow">{t('reservePage.internal.yourDetails')}</p><h2>{t('reservePage.internal.formTitle')}</h2><Field label={t('reservePage.internal.fullName')} error={errors.fullName}><input autoComplete="name" value={form.fullName} onChange={event => setForm({ ...form, fullName: event.target.value })} /></Field><div className="public-phone-row"><Field label={t('reservePage.internal.countryCode')} error={errors.countryPrefix}><select value={form.countryPrefix} onChange={event => setForm({ ...form, countryPrefix: event.target.value })}>{countryCodes.map(code => <option key={code}>{code}</option>)}</select></Field><Field label={t('reservePage.internal.phone')} error={errors.phone}><input inputMode="tel" autoComplete="tel-national" placeholder="44 123 456" value={form.phone} onChange={event => setForm({ ...form, phone: event.target.value })} /></Field></div><p className="phone-format-help">{t('reservePage.internal.phoneHelp')}</p><Field label={t('reservePage.internal.email')} error={errors.email}><input type="email" autoComplete="email" value={form.email} onChange={event => setForm({ ...form, email: event.target.value })} /></Field>{message && <div className="public-reservation-message" role="alert">{message}</div>}<button className="reserve-seat-button public-submit-reservation" disabled={submitting || holding || !selected.length}>{submitting ? t('reservePage.internal.submitting') : t('reservePage.internal.submit')}</button><small className="reservation-submit-note">{t('reservePage.internal.submitNote')}</small></form>
          </aside>
        </div>{!layout.reservationsAvailable && <div className="public-reservation-message reservation-rules-message" role="status">{layout.unavailableMessage || t('reservePage.internal.unavailable')}</div>}</>}
  </article>
}

function Field({ label, error, children }) {
  return <label className={error ? 'has-error' : ''}><span>{label}</span>{children}{error && <small className="reservation-field-error">{error}</small>}</label>
}
