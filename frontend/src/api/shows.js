import { apiClient } from './client'
import { getDemoShow, getDemoShows, isCanceledRequest } from './demo'

export async function getShows(language, signal) {
  try {
    const response = await apiClient.get(`/api/${language}/shows`, { signal })
    return response.data
  } catch (error) {
    if (isCanceledRequest(error)) {
      throw error
    }
    return getDemoShows(language, signal)
  }
}

export async function getShow(language, slug, signal) {
  try {
    const response = await apiClient.get(`/api/${language}/shows/${slug}`, { signal })
    return response.data
  } catch (error) {
    if (isCanceledRequest(error)) {
      throw error
    }
    return getDemoShow(language, slug, signal)
  }
}
