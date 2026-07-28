import { useEffect, useState } from 'react'
import { adminApi } from '../api'
import { LanguageTabs, LoadingSkeleton, PageHeader, Toast } from '../components/AdminUi'

const sections = [
  ['Hero section', [['Hero slogan', 'heroSlogan'], ['Supporting text', 'heroSupportingText'], ['Primary button text', 'heroButtonText']]],
  ['About preview', [['Title', 'aboutTitle'], ['Description', 'aboutShort', true], ['Button label', 'aboutButtonText']]],
  ['Reservation banner', [['Title', 'reservationTitle'], ['Description', 'reservationText', true], ['Button text', 'reservationButtonText']]],
  ['PITF feature', [['Title', 'pitfTitle'], ['Description', 'pitfDescription', true], ['Button text', 'pitfButtonText']]],
]
export function HomepageManagementPage() {
  const [form, setForm] = useState(null); const [saved, setSaved] = useState(''); const [lang, setLang] = useState('sq'); const [toast, setToast] = useState('')
  useEffect(() => { adminApi.homepage().then(x => { setForm(x); setSaved(JSON.stringify(x)) }) }, [])
  if (!form) return <LoadingSkeleton />
  const dirty = JSON.stringify(form) !== saved
  const tr = form.translations.find(x => x.languageCode === lang)
  const change = (key, value) => setForm({ ...form, [key]: value })
  const changeTr = (key, value) => setForm({ ...form, translations: form.translations.map(x => x.languageCode === lang ? { ...x, [key]: value } : x) })
  const save = async e => { e.preventDefault(); const data = await adminApi.saveHomepage(form); setForm(data); setSaved(JSON.stringify(data)); setToast('Homepage settings saved.') }
  return <><PageHeader eyebrow="Content" title="Homepage" description="Edit each homepage section in Albanian and English, control visibility and preview the result." actions={<a className="admin-outline-button" href={`#/${lang}`} target="_blank">Preview {lang.toUpperCase()} ↗</a>} />
    <form className="admin-form" onSubmit={save}><LanguageTabs active={lang} onChange={setLang} />
      {sections.map(([title, fields], index) => <section className="admin-panel" key={title}><div className="panel-heading"><h2>{title}</h2>{index !== 1 && <label className="admin-switch"><input type="checkbox" checked={index === 0 ? form.heroIsVisible : index === 2 ? form.reservationBannerIsVisible : form.pitfFeatureIsVisible} onChange={e => change(index === 0 ? 'heroIsVisible' : index === 2 ? 'reservationBannerIsVisible' : 'pitfFeatureIsVisible', e.target.checked)} /><span /> Visible</label>}</div><div className="form-grid">{fields.map(([label, key, large]) => <label className={large ? 'full' : ''} key={key}>{label}{large ? <textarea rows="5" value={tr[key]} onChange={e => changeTr(key, e.target.value)} /> : <input value={tr[key]} onChange={e => changeTr(key, e.target.value)} />}</label>)}</div></section>)}
      <section className="admin-panel"><h2>Homepage selections</h2><div className="form-grid"><label>Latest news article count<input type="number" min="1" max="12" value={form.latestNewsCount} onChange={e => change('latestNewsCount', Number(e.target.value))} /></label><label>Reservation URL<input type="url" value={form.reservationUrl ?? ''} onChange={e => change('reservationUrl', e.target.value)} /></label></div><p className="admin-help">Featured play and news ordering will use the existing featured flags when their list-management screens are connected.</p></section>
      <div className="sticky-save"><button className="admin-primary-button" disabled={!dirty}>Save homepage</button><span className={dirty ? 'unsaved-chip' : 'saved-chip'}>{dirty ? 'Unsaved changes' : 'Saved'}</span></div>
    </form><Toast message={toast} onClose={() => setToast('')} /></>
}
