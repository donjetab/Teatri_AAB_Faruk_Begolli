import galleryOne from '../assets/teatri/AAB-THEATER-CHAIRS.jpg'
import galleryTwo from '../assets/teatri/AAB-THEATER-FARUK-BEGOLLI.jpg'
import galleryThree from '../assets/teatri/AAB-THEATER-SCENE.jpg'
import galleryFour from '../assets/teatri/AAB-THEATER-VIEW-FROM-SCENE.jpg'
import galleryFive from '../assets/teatri/THEATER-AAB.jpg'
import gallerySix from '../assets/teatri/CHAIRS.jpg'

let demoDataPromise

const fallbackShowPosters = {
  'mersieri-dhe-kamieri': '/demo-media/uploads/shows/mersieri-dhe-kamieri/mersier%20dhe%20kamieri%20-%20poster.png',
  tjetri: '/demo-media/uploads/dev/news/2024-03-10-tjetri-ne-teatrin-aab-faruk-begolli/10.03.2024.jpg',
  brinat: '/demo-media/uploads/shows/brinat/brinat%20-%20poster.png',
  'hana-dhe-dielli': '/demo-media/uploads/shows/hana-dhe-dielli/hana%20dhe%20dielli%20-%20poster.png',
  'per-caj-te-hillary': '/demo-media/uploads/shows/per-caj-te-hillary/per%20caj%20te%20hillary%20-%20poster.jpg',
  'dy-gjitare-enderrimtare': '/demo-media/uploads/shows/dy-gjitare-enderrimtare/dy-gjitare-enderrimtare%20-%20poster.png',
  'profesor-jam-talent': '/demo-media/uploads/shows/profesor-jam-talent/profesor%20jam%20talent%20-%20poster.png',
  'venera-ne-gezof': '/demo-media/uploads/shows/venera-ne-gezof/venera%20ne%20gezof%20-%20poster.png',
}

const withFallbackPoster = show => show && ({ ...show, posterUrl: show.posterUrl || fallbackShowPosters[show.slug] || null })

function loadDemoData(signal) {
  demoDataPromise ??= fetch(`${import.meta.env.BASE_URL}demo-data.json`, { signal })
    .then((response) => {
      if (!response.ok) {
        throw new Error('Demo data is unavailable.')
      }
      return response.json()
    })
    .catch((error) => {
      demoDataPromise = undefined
      throw error
    })

  return demoDataPromise
}

export async function getDemoHome(language, signal) {
  const data = await loadDemoData(signal)
  return data[language]?.home ?? data.sq.home
}

export async function getDemoShows(language, signal) {
  const data = await loadDemoData(signal)
  const shows = data[language]?.shows ?? data.sq.shows

  return (Array.isArray(shows) ? shows : shows.value).map(withFallbackPoster)
}

export async function getDemoShow(language, slug, signal) {
  const data = await loadDemoData(signal)
  return withFallbackPoster(data[language]?.showDetails?.[slug] ?? data.sq.showDetails[slug])
}

export async function getDemoNews(language, signal) {
  const data = await loadDemoData(signal)
  return data[language]?.news ?? data.sq.news
}

export async function getDemoNewsArticle(language, slug, signal) {
  const data = await loadDemoData(signal)
  return data[language]?.newsDetails?.[slug] ?? data.sq.newsDetails[slug]
}

export async function getDemoReserve(language, signal) {
  const data = await loadDemoData(signal)
  return data[language]?.reserve ?? data.sq.reserve
}

export async function getDemoPerformances(language, signal) {
  const data = await loadDemoData(signal)
  const localized = data[language] ?? data.sq
  if (localized.performances) return localized.performances.map(performance => ({
    ...performance,
    posterUrl: performance.posterUrl || fallbackShowPosters[performance.showSlug] || null,
  }))
  return (localized.reserve?.activeShows ?? []).map((show) => ({
    id: show.id,
    showId: show.id,
    showTitle: show.title,
    showSlug: show.slug,
    posterUrl: localized.shows?.find(item => item.slug === show.slug)?.posterUrl || fallbackShowPosters[show.slug] || null,
    startDateTimeUtc: `${show.date}T${show.time}:00+02:00`,
    venue: show.venue,
    venueAddress: null,
    hall: null,
    status: 'Scheduled',
    ticketUrl: null,
    contactPhone: show.phone,
    reservationMode: 'Internal',
    internalReservationUrl: `/${language}/${language === 'sq' ? 'rezervo' : 'reserve'}/${show.id}`,
  }))
}

const fallbackGalleryImages = [
  galleryOne,
  galleryTwo,
  galleryThree,
  galleryFour,
  galleryFive,
  gallerySix,
].map((fileUrl, index) => ({
  id: index + 1,
  fileUrl,
  altText: index === 0 ? 'Teatri AAB Faruk Begolli' : `Teatri AAB Faruk Begolli, pamja ${index + 1}`,
}))

export async function getDemoGallery(language, signal) {
  await loadDemoData(signal)
  return fallbackGalleryImages.map(image => ({
    ...image,
    altText: language === 'en'
      ? `AAB Faruk Begolli Theatre, view ${image.id}`
      : image.altText,
  }))
}

