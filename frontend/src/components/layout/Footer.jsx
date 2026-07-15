import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import logoScene from '../../assets/Teatri Logo/Teatri Logo -W-RED.png'
import { getLocalizedPath } from '../../routes/localizedRoutes'
import { NewsletterForm } from './NewsletterForm'

export function Footer({ language, homepageMeta }) {
  const { t } = useTranslation()

  const links = ['home', 'about', 'shows', 'news', 'gallery']

  return (
    <footer className="site-footer">
      <div className="site-footer-inner">
        <Link to={getLocalizedPath('home', language)} className="footer-logo-link" aria-label={t('brand')}>
          <img src={logoScene} alt={t('brand')} className="footer-logo" />
        </Link>

        <section className="footer-column">
          <h2>{t('footer.links')}</h2>
          <ul>
            {links.map((item) => (
              <li key={item}>
                <Link to={getLocalizedPath(item, language)}>{t(`nav.${item}`)}</Link>
              </li>
            ))}
          </ul>
        </section>

        <section className="footer-column">
          <h2>{t('footer.visit')}</h2>
          <ul>
            <li>
              <Link to={getLocalizedPath('contact', language)}>{t('footer.location')}</Link>
            </li>
            <li>
              <Link to={getLocalizedPath('contact', language)}>{t('nav.contact')}</Link>
            </li>
          </ul>
        </section>

        <section className="footer-column social-column">
          <h2>{t('footer.follow')}</h2>
          <div className="social-links">
            <a href={homepageMeta?.facebookUrl ?? 'https://www.facebook.com/aabtheatre'} aria-label="Facebook">
              <span className="social-facebook">f</span>
            </a>
            <a href={homepageMeta?.instagramUrl ?? 'https://www.instagram.com/aabtheatre'} aria-label="Instagram">
              <span className="social-instagram" />
            </a>
          </div>
        </section>

        <section className="footer-column newsletter-column">
          <h2>{t('footer.newsletter')}</h2>
          <NewsletterForm language={language} />
        </section>
      </div>
    </footer>
  )
}
