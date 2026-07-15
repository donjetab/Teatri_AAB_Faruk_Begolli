import { apiClient } from './client'

export async function getHome(language, signal) {
  const response = await apiClient.get(`/api/${language}/home`, { signal })
  return response.data
}
