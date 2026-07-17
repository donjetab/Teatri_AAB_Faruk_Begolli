import { useTranslation } from 'react-i18next'
import showsHeaderBackground from '../assets/shows-header.jpg'
import smoke from '../assets/smoke_3.png'
import theatreIcon from '../assets/acting-icon-gold.png'
import bretkosaPoster from '../assets/shows/bretkosa/poster.png'
import rrenaPoster from '../assets/shows/rrena/poster.jpg'
import profesorPoster from '../assets/shows/profesor-jam-talent/poster.png'
import tjetriPoster from '../assets/shows/tjetri/poster.png'
import mersieriPoster from '../assets/shows/mersieri-dhe-kamieri/poster.png'
import gjirafaPoster from '../assets/shows/gjirafa-dhe-buburreci/poster.jpg'
import qeniPoster from '../assets/shows/qeni-i-baskervileve/poster.jpg'
import brinatPoster from '../assets/shows/brinat/poster.png'
import hanaPoster from '../assets/shows/hana-dhe-dielli/poster.png'
import hillaryPoster from '../assets/shows/per-caj-te-hillary/poster.jpg'
import dyGjitarePoster from '../assets/shows/dy-gjitare-enderrimtare/poster.jpg'

export function ShowsPage() {
  const { t } = useTranslation()
  const filters = ['all', 'drama', 'comedy', 'children']
  const shows = [
    { title: 'Bretkosa', poster: bretkosaPoster },
    { title: 'Rrena', poster: rrenaPoster },
    { title: 'Profesor, Jam Talent...3', poster: profesorPoster },
    { title: 'Tjetri', poster: tjetriPoster },
    { title: 'Mersieri Dhe Kamieri', poster: mersieriPoster },
    { title: 'Gjirafa Dhe Buburreci', poster: gjirafaPoster },
    { title: 'Qeni I Baskërvileve', poster: qeniPoster },
    { title: 'Brinat', poster: brinatPoster },
    { title: 'Hana Dhe Dielli', poster: hanaPoster },
    { title: 'Për Çaj Te Hillary', poster: hillaryPoster },
    { title: 'Dy Gjitarë Ëndërrimtarë', poster: dyGjitarePoster },
  ]

  return (
    <article className="shows-page">
      <section
        className="shows-page-hero page-hero"
        style={{ '--page-hero-image': `url("${showsHeaderBackground}")` }}
        aria-labelledby="shows-page-title"
      >
        <img className="page-hero-smoke" src={smoke} alt="" aria-hidden="true" />
        <div className="page-hero-content">
          <h1 id="shows-page-title">{t('showsPage.heroTitle')}</h1>
          <div className="page-hero-rule" aria-hidden="true">
            <span />
            <img src={theatreIcon} alt="" aria-hidden="true" />
            <span />
          </div>
          <p>{t('showsPage.heroSubtitle')}</p>
          <div className="shows-filter-bar" aria-label={t('showsPage.filtersLabel')}>
            {filters.map((filter, index) => (
              <button
                key={filter}
                type="button"
                className={index === 0 ? 'shows-filter active' : 'shows-filter'}
              >
                {t(`showsPage.filters.${filter}`)}
              </button>
            ))}
          </div>
        </div>
      </section>

      <section className="shows-list-section" aria-label={t('showsPage.listLabel')}>
        <div className="shows-page-grid">
          {shows.map((show) => (
            <article className="shows-page-card" key={show.title}>
              <a href="#" className="shows-page-card-link" aria-label={t('showsPage.openShow', { title: show.title })}>
                <img src={show.poster} alt={t('showsPage.posterAlt', { title: show.title })} loading="lazy" />
                <span className="shows-page-card-overlay" aria-hidden="true" />
                <div className="shows-page-card-copy">
                  <h2>{show.title}</h2>
                  <p>{t('showsPage.directorPlaceholder')}</p>
                </div>
                <span className="shows-page-card-actions" aria-hidden="true">
                  <span />
                  <span />
                </span>
              </a>
            </article>
          ))}
        </div>
      </section>
    </article>
  )
}
