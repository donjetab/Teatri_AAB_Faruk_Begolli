export const canSelectPublicSeat = (layout, seat, selectedCount, isSelected = false) => {
  if (!layout?.reservationsAvailable || seat?.state !== 'Available') return false
  return isSelected || !layout.maxSeatsPerReservation || selectedCount < layout.maxSeatsPerReservation
}

export const publicReservationUnavailableMessage = (layout, fallback) =>
  layout?.reservationsAvailable === false ? (layout.unavailableMessage || fallback) : ''
