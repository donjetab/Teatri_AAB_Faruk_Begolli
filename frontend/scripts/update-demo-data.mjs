import { readFile, writeFile } from 'node:fs/promises'

const apiBaseUrl = process.env.API_BASE_URL ?? 'http://127.0.0.1:5000'
const demoDataUrl = new URL('../public/demo-data.json', import.meta.url)
const languages = ['sq', 'en']

async function get(path) {
  const response = await fetch(`${apiBaseUrl}${path}`)
  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText}: ${path}`)
  }
  return response.json()
}

const data = JSON.parse(await readFile(demoDataUrl, 'utf8'))

for (const language of languages) {
  const [home, shows, news] = await Promise.all([
    get(`/api/${language}/home`),
    get(`/api/${language}/shows`),
    get(`/api/${language}/news`),
  ])

  const [showDetails, newsDetails] = await Promise.all([
    Promise.all(shows.map((show) =>
      get(`/api/${language}/shows/${encodeURIComponent(show.slug)}`)
        .then((detail) => [show.slug, detail]),
    )),
    Promise.all(news
      .filter((article) => !article.isExternal)
      .map((article) =>
        get(`/api/${language}/news/${encodeURIComponent(article.slug)}`)
          .then((detail) => [article.slug, detail]),
      )),
  ])

  data[language] = {
    home,
    shows,
    showDetails: Object.fromEntries(showDetails),
    news,
    newsDetails: Object.fromEntries(newsDetails),
    reserve: {
      activeShows: [
        {
          id: 1,
          title: 'Bretkosa',
          slug: 'bretkosa',
          date: '2026-09-10',
          venue: language === 'sq' ? 'Teatri Kombëtar' : 'National Theatre',
          time: '19:30',
          phone: '048 999 000',
          phoneUrl: 'tel:+38348999000',
          reservationUrl: '#',
          seatsAvailable: true,
        },
        {
          id: 2,
          title: 'Bretkosa',
          slug: 'bretkosa',
          date: '2026-09-17',
          venue: language === 'sq' ? 'Teatri Kamertal AAB' : 'AAB Chamber Theatre',
          time: '19:30',
          phone: '048 999 000',
          phoneUrl: 'tel:+38348999000',
          reservationUrl: '#',
          seatsAvailable: true,
        },
      ],
    },
  }
}

await writeFile(demoDataUrl, `${JSON.stringify(data, null, 2)}\n`, 'utf8')
console.log(`Updated fallback data for ${languages.join(', ')} from ${apiBaseUrl}.`)
