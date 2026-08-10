import axios from 'axios'
import heroImage from '../assets/hero.png'
import aboutImage from '../assets/teatri/AAB.jpg'
import reservationImage from '../assets/cta-background.png'

export const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000'

export const apiClient = axios.create({
  baseURL: apiBaseUrl,
  withCredentials: true,
  headers: {
    Accept: 'application/json',
  },
})

const bundledMediaByApiPath = {
  '/uploads/dev/homepage/hero-theatre-hall.png': heroImage,
  '/uploads/dev/homepage/about-preview-per-ne.jpg': aboutImage,
  '/uploads/dev/homepage/reservation-banner.png': reservationImage,
}

export function resolveMediaUrl(url) {
  if (!url) {
    return ''
  }

  if (/^https?:\/\//i.test(url)) {
    return url
  }

  if (/^(?:data:|blob:)/i.test(url)) {
    return url
  }

  if (bundledMediaByApiPath[url]) {
    return bundledMediaByApiPath[url]
  }

  if (/^\/demo-media\//i.test(url)) {
    return `${import.meta.env.BASE_URL}${url.slice(1)}`
  }

  if (import.meta.env.PROD && !import.meta.env.VITE_API_BASE_URL && /^\/?uploads\//i.test(url)) {
    return `${import.meta.env.BASE_URL}demo-media/${url.replace(/^\/+/, '')}`
  }

  // Only files under /uploads are served by ASP.NET. Every other relative
  // path is a Vite/public asset and must stay on the frontend host, including
  // paths prefixed by the configured /Teatri_AAB_Faruk_Begolli/ base.
  if (!/^\/?uploads\//i.test(url)) {
    return url
  }

  return `${apiBaseUrl}${url.startsWith('/') ? url : `/${url}`}`
}
