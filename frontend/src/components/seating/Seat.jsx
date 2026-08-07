import availableSeatSvg from '../../../../seat.svg?raw'
import selectedSeatSvg from '../../../../red-border-seat.svg?raw'
import reservedSeatSvg from '../../../../reserved-seat.svg?raw'
import adminConfirmedSeatSvg from '../../../../admin-confirmed-seat.svg?raw'

const stateName = value => String(value || 'available').replace(/\s+/g, '-').toLowerCase()

const reservedStates = new Set([
  'reserved',
  'unavailable',
  'unconfirmed',
  'adminblocked',
  'admin-blocked',
])

const seatArtwork = (state, selected) => {
  if (selected) return selectedSeatSvg
  if (state === 'confirmed') return adminConfirmedSeatSvg
  if (reservedStates.has(state)) return reservedSeatSvg
  return availableSeatSvg
}

export function Seat({
  seat,
  state = 'available',
  selected = false,
  editor = false,
  size,
  colors,
  showNumber = true,
  onSelect,
  onPointerDown,
}) {
  const normalizedState = stateName(state)
  const artwork = seatArtwork(normalizedState, selected)
  const scale = Number(size ?? seat.size ?? 40) / 40
  const enabled = seat.isActive !== false
  const interactive = editor || (enabled && (normalizedState === 'available' || selected))
  const activate = event => {
    event.stopPropagation()
    if (interactive) onSelect?.(seat)
  }
  const keyDown = event => {
    if (event.key !== 'Enter' && event.key !== ' ') return
    event.preventDefault()
    activate(event)
  }
  const style = {
    '--seat-fill': colors?.fill ?? seat.fillColor,
    '--seat-accent': colors?.accent ?? seat.accentColor,
    '--seat-stroke': colors?.stroke ?? seat.strokeColor,
    '--seat-number': colors?.number ?? seat.numberColor,
  }

  return <g
    className={`schema-seat schema-seat--${normalizedState}${selected ? ' is-selected' : ''}${enabled ? '' : ' is-disabled'}`}
    transform={`translate(${seat.x} ${seat.y}) rotate(${seat.rotation || 0}) scale(${scale})`}
    style={style}
    onClick={activate}
    onKeyDown={keyDown}
    onPointerDown={event => onPointerDown?.(event, seat)}
    role="button"
    aria-label={`Section ${seat.section}, row ${seat.row}, seat ${seat.label}, ${normalizedState}`}
    aria-pressed={selected}
    aria-disabled={!interactive}
    tabIndex={interactive ? 0 : -1}
  >
    <rect className="schema-seat__hit-area" x="-21" y="-22" width="42" height="54" rx="5" />
    <g
      className="schema-seat__variant-art"
      transform="translate(-15.5 -20)"
      aria-hidden="true"
      dangerouslySetInnerHTML={{ __html: artwork }}
    />
    <foreignObject className="schema-seat__public-chair" x="-17" y="-20" width="34" height="42" aria-hidden="true">
      <span className="schema-seat__public-chair-shape" />
    </foreignObject>
    {showNumber && <text className="schema-seat__number" y="29" textAnchor="middle">{seat.label}</text>}
  </g>
}
