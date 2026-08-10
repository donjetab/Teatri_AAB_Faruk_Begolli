import { apiClient } from './client'
import { getDemoPitf, isCanceledRequest } from './demo'

export async function getPitf(language, signal) {
  try {
    const response = await apiClient.get(`/api/${language}/pitf`, { signal })
    return response.data
  } catch (error) {
    if (isCanceledRequest(error)) throw error
    return getDemoPitf(language, signal)
  }
}
