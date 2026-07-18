import bretkosaPoster from './bretkosa/poster.png'
import rrenaPoster from './rrena/poster.jpg'
import profesorPoster from './profesor-jam-talent/poster.png'
import tjetriPoster from './tjetri/poster.png'
import mersieriPoster from './mersieri-dhe-kamieri/poster.png'
import gjirafaPoster from './gjirafa-dhe-buburreci/poster.jpg'
import qeniPoster from './qeni-i-baskervileve/poster.jpg'
import brinatPoster from './brinat/poster.png'
import hanaPoster from './hana-dhe-dielli/poster.png'
import hillaryPoster from './per-caj-te-hillary/poster.jpg'
import dyGjitarePoster from './dy-gjitare-enderrimtare/poster.jpg'
import veneraPoster from './venera-ne-gezof/poster.png'

export const postersBySlug = {
  bretkosa: bretkosaPoster,
  rrena: rrenaPoster,
  'profesor-jam-talent': profesorPoster,
  tjetri: tjetriPoster,
  'mersieri-dhe-kamieri': mersieriPoster,
  'gjirafa-dhe-buburreci': gjirafaPoster,
  'qeni-i-baskervileve': qeniPoster,
  brinat: brinatPoster,
  'hana-dhe-dielli': hanaPoster,
  'per-caj-te-hillary': hillaryPoster,
  'dy-gjitare-enderrimtare': dyGjitarePoster,
  'venera-ne-gezof': veneraPoster,
}

const galleryModules = import.meta.glob('./*/gallery/*.{jpg,jpeg,png}', {
  eager: true,
  query: '?url',
  import: 'default',
})

const trailerModules = import.meta.glob('./*/trailer/*.mp4', {
  eager: true,
  query: '?url',
  import: 'default',
})

function groupAssetsByShow(modules) {
  return Object.entries(modules)
    .sort(([left], [right]) => left.localeCompare(right, undefined, { numeric: true }))
    .reduce((assets, [path, url]) => {
      const slug = path.split('/')[1]
      assets[slug] ??= []
      assets[slug].push(url)
      return assets
    }, {})
}

export const galleriesBySlug = groupAssetsByShow(galleryModules)
export const heroImagesBySlug = Object.fromEntries(
  Object.entries(galleriesBySlug).map(([slug, images]) => [slug, images[0]]),
)
export const trailersBySlug = Object.fromEntries(
  Object.entries(groupAssetsByShow(trailerModules)).map(([slug, videos]) => [slug, videos[0]]),
)
