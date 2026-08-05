import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { adminApi } from '../api'
import { LanguageTabs, LoadingSkeleton, PageHeader, Toast } from '../components/AdminUi'

const destinations = {
  home: { sq: '/sq', en: '/en' }, about: { sq: '/sq/per-ne', en: '/en/about' },
  shows: { sq: '/sq/shfaqjet', en: '/en/shows' }, news: { sq: '/sq/lajme', en: '/en/news' },
  pitf: { sq: '/sq/pitf', en: '/en/pitf' }, gallery: { sq: '/sq/galeria', en: '/en/gallery' },
  location: { sq: '/sq/kontakti', en: '/en/contact' },
  contact: { sq: '/sq/kontakti', en: '/en/contact' },
}

export function NavigationFooterPage() {
  const [form, setForm] = useState(null)
  const [saved, setSaved] = useState('')
  const [language, setLanguage] = useState('sq')
  const [toast, setToast] = useState('')
  useEffect(() => { Promise.all([adminApi.navigation(), adminApi.website()]).then(([navigation, website]) => { const value = { ...navigation, website }; setForm(value); setSaved(JSON.stringify(value)) }) }, [])
  if (!form) return <LoadingSkeleton rows={7} />
  const dirty = JSON.stringify(form) !== saved
  const translation = form.translations.find(item => item.languageCode === language)
  const ordered = [...form.items].sort((a, b) => a.sortOrder - b.sortOrder)
  const updateTranslation = values => setForm({ ...form, translations: form.translations.map(item => item.languageCode === language ? { ...item, ...values } : item) })
  const updateItem = (routeKey, values) => setForm({ ...form, items: form.items.map(item => item.routeKey === routeKey ? { ...item, ...values } : item) })
  const move = (routeKey, direction) => {
    const index = ordered.findIndex(item => item.routeKey === routeKey); const target = index + direction
    if (target < 0 || target >= ordered.length) return
    const next = [...ordered]; [next[index], next[target]] = [next[target], next[index]]
    setForm({ ...form, items: next.map((item, sortOrder) => ({ ...item, sortOrder })) })
  }
  const save = async () => {
    const data = await adminApi.saveNavigation({ items: ordered, translations: form.translations })
    const value = { ...data, website: form.website }; setForm(value); setSaved(JSON.stringify(value)); setToast('Navigation and footer saved. The public website has been updated.')
  }
  return <>
    <PageHeader eyebrow="Website" title="Navigation & Footer" description="Edit bilingual menu labels, visibility, order and footer wording without typing destination URLs." actions={<a className="admin-outline-button" href={`#/${language}`} target="_blank" rel="noreferrer">Open website ↗</a>} />
    <LanguageTabs active={language} onChange={setLanguage} />
    <section className="admin-panel admin-form navigation-management-panel"><div className="panel-heading"><div><h2>Header and footer links</h2><p>Use the arrows to reorder pages. Destinations are protected from typing mistakes.</p></div><span className={dirty ? 'unsaved-chip' : 'saved-chip'}>{dirty ? 'Unsaved changes' : 'All changes saved'}</span></div>
      <div className="navigation-editor-list">{ordered.map((item, index) => <article key={item.routeKey}>
        <div className="navigation-reorder"><button type="button" disabled={index === 0} onClick={() => move(item.routeKey, -1)} aria-label="Move up">↑</button><button type="button" disabled={index === ordered.length - 1} onClick={() => move(item.routeKey, 1)} aria-label="Move down">↓</button></div>
        <label>Label ({language === 'sq' ? 'Albanian' : 'English'})<input required value={translation.labels[item.routeKey] ?? ''} onChange={event => updateTranslation({ labels: { ...translation.labels, [item.routeKey]: event.target.value } })} /></label>
        <div className="navigation-destination"><span>Destination</span><strong>{destinations[item.routeKey][language]}</strong></div>
        <label className="admin-check"><input type="checkbox" checked={item.showInHeader} onChange={event => updateItem(item.routeKey, { showInHeader: event.target.checked })} /> Header</label>
        <label className="admin-check"><input type="checkbox" checked={item.showInFooter} onChange={event => updateItem(item.routeKey, { showInFooter: event.target.checked })} /> Footer links</label>
      </article>)}</div>
      <label className="navigation-reserve-label">Reservation button label ({language === 'sq' ? 'Albanian' : 'English'})<input value={translation.reserveLabel} onChange={event => updateTranslation({ reserveLabel: event.target.value })} /></label>
    </section>
    <section className="admin-panel admin-form navigation-management-panel"><div className="panel-heading"><div><h2>Footer wording</h2><p>Edit the headings and newsletter invitation for the selected language.</p></div></div><div className="form-grid">
      <label>Links column heading<input value={translation.footerLinksTitle} onChange={event => updateTranslation({ footerLinksTitle: event.target.value })} /></label>
      <label>Visit column heading<input value={translation.footerVisitTitle} onChange={event => updateTranslation({ footerVisitTitle: event.target.value })} /></label>
      <label>Social column heading<input value={translation.footerFollowTitle} onChange={event => updateTranslation({ footerFollowTitle: event.target.value })} /></label>
      <label>Newsletter heading<input value={translation.footerNewsletterTitle} onChange={event => updateTranslation({ footerNewsletterTitle: event.target.value })} /></label>
      <label>Newsletter invitation<input value={translation.footerNewsletterText} onChange={event => updateTranslation({ footerNewsletterText: event.target.value })} /></label>
    </div><div className="footer-shared-information"><div><span>Shared website information</span><strong>{form.website.facebookUrl ? 'Facebook connected' : 'Facebook not configured'} · {form.website.instagramUrl ? 'Instagram connected' : 'Instagram not configured'}</strong><small>Social URLs, theatre name, address, logo and copyright are edited in Website Information.</small></div><Link className="admin-outline-button" to="/admin/website-information">Edit shared details →</Link></div></section>
    <div className="sticky-save"><button type="button" className="admin-primary-button" disabled={!dirty} onClick={save}>Save navigation & footer</button></div><Toast message={toast} onClose={() => setToast('')} />
  </>
}
