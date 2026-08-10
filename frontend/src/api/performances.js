import { apiClient } from './client'
import { getDemoPerformances, getDemoPerformanceSeats, isCanceledRequest } from './demo'

export async function getUpcomingPerformances(language, signal) {
  try {
    const response = await apiClient.get(`/api/${language}/performances`, { signal })
    return response.data
  } catch (error) {
    if (isCanceledRequest(error)) throw error
    return getDemoPerformances(language, signal)
  }
}

export const getPerformanceSeats = async (language, performanceId, holdToken, signal) => {
  try {
    const response = await apiClient.get(`/api/${language}/reservations/performances/${performanceId}/seats`, { signal, params: holdToken ? { holdToken } : undefined })
    return response.data
  } catch (error) {
    if (isCanceledRequest(error)) throw error
    const fallback = await getDemoPerformanceSeats(language, performanceId, signal)
    if (!fallback) throw error
    return fallback
  }
}
export const holdPerformanceSeats = (language, performanceId, seatIds, holdToken) => apiClient.post(`/api/${language}/reservations/performances/${performanceId}/holds`, { seatIds, holdToken }).then(r => r.data)
export const releasePerformanceHold = (language, holdToken) => apiClient.delete(`/api/${language}/reservations/holds/${holdToken}`)
export const createReservation = (language, performanceId, data) => apiClient.post(`/api/${language}/reservations/performances/${performanceId}`, data).then(r => r.data)
