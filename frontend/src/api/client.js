import axios from 'axios'
import heroImage from '../assets/hero.png'
import aboutImage from '../assets/teatri/AAB.jpg'
import reservationImage from '../assets/cta-background.png'
import { postersBySlug } from '../assets/shows/showAssets'

export const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000'

export const apiClient = axios.create({
  baseURL: apiBaseUrl,
  headers: {
    Accept: 'application/json',
  },
})

const bundledMediaByApiPath = {
  '/uploads/dev/homepage/hero-theatre-hall.png': heroImage,
  '/uploads/dev/homepage/about-preview-per-ne.jpg': aboutImage,
  '/uploads/dev/homepage/reservation-banner.png': reservationImage,
  '/uploads/dev/shows/bretkosa-poster.png': postersBySlug.bretkosa,
  '/uploads/dev/shows/qeni-i-baskervileve-poster.jpg': postersBySlug['qeni-i-baskervileve'],
  '/uploads/dev/shows/gjirafa-dhe-buburreci-poster.jpg': postersBySlug['gjirafa-dhe-buburreci'],
  '/uploads/dev/shows/rrena-poster.jpg': postersBySlug.rrena,
}

export function resolveMediaUrl(url) {
  if (!url) {
    return ''
  }

  if (/^https?:\/\//i.test(url)) {
    return url
  }

  if (bundledMediaByApiPath[url]) {
    return bundledMediaByApiPath[url]
  }

  return `${apiBaseUrl}${url.startsWith('/') ? url : `/${url}`}`
}
