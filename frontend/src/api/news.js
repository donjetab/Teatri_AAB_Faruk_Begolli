import { apiClient } from './client'

export async function getNews(language, signal) {
  const response = await apiClient.get(`/api/${language}/news`, { signal })
  return response.data
}

export async function getNewsArticle(language, slug, signal) {
  const response = await apiClient.get(`/api/${language}/news/${slug}`, { signal })
  return response.data
}
