import { apiClient } from './client'
import { getDemoHome, isCanceledRequest } from './demo'

export async function getHome(language, signal) {
  try {
    const response = await apiClient.get(`/api/${language}/home`, { signal })
    return response.data
  } catch (error) {
    if (isCanceledRequest(error)) {
      throw error
    }
    return getDemoHome(language, signal)
  }
}
