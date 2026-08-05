import { apiClient } from './client'

export const getNavigation = signal => apiClient.get('/api/navigation', { signal }).then(response => response.data)