export async function getDemoPitf(language, signal) {
  await loadDemoData(signal)
  const english = language === 'en'
  return {
    title: 'PITF',
    description: english
      ? 'Prishtina International Theatre Festival was founded at AAB Faruk Begolli Theatre in June 2017. The festival brings artists and audiences together through original international theatre.\n\nEach edition continues the theatre’s commitment to cultural exchange and new stage experiences.'
      : 'Festivali Ndërkombëtar i Teatrit “Prishtina International Theatre Festival” është themeluar në ambientet e Teatrit AAB “Faruk Begolli” në qershor të vitit 2017. Festivali bashkon artistë dhe publik përmes teatrit ndërkombëtar dhe krijimtarisë origjinale.\n\nÇdo edicion vazhdon përkushtimin e teatrit ndaj shkëmbimit kulturor dhe përvojave të reja skenike.',
    imageUrl: '/demo-media/uploads/dev/pitf/pitf-2024-cover.jpg',
    buttonText: null,
    buttonUrl: null,
    editions: Array.from({ length: 10 }, (_, index) => {
      const editionNumber = 10 - index
      const year = 2016 + editionNumber
      return {
        id: editionNumber,
        editionNumber,
        year,
        name: english ? `PITF ${year}` : `PITF ${year}`,
        imageUrl: `/demo-media/uploads/pitf-editions/${editionNumber}.png`,
        destinationUrl: null,
      }
    }),
  }
}

export async function getDemoPerformanceSeats(language, performanceId, signal) {
  const data = await loadDemoData(signal)
  const snapshot = data[language]?.performanceSeats?.[performanceId]
    ?? data.sq.performanceSeats?.[performanceId]
  if (snapshot) return snapshot
  const performance = (await getDemoPerformances(language, signal)).find(item => String(item.id) === String(performanceId))
  if (!performance) return null
  return createPreviewSeating(performance, Number(performanceId) % 2 === 0)
}

function createPreviewSeating(performance, chamber) {
  const seats = []
  let id = Number(performance.id) * 1000
  const add = (section, row, sectionOrder, rowOrder, labels, point) => labels.forEach((label, index) => {
    const position = point(index, labels.length)
    const unavailable = (index + rowOrder * 3 + sectionOrder * 5) % 7 < 2
    seats.push({ id: ++id, section, row, label: String(label).padStart(chamber ? 2 : 1, '0'), sectionOrder, rowOrder, seatOrder: index + 1, x: position.x, y: position.y, rotation: position.rotation ?? 0, isActive: true, state: unavailable ? 'Unavailable' : 'Available' })
  })
  if (chamber) {
    add('B', 'B1', 1, 1, range(1, 15), index => ({ x: 330 + index * 38, y: 190 }))
    add('B', 'B2', 1, 2, range(16, 29), index => ({ x: 380 + index * 38, y: 138 }))
    add('B', 'B3', 1, 3, range(30, 43), index => ({ x: 410 + index * 38, y: 86 }))
    add('A', 'A1', 2, 1, range(2, 10).reverse(), index => ({ x: 220, y: 320 + index * 38, rotation: -90 }))
    add('A', 'A2', 2, 2, range(12, 21).reverse(), index => ({ x: 158, y: 285 + index * 38, rotation: -90 }))
    add('A', 'A3', 2, 3, range(22, 32).reverse(), index => ({ x: 96, y: 250 + index * 38, rotation: -90 }))
    add('C', 'C1', 3, 1, range(1, 10), index => ({ x: 980, y: 285 + index * 38, rotation: 90 }))
    add('C', 'C2', 3, 2, range(11, 20), index => ({ x: 1042, y: 250 + index * 38, rotation: 90 }))
    add('C', 'C3', 3, 3, range(21, 30), index => ({ x: 1104, y: 250 + index * 38, rotation: 90 }))
  } else {
    ;[23, 24, 25, 26, 27, 26, 25, 26, 27, 28].forEach((count, rowIndex) => add('Main', String.fromCharCode(65 + rowIndex), 1, rowIndex + 1, range(1, count), (index) => {
      const centered = count === 1 ? 0 : index / (count - 1) * 2 - 1
      return { x: (1350 - (count - 1) * 44) / 2 + index * 44, y: 820 - rowIndex * 57 + 48 * centered * centered, rotation: centered * 10 }
    }))
    ;[11, 12].forEach(rowNumber => add('Upper', String.fromCharCode(65 + rowNumber - 1), 2, rowNumber, range(1, 14), index => ({ x: index < 9 ? 330 + index * 46 : 800 + (index - 9) * 46, y: rowNumber === 12 ? 100 : 165 })))
  }
  return { id: performance.id, showTitle: performance.showTitle, startsAt: performance.startDateTimeUtc, venue: performance.venue, canvasWidth: chamber ? 1200 : 1350, canvasHeight: chamber ? 800 : 1000, stageLabel: 'STAGE', stageX: chamber ? 300 : 230, stageY: chamber ? 285 : 920, stageWidth: chamber ? 600 : 890, stageHeight: chamber ? 350 : 54, reservationsAvailable: false, maxSeatsPerReservation: 6, unavailableMessage: languagePreviewMessage(), seats }
}

const range = (first, last) => Array.from({ length: last - first + 1 }, (_, index) => first + index)
const languagePreviewMessage = () => 'Static preview: reservations are disabled.'

export function isCanceledRequest(error) {
  return error?.name === 'CanceledError' ||
    error?.name === 'AbortError' ||
    error?.code === 'ERR_CANCELED'
}
