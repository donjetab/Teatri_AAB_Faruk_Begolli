import { useEffect, useState } from 'react'
import { adminApi } from '../api'
import { LoadingSkeleton } from './AdminUi'

const blankCredit = type => ({ id: null, personId: null, personName: '', creditTypeId: type?.id ?? '', creditTypeCode: type?.code ?? '', roleSq: type?.labelSq ?? '', roleEn: type?.labelEn ?? '', characterName: '', displayOrder: 0 })

export function CreditRepeater({ showId, value, onChange, onSaved }) {
  const controlled = Array.isArray(value)
  const [localCredits, setLocalCredits] = useState(controlled ? value : null)
  const [types, setTypes] = useState([])
  const [saving, setSaving] = useState(false)
  const credits = controlled ? value : localCredits
  const setCredits = updater => {
    const next = typeof updater === 'function' ? updater(credits) : updater
    if (controlled) onChange?.(next)
    else setLocalCredits(next)
  }

  useEffect(() => {
    if (showId) adminApi.showCredits(showId).then(data => { setLocalCredits(data.credits); setTypes(data.creditTypes) })
    else adminApi.showCreditTypes().then(setTypes)
  }, [showId])

  if (!credits) return <LoadingSkeleton rows={3} />
  const update = (index, key, value) => setCredits(current => current.map((item, i) => i === index ? { ...item, [key]: value } : item))
  const typeChanged = (index, id) => {
    const type = types.find(x => x.id === Number(id))
    if (!type) return
    setCredits(current => current.map((item, i) => i === index ? { ...item, creditTypeId: type.id, creditTypeCode: type.code, roleSq: type.labelSq, roleEn: type.labelEn } : item))
  }
  const move = (index, direction) => {
    const target = index + direction
    if (target < 0 || target >= credits.length) return
    setCredits(current => { const copy = [...current]; [copy[index], copy[target]] = [copy[target], copy[index]]; return copy })
  }
  const save = async () => {
    setSaving(true)
    try {
      const data = await adminApi.saveShowCredits(showId, credits.map((x, i) => ({ ...x, displayOrder: i, creditTypeId: Number(x.creditTypeId) })))
      setLocalCredits(data.credits); onSaved?.()
    } finally { setSaving(false) }
  }

  return <div className="credit-repeater">
    <div className="credit-repeater-heading"><div><h2>Production credits</h2><p>Keep cast and crew structured, translated and in the order shown publicly.</p></div><button className="admin-outline-button" type="button" disabled={!types.length} onClick={() => setCredits(current => [...current, blankCredit(types[0])])}>Add credit</button></div>
    {!credits.length && <div className="admin-empty"><strong>No credits yet</strong><p>Add the director, playwright, cast and production crew.</p></div>}
    {credits.map((credit, index) => <article className="credit-row" key={`${credit.id ?? 'new'}-${index}`}>
      <div className="credit-order"><button type="button" disabled={index === 0} onClick={() => move(index, -1)} aria-label="Move up">↑</button><span>{index + 1}</span><button type="button" disabled={index === credits.length - 1} onClick={() => move(index, 1)} aria-label="Move down">↓</button></div>
      <div className="credit-fields">
        <label>Person<input required value={credit.personName} onChange={e => update(index, 'personName', e.target.value)} placeholder="Full name" /></label>
        <label>Credit type<select required value={credit.creditTypeId} onChange={e => typeChanged(index, e.target.value)}>{types.map(type => <option value={type.id} key={type.id}>{type.labelSq} / {type.labelEn}</option>)}</select></label>
        <label>Role in Albanian<input value={credit.roleSq} onChange={e => update(index, 'roleSq', e.target.value)} /></label>
        <label>Role in English<input value={credit.roleEn} onChange={e => update(index, 'roleEn', e.target.value)} /></label>
        <label className="full">Character or additional detail<input value={credit.characterName ?? ''} onChange={e => update(index, 'characterName', e.target.value)} placeholder="Optional character name" /></label>
      </div>
      <button className="credit-remove" type="button" onClick={() => setCredits(current => current.filter((_, i) => i !== index))} aria-label={`Remove ${credit.personName || 'credit'}`}>×</button>
    </article>)}
    {!controlled && <div className="credit-save"><button className="admin-primary-button" type="button" disabled={saving} onClick={save}>{saving ? 'Saving…' : 'Save credits'}</button></div>}
  </div>
}
