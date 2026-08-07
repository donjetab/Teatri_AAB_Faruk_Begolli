import test from 'node:test'
import assert from 'node:assert/strict'
import { canSelectPublicSeat, publicReservationUnavailableMessage } from '../src/utils/reservationRules.js'

test('paused reservations prevent public seat selection', () => {
  assert.equal(canSelectPublicSeat({ reservationsAvailable: false }, { state: 'Available' }, 0), false)
})

test('seat limit prevents another selection but allows deselection', () => {
  const layout = { reservationsAvailable: true, maxSeatsPerReservation: 2 }
  assert.equal(canSelectPublicSeat(layout, { state: 'Available' }, 2), false)
  assert.equal(canSelectPublicSeat(layout, { state: 'Available' }, 2, true), true)
})

test('configured unavailable message takes precedence', () => {
  assert.equal(publicReservationUnavailableMessage({ reservationsAvailable: false, unavailableMessage: 'Call the box office.' }, 'Closed'), 'Call the box office.')
})
