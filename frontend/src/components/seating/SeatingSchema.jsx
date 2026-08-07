import { Seat } from './Seat'

export const flattenTemplateSeats = template => (template?.sections ?? []).flatMap(section => section.rows.flatMap(row => row.seats.map(seat => ({ ...seat, section: section.name, row: row.label, sectionOrder: section.displayOrder, rowOrder: row.displayOrder }))))

export function SeatingSchema({ schema, seats, selectedIds = [], editor = false, showNumbers = true, zoom = 1, pan = { x: 0, y: 0 }, onSeatClick, onSeatPointerDown }) {
  if (!schema) return null
  const showRowLetters = (schema.sections ?? []).some(section => section.name?.toUpperCase() === 'MAIN')
  const labelledRows = showRowLetters ? (schema.sections ?? []).flatMap(section => section.rows.map(row => {
    const rowSeats = seats.filter(seat => seat.section === section.name && seat.row === row.label)
    if (!rowSeats.length) return null
    const left = rowSeats.reduce((current, seat) => Number(seat.x) < Number(current.x) ? seat : current)
    const right = rowSeats.reduce((current, seat) => Number(seat.x) > Number(current.x) ? seat : current)
    return { label: row.label, leftX: Number(left.x) - 34, leftY: Number(left.y) + 7, rightX: Number(right.x) + 34, rightY: Number(right.y) + 7 }
  }).filter(Boolean)) : []
  return <div className="seating-schema-shell"><svg className="seating-schema" viewBox={`0 0 ${schema.canvasWidth} ${schema.canvasHeight}`} preserveAspectRatio="xMidYMid meet" aria-label={schema.name || schema.showTitle}>
    <g transform={`translate(${pan.x} ${pan.y}) scale(${zoom})`}>
      {schema.stageWidth && <g className="schema-stage"><rect x={schema.stageX} y={schema.stageY} width={schema.stageWidth} height={schema.stageHeight} rx="8" /><text x={Number(schema.stageX) + Number(schema.stageWidth) / 2} y={Number(schema.stageY) + Number(schema.stageHeight) / 2} textAnchor="middle">{schema.stageLabel || 'STAGE'}</text></g>}
      {labelledRows.map(row => <g className="schema-row-label" key={row.label}><text x={row.leftX} y={row.leftY} textAnchor="middle">{row.label}</text><text x={row.rightX} y={row.rightY} textAnchor="middle">{row.label}</text></g>)}
      {seats.map(seat => <Seat key={seat.clientId ?? seat.id} seat={seat} state={seat.state?.toLowerCase() || 'available'} selected={selectedIds.includes(seat.clientId ?? seat.id)} editor={editor} showNumber={showNumbers} size={seat.size} colors={seat.colors} onSelect={onSeatClick} onPointerDown={onSeatPointerDown} />)}
    </g>
  </svg></div>
}
