import { apiClient } from './client'

export function getUpcomingPerformances(language, signal) {
  return apiClient.get(`/api/${language}/performances`, { signal }).then(response => response.data)
}

export const getPerformanceSeats = (language, performanceId, holdToken, signal) => apiClient.get(`/api/${language}/reservations/performances/${performanceId}/seats`, { signal, params: holdToken ? { holdToken } : undefined }).then(r => r.data)
export const holdPerformanceSeats = (language, performanceId, seatIds, holdToken) => apiClient.post(`/api/${language}/reservations/performances/${performanceId}/holds`, { seatIds, holdToken }).then(r => r.data)
export const releasePerformanceHold = (language, holdToken) => apiClient.delete(`/api/${language}/reservations/holds/${holdToken}`)
export const createReservation = (language, performanceId, data) => apiClient.post(`/api/${language}/reservations/performances/${performanceId}`, data).then(r => r.data)
