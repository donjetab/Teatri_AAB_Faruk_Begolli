import { apiClient } from './client'

export async function getStaticPage(language, pageKey, signal) {
  const response = await apiClient.get(`/api/${language}/pages/${pageKey}`, { signal })
  return response.data
}
