import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import logoScene from '../../assets/Teatri Logo/Teatri Logo -W-RED.png'
import { getLocalizedPath } from '../../routes/localizedRoutes'
import { NewsletterForm } from './NewsletterForm'
import { resolveMediaUrl } from '../../api/client'

export function Footer({ language, homepageMeta, navigation }) {
  const { t } = useTranslation()
  const managed = navigation?.translations?.find(item => item.languageCode === language)
  const links = navigation?.items?.filter(item => item.showInFooter).sort((a, b) => a.sortOrder - b.sortOrder).map(item => item.routeKey) ?? ['home', 'about', 'shows', 'news', 'gallery', 'location']
  const mainLinks = links.filter(item => item !== 'location' && item !== 'contact')
  const visitLinks = links.filter(item => item === 'location' || item === 'contact')
  const label = item => managed?.labels?.[item] || t(`nav.${item}`)

  return (
    <footer className="site-footer">
      <div className="site-footer-inner">
        <Link to={getLocalizedPath('home', language)} className="footer-logo-link" aria-label={t('brand')}>
          <img src={resolveMediaUrl(homepageMeta?.footerLogoUrl) || logoScene} alt={t('brand')} className="footer-logo" />
        </Link>

        <section className="footer-column">
          <h2>{managed?.footerLinksTitle || t('footer.links')}</h2>
          <ul>
            {mainLinks.map((item) => (
              <li key={item}>
                <Link to={getLocalizedPath(item, language)}>{label(item)}</Link>
              </li>
            ))}
          </ul>
        </section>

        <section className="footer-column">
          <h2>{managed?.footerVisitTitle || t('footer.visit')}</h2>
          <ul>
            {visitLinks.map(item => <li key={item}><Link to={getLocalizedPath(item, language)}>{label(item)}</Link></li>)}
          </ul>
        </section>

        <section className="footer-column social-column">
          <h2>{managed?.footerFollowTitle || t('footer.follow')}</h2>
          <div className="social-links">
            {homepageMeta?.facebookUrl && (
              <a href={homepageMeta.facebookUrl} target="_blank" rel="noreferrer" aria-label="Facebook">
                <span className="social-facebook">f</span>
              </a>
            )}
            {homepageMeta?.instagramUrl && (
              <a href={homepageMeta.instagramUrl} target="_blank" rel="noreferrer" aria-label="Instagram">
                <span className="social-instagram" />
              </a>
            )}
          </div>
        </section>

        <section className="footer-column newsletter-column">
          <h2>{managed?.footerNewsletterTitle || t('footer.newsletter')}</h2>
          <NewsletterForm language={language} invitation={managed?.footerNewsletterText} />
        </section>
      </div>
      {homepageMeta?.footerCopyrightText && <p className="footer-copyright">{homepageMeta.footerCopyrightText}</p>}
    </footer>
  )
}
