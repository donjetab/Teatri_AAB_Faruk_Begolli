import { apiClient } from './client'

export function getUpcomingPerformances(language, signal) {
  return apiClient.get(`/api/${language}/performances`, { signal }).then(response => response.data)
}
