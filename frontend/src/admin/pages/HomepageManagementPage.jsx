import { useEffect, useState } from 'react'
import { adminApi } from '../api'
import { LanguageTabs, LoadingSkeleton, PageHeader, Toast } from '../components/AdminUi'
import { MediaPicker } from '../components/MediaPicker'

const sections = [
  { title: 'Hero section', media: ['Hero background', 'heroMediaAssetId'], fields: [['Hero slogan', 'heroSlogan'], ['Supporting text', 'heroSupportingText'], ['Primary button text', 'heroButtonText']], destination: ['Primary button destination', 'primaryButtonLink'] },
  { title: 'About preview', media: ['About preview image', 'aboutMediaAssetId'], fields: [['Title', 'aboutTitle'], ['Description', 'aboutShort', true]] },
  { title: 'Reservation banner', media: ['Reservation banner background', 'reservationMediaAssetId'], fields: [['Title', 'reservationTitle'], ['Description', 'reservationText', true], ['Button text', 'reservationButtonText']], destination: ['Reservation destination', 'reservationUrl'] },
  { title: 'PITF feature', media: ['PITF feature image', 'pitfMediaAssetId'], fields: [['Title', 'pitfTitle'], ['Description', 'pitfDescription', true], ['Button text', 'pitfButtonText']], destination: ['PITF button destination', 'pitfDestinationUrl'] },
]
const pageOptions = [
  ['#/sq', 'Homepage'], ['#/sq/shfaqjet', 'Shows / Plays'], ['#/sq/per-ne', 'About the theatre'],
  ['#/sq/rezervo', 'Reserve tickets'], ['#/sq/lajme', 'News'], ['#/sq/pitf', 'PITF page'],
  ['#/sq/galeria', 'Gallery'], ['#/sq/kontakti', 'Contact'],
  ['https://pitf.teatriaab.com/', 'External PITF website'],
]

export function HomepageManagementPage() {
  const [form, setForm] = useState(null)
  const [saved, setSaved] = useState('')
  const [lang, setLang] = useState('sq')
  const [toast, setToast] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  useEffect(() => { adminApi.homepage().then(x => { setForm(x); setSaved(JSON.stringify(x)) }) }, [])
  if (!form) return <LoadingSkeleton />
  const dirty = JSON.stringify(form) !== saved
  const tr = form.translations.find(x => x.languageCode === lang)
  const change = (key, value) => setForm({ ...form, [key]: value })
  const changeTr = (key, value) => setForm({ ...form, translations: form.translations.map(x => x.languageCode === lang ? { ...x, [key]: value } : x) })
  const save = async e => {
    e.preventDefault(); setBusy(true); setError('')
    try { const data = await adminApi.saveHomepage(form); setForm(data); setSaved(JSON.stringify(data)); setToast('Homepage settings saved.') }
    catch (e) { setError(e.response?.data?.detail ?? e.response?.data?.title ?? 'The homepage could not be saved.') }
    finally { setBusy(false) }
  }

  return <><PageHeader eyebrow="Content" title="Homepage" description="Edit each homepage section in Albanian and English and preview the result." actions={<a className="admin-outline-button" href={`#/${lang}`} target="_blank" rel="noreferrer">Preview {lang.toUpperCase()} ↗</a>} />
    <form className="admin-form" onSubmit={save}><LanguageTabs active={lang} onChange={setLang} />
      {sections.map(section => <section className="admin-panel" key={section.title}>
        <div className="panel-heading"><h2>{section.title}</h2></div>
        <div className="form-grid">
          {section.fields.map(([label, key, large]) => <label className={large ? 'full' : ''} key={key}>{label}{key === 'heroSupportingText' && <small>Optional — leave empty to hide this text.</small>}{large ? <textarea rows="5" value={tr[key]} onChange={e => changeTr(key, e.target.value)} /> : <input value={tr[key]} onChange={e => changeTr(key, e.target.value)} />}</label>)}
          {section.destination && <label>{section.destination[0]}<select value={section.destination[1] === 'reservationUrl' ? '#/sq/rezervo' : (form[section.destination[1]] ?? '')} onChange={e => change(section.destination[1], e.target.value)}>{(section.destination[1] === 'reservationUrl' ? [['#/sq/rezervo', 'Reserve tickets']] : pageOptions).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select><small>The matching English page is selected automatically.</small></label>}
          {section.media && <div className="full"><MediaPicker label={section.media[0]} type="image/" value={form[section.media[1]]} onSelect={media => change(section.media[1], media?.id ?? null)} /></div>}
        </div>
      </section>)}
      {error && <div className="admin-form-error">{error}</div>}
      <div className="sticky-save"><button className="admin-primary-button" disabled={!dirty || busy}>{busy ? 'Saving…' : 'Save homepage'}</button><span className={dirty ? 'unsaved-chip' : 'saved-chip'}>{dirty ? 'Unsaved changes' : 'Saved'}</span></div>
    </form><Toast message={toast} onClose={() => setToast('')} /></>
}
