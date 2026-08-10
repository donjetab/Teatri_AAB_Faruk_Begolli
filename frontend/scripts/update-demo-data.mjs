import { mkdir, readFile, writeFile } from 'node:fs/promises'
import { dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

const apiBaseUrl = process.env.API_BASE_URL ?? 'http://127.0.0.1:5000'
const demoDataUrl = new URL('../public/demo-data.json', import.meta.url)
const languages = ['sq', 'en']

async function get(path) {
  const response = await fetch(`${apiBaseUrl}${path}`)
  if (!response.ok) throw new Error(`${response.status} ${response.statusText}: ${path}`)
  return response.json()
}

async function saveMedia(value) {
  if (Array.isArray(value)) return Promise.all(value.map(saveMedia))
  if (value && typeof value === 'object') {
    return Object.fromEntries(await Promise.all(Object.entries(value).map(async ([key, item]) => [key, await saveMedia(item)])))
  }
  if (typeof value !== 'string' || !/^\/?uploads\//i.test(value)) return value

  const apiPath = `/${value.replace(/^\/+/, '')}`
  const publicPath = `/demo-media${apiPath}`
  const diskUrl = new URL(`../public${publicPath}`, import.meta.url)
  const diskPath = fileURLToPath(diskUrl)
  const response = await fetch(`${apiBaseUrl}${apiPath}`)
  if (!response.ok) {
    console.warn(`Skipped missing media: ${apiPath}`)
    return value
  }
  await mkdir(dirname(diskPath), { recursive: true })
  await writeFile(diskPath, Buffer.from(await response.arrayBuffer()))
  return publicPath
}

const data = JSON.parse(await readFile(demoDataUrl, 'utf8'))

for (const language of languages) {
  const [home, shows, news, performances] = await Promise.all([
    get(`/api/${language}/home`),
    get(`/api/${language}/shows`),
    get(`/api/${language}/news`),
    get(`/api/${language}/performances`),
  ])
  const [showDetails, newsDetails, seatEntries] = await Promise.all([
    Promise.all(shows.map(show => get(`/api/${language}/shows/${encodeURIComponent(show.slug)}`).then(detail => [show.slug, detail]))),
    Promise.all(news.filter(article => !article.isExternal).map(article => get(`/api/${language}/news/${encodeURIComponent(article.slug)}`).then(detail => [article.slug, detail]))),
    Promise.all(performances.filter(item => item.reservationMode === 'Internal').map(item => get(`/api/${language}/reservations/performances/${item.id}/seats`).then(layout => [item.id, layout]))),
  ])

  data[language] = await saveMedia({
    home,
    shows,
    showDetails: Object.fromEntries(showDetails),
    news,
    newsDetails: Object.fromEntries(newsDetails),
    performances,
    performanceSeats: Object.fromEntries(seatEntries),
  })
}

await writeFile(demoDataUrl, `${JSON.stringify(data, null, 2)}\n`, 'utf8')
console.log(`Updated fallback content, media, schedule, and seating for ${languages.join(', ')} from ${apiBaseUrl}.`)
