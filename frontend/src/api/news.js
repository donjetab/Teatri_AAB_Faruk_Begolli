import { apiClient } from './client'
import {
  getDemoNews,
  getDemoNewsArticle,
  isCanceledRequest,
} from './demo'

export async function getNews(language, signal) {
  try {
    const response = await apiClient.get(`/api/${language}/news`, { signal })
    return response.data
  } catch (error) {
    if (isCanceledRequest(error)) {
      throw error
    }
    return getDemoNews(language, signal)
  }
}

export async function getNewsArticle(language, slug, signal) {
  try {
    const response = await apiClient.get(`/api/${language}/news/${slug}`, { signal })
    return response.data
  } catch (error) {
    if (isCanceledRequest(error)) {
      throw error
    }
    return getDemoNewsArticle(language, slug, signal)
  }
}
