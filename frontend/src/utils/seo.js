export function upsertMeta(selector, attributes) {
  let element = document.head.querySelector(selector)
  if (!element) {
    element = document.createElement('meta')
    document.head.appendChild(element)
  }
  Object.entries(attributes).forEach(([key, value]) => element.setAttribute(key, value))
}

export function setPageMetadata({ title, description, image, type = 'website', noIndex = false }) {
  document.title = title
  upsertMeta('meta[name="description"]', { name: 'description', content: description })
  upsertMeta('meta[name="robots"]', { name: 'robots', content: noIndex ? 'noindex, nofollow' : 'index, follow, max-image-preview:large' })
  upsertMeta('meta[property="og:title"]', { property: 'og:title', content: title })
  upsertMeta('meta[property="og:description"]', { property: 'og:description', content: description })
  upsertMeta('meta[property="og:type"]', { property: 'og:type', content: type })
  upsertMeta('meta[property="og:url"]', { property: 'og:url', content: window.location.href.split('#')[0] + window.location.hash })
  upsertMeta('meta[name="twitter:card"]', { name: 'twitter:card', content: image ? 'summary_large_image' : 'summary' })
  upsertMeta('meta[name="twitter:title"]', { name: 'twitter:title', content: title })
  upsertMeta('meta[name="twitter:description"]', { name: 'twitter:description', content: description })
  let canonical = document.head.querySelector('link[rel="canonical"]')
  if (!canonical) {
    canonical = document.createElement('link')
    canonical.rel = 'canonical'
    document.head.appendChild(canonical)
  }
  canonical.href = `${window.location.origin}${window.location.pathname}`
  if (image) {
    const absoluteImage = new URL(image, window.location.origin).href
    upsertMeta('meta[property="og:image"]', { property: 'og:image', content: absoluteImage })
    upsertMeta('meta[name="twitter:image"]', { name: 'twitter:image', content: absoluteImage })
  }
}

export function setStructuredData(id, value) {
  let script = document.head.querySelector(`script[data-seo-id="${id}"]`)
  if (!script) {
    script = document.createElement('script')
    script.type = 'application/ld+json'
    script.dataset.seoId = id
    document.head.appendChild(script)
  }
  script.textContent = JSON.stringify(value)
  return () => script.remove()
}
