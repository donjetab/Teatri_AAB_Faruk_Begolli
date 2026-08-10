import { apiClient } from './client'
import { getDemoGallery, isCanceledRequest } from './demo'

export async function getGalleryImages(language, signal) {
  try {
    const response = await apiClient.get(`/api/${language}/gallery`, { signal })
    return response.data
  } catch (error) {
    if (isCanceledRequest(error)) throw error
    return getDemoGallery(language, signal)
  }
}
