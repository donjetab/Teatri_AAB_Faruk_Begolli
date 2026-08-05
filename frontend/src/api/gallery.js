import { apiClient } from './client'

export function getGalleryImages(language, signal) {
  return apiClient.get(`/api/${language}/gallery`, { signal }).then(response => response.data)
}
