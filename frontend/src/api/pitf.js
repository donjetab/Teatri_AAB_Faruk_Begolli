import { apiClient } from './client'

export const getPitf = (language, signal) =>
  apiClient.get(`/api/${language}/pitf`, { signal }).then(response => response.data)
