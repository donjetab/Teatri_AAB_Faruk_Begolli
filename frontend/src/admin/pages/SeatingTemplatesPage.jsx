import { useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import { adminApi } from '../api'
import { PageHeader, Toast } from '../components/AdminUi'
import { SeatingSchema, flattenTemplateSeats } from '../../components/seating/SeatingSchema'

const copy = value => structuredClone(value)
const applyFixedOrientations = value => {
  const item = copy(value)
  const sectionNames = new Set((item?.sections ?? []).map(section => section.name.toUpperCase()))
  if (sectionNames.has('MAIN') && sectionNames.has('UPPER')) {
    item.canvasWidth = 1350
    item.canvasHeight = 1000
    item.stageLabel = 'STAGE'
    item.stageX = 230
    item.stageY = 920
    item.stageWidth = 890
    item.stageHeight = 54
    item.sections = item.sections.map(section => ({
      ...section,
      rows: section.rows.map(row => {
        const rowIndex = Math.max(0, row.label.toUpperCase().charCodeAt(0) - 65)
        const ordered = [...row.seats].sort((a, b) => Number(a.displayOrder) - Number(b.displayOrder))
        return {
          ...row,
          seats: row.seats.map(seat => {
            const index = ordered.indexOf(seat)
            if (section.name.toUpperCase() === 'UPPER') {
              const number = index + 1
              return { ...seat, x: number <= 9 ? 330 + (number - 1) * 46 : 800 + (number - 10) * 46, y: rowIndex === 11 ? 100 : 165, rotation: 0 }
            }
            const count = ordered.length
            const span = (count - 1) * 44
            const centered = count === 1 ? 0 : index / (count - 1) * 2 - 1
            return { ...seat, x: (1350 - span) / 2 + index * 44, y: 820 - rowIndex * 57 + 48 * centered * centered, rotation: centered * 10 }
          }),
        }
      }),
    }))
    return item
  }
  if (!sectionNames.has('A') || !sectionNames.has('C')) return item
  item.stageY = 285
  item.stageHeight = 350
  item.sections = item.sections.map(section => {
    const sectionName = section.name.toUpperCase()
    return {
      ...section,
      rows: section.rows.map(row => {
        const maximum = sectionName === 'A' ? { A1: 10, A2: 21, A3: 32 }[row.label.toUpperCase()] : null
        const ordered = maximum ? [...row.seats].sort((a, b) => Number(a.y) - Number(b.y)) : []
        const verticallyOrdered = sectionName === 'C' ? [...row.seats].sort((a, b) => Number(a.displayOrder) - Number(b.displayOrder)) : []
        const fixedX = sectionName === 'A' ? { A1: 220, A2: 158, A3: 96 }[row.label.toUpperCase()] : sectionName === 'C' ? { C1: 980, C2: 1042, C3: 1104 }[row.label.toUpperCase()] : null
        const fixedY = sectionName === 'B' ? { B1: 190, B2: 138, B3: 86 }[row.label.toUpperCase()] : null
        const verticalBase = sectionName === 'C' ? { C1: 285, C2: 250, C3: 250 }[row.label.toUpperCase()] : null
        return {
          ...row,
          seats: row.seats.map(seat => ({
            ...seat,
            label: seat.label,
            x: fixedX ?? seat.x,
            y: verticalBase != null ? verticalBase + verticallyOrdered.indexOf(seat) * 38 : fixedY ?? seat.y,
            rotation: sectionName === 'A' ? -90 : sectionName === 'C' ? 90 : seat.rotation,
          })),
        }
      }),
    }
  })
  return item
}

export function SeatingTemplatesPage() {
  const { performanceId } = useParams()
  const performanceMode = Boolean(performanceId)
  const [templates, setTemplates] = useState([])
  const [draft, setDraft] = useState(null)
  const [selectedId, setSelectedId] = useState(null)
  const [publicPreview, setPublicPreview] = useState(false)
  const [toast, setToast] = useState('')
  const seats = useMemo(() => flattenTemplateSeats(draft).map(x => ({ ...x, clientId: x.clientId ?? x.id ?? `${x.section}-${x.row}-${x.displayOrder}` })), [draft])
  const selected = seats.find(x => x.clientId === selectedId)

  const load = async preferred => {
    if (performanceMode) { const item = await adminApi.performanceLayout(performanceId); setTemplates([item]); setDraft(applyFixedOrientations(item)); setSelectedId(null); return }
    const items = await adminApi.seatingTemplates(); setTemplates(items)
    const item = items.find(x => x.id === preferred) ?? items[0]
    setDraft(item ? applyFixedOrientations(item) : null); setSelectedId(null)
  }
  useEffect(() => { void load() }, [])
  const key = (seat, section, row) => seat.clientId ?? seat.id ?? `${section.name}-${row.label}-${seat.displayOrder}`
  const changeSeat = (target, changes) => setDraft(current => ({ ...current, sections: current.sections.map(section => ({ ...section, rows: section.rows.map(row => ({ ...row, seats: row.seats.map(seat => key(seat, section, row) === target.clientId ? { ...seat, ...changes } : seat) })) })) }))
  const moveSeat = (target, sectionName, rowLabel) => setDraft(current => {
    let moved
    const sections = current.sections.map(section => ({ ...section, rows: section.rows.map(row => ({ ...row, seats: row.seats.filter(seat => { if (key(seat, section, row) === target.clientId) { moved = seat; return false } return true }) })) }))
    return { ...current, sections: sections.map(section => section.name === sectionName ? { ...section, rows: section.rows.map(row => row.label === rowLabel ? { ...row, seats: [...row.seats, moved] } : row) } : section) }
  })
  const save = async () => { const saved = performanceMode ? await adminApi.savePerformanceLayout(performanceId, draft) : await adminApi.saveSeatingTemplate(draft.id, draft); await load(saved.id); setToast(performanceMode ? 'Performance seating layout and seat names saved to the database.' : 'Seating template and seat names saved to the database. Future performances will use this version.') }
  const reset = async () => { if (!window.confirm('Reset this theatre to the saved reference layout?')) return; const saved = await adminApi.resetSeatingTemplate(draft.id); await load(saved.id); setToast('Reference layout restored.') }
  const renameSection = (index, name) => setDraft(current => ({ ...current, sections: current.sections.map((section, i) => i === index ? { ...section, name } : section) }))
  const renameRow = (sectionIndex, rowIndex, label) => setDraft(current => ({ ...current, sections: current.sections.map((section, i) => i === sectionIndex ? { ...section, rows: section.rows.map((row, j) => j === rowIndex ? { ...row, label } : row) } : section) }))

  return <>
    <PageHeader eyebrow="Reservations" title={performanceMode ? 'Performance seating layout' : 'Theatre seating templates'} description={performanceMode ? 'Changes apply only to this performance.' : 'Edit reusable defaults. Existing performance layouts are not changed.'} actions={<><button className="admin-outline-button" onClick={() => { setPublicPreview(value => !value); setSelectedId(null) }} disabled={!draft}>{publicPreview ? 'Edit layout' : 'Front view'}</button><button className="admin-primary-button" onClick={save} disabled={!draft || publicPreview}>Save layout</button></>} />
    <section className="admin-panel seating-editor-toolbar">
      {!performanceMode && <label>Theatre<select value={draft?.id ?? ''} onChange={e => { setDraft(applyFixedOrientations(templates.find(x => x.id === Number(e.target.value)))); setSelectedId(null) }}>{templates.map(x => <option key={x.id} value={x.id}>{x.venue}</option>)}</select></label>}
    </section>
    {draft && <div className={`seating-editor-grid${publicPreview ? ' is-public-preview' : ''}`}>
      <section className={`admin-panel seating-editor-canvas${publicPreview ? ' public-schema-scroll' : ''}`}><SeatingSchema schema={draft} seats={seats} editor={!publicPreview} selectedIds={selectedId == null ? [] : [selectedId]} onSeatClick={publicPreview ? undefined : seat => setSelectedId(seat.clientId)} /></section>
      {!publicPreview && <aside className="admin-panel seating-inspector"><h2>{selected ? `Seat ${selected.label}` : 'Sections and rows'}</h2>
        {selected ? <>
          <label>Number<input value={selected.label} onChange={e => changeSeat(selected, { label: e.target.value })} /></label>
          <label>Section<select value={selected.section} onChange={e => { const section = draft.sections.find(x => x.name === e.target.value); moveSeat(selected, section.name, section.rows[0].label) }}>{draft.sections.map(x => <option key={x.name}>{x.name}</option>)}</select></label>
          <label>Row<select value={selected.row} onChange={e => moveSeat(selected, selected.section, e.target.value)}>{draft.sections.find(x => x.name === selected.section)?.rows.map(x => <option key={x.label}>{x.label}</option>)}</select></label>
          <label><input type="checkbox" checked={selected.isActive} onChange={e => changeSeat(selected, { isActive: e.target.checked })} /> Seat enabled</label>
          <button className="admin-primary-button seating-inspector-save" type="button" onClick={save}>Save seat changes</button>
        </> : draft.sections.map((section, si) => <div className="seating-structure" key={si}><label>Section<input value={section.name} onChange={e => renameSection(si, e.target.value)} /></label>{section.rows.map((row, ri) => <label key={ri}>Row<input value={row.label} onChange={e => renameRow(si, ri, e.target.value)} /></label>)}</div>)}
      </aside>}
    </div>}
    {toast && <Toast message={toast} onClose={() => setToast('')} />}
  </>
}
