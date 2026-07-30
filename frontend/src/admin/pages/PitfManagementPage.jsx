import { useEffect, useState } from 'react'
import { adminApi } from '../api'
import { LanguageTabs, LoadingSkeleton, PageHeader, Toast } from '../components/AdminUi'
import { MediaPicker } from '../components/MediaPicker'
import { useAdminDialog } from '../components/AdminDialog'

const translation = (code, name = '') => ({ languageCode: code, name })
const apiError = (error, fallback) => {
  const errors = error.response?.data?.errors
  if (errors) return Object.values(errors).flat().join(' ')
  return error.response?.data?.detail ?? error.response?.data?.title ?? fallback
}

export function PitfManagementPage() {
  const dialog = useAdminDialog()
  const [data, setData] = useState(null)
  const [lang, setLang] = useState('sq')
  const [busy, setBusy] = useState(false)
  const [toast, setToast] = useState('')
  const [error, setError] = useState('')
  const [editionErrors, setEditionErrors] = useState({})

  const normalize = result => ({
    ...result,
    translations: ['sq', 'en'].map(languageCode => result.translations.find(x => x.languageCode === languageCode) ?? { languageCode, title: '', description: '', buttonText: '' }),
    editions: result.editions.map(edition => ({ ...edition, translations: ['sq', 'en'].map(languageCode => edition.translations.find(x => x.languageCode === languageCode) ?? translation(languageCode)) })),
  })
  const load = () => adminApi.pitf().then(result => setData(normalize(result))).catch(e => setError(e.response?.data?.detail ?? 'PITF content could not be loaded.'))
  useEffect(() => { void load() }, [])
  if (!data) return error ? <div className="admin-form-error">{error}</div> : <LoadingSkeleton rows={8} />

  const text = data.translations.find(item => item.languageCode === lang)
  const updateText = (key, value) => setData(current => ({ ...current, translations: current.translations.map(item => item.languageCode === lang ? { ...item, [key]: value } : item) }))
  const updateEdition = (index, change) => setData(current => ({ ...current, editions: current.editions.map((item, itemIndex) => itemIndex === index ? { ...item, ...change } : item) }))
  const updateEditionName = (index, code, name) => setData(current => ({
    ...current,
    editions: current.editions.map((edition, editionIndex) => editionIndex === index
      ? { ...edition, translations: edition.translations.map(item => item.languageCode === code ? { ...item, name } : item) }
      : edition),
  }))

  const savePage = async () => {
    setBusy(true); setError('')
    try {
      setData(normalize(await adminApi.savePitfPage({ imageMediaAssetId: data.imageMediaAssetId, buttonUrl: data.buttonUrl, translations: data.translations })))
      setToast('PITF page content saved.')
    } catch (e) { setError(e.response?.data?.detail ?? 'PITF page content could not be saved.') }
    finally { setBusy(false) }
  }
  const saveEdition = async (edition, index) => {
    const editionKey = edition.id ?? edition.clientKey
    const sqName = edition.translations.find(item => item.languageCode === 'sq')?.name.trim()
    const enName = edition.translations.find(item => item.languageCode === 'en')?.name.trim()
    if (!edition.editionNumber || !edition.year || !sqName || !enName) {
      setEditionErrors(current => ({ ...current, [editionKey]: 'Edition number, year, Albanian name and English name are required.' }))
      return
    }
    setBusy(true); setError('')
    setEditionErrors(current => ({ ...current, [editionKey]: '' }))
    try {
      const payload = { ...edition, destinationUrl: edition.destinationUrl?.trim() || null, coverUrl: undefined, id: undefined, clientKey: undefined }
      const result = edition.id ? await adminApi.savePitfEdition(edition.id, payload) : await adminApi.createPitfEdition(payload)
      setData(normalize(result)); setToast(`PITF ${edition.year} saved.`)
    } catch (e) {
      const message = apiError(e, 'The edition could not be saved.')
      setEditionErrors(current => ({ ...current, [editionKey]: message }))
    }
    finally { setBusy(false) }
  }
  const addEdition = () => setData(current => {
    const nextNumber = Math.max(0, ...current.editions.map(item => item.editionNumber)) + 1
    const clientKey = `new-${Date.now()}`
    window.requestAnimationFrame(() => document.getElementById(`pitf-edition-${clientKey}`)?.scrollIntoView({ behavior: 'smooth', block: 'center' }))
    return { ...current, editions: [{ id: null, clientKey, editionNumber: nextNumber, year: new Date().getFullYear(), coverMediaAssetId: null, coverUrl: null, destinationUrl: '', isPublished: true, translations: [translation('sq'), translation('en')] }, ...current.editions] }
  })
  const removeEdition = async (edition, index) => {
    if (!await dialog.confirm({ title: 'Delete PITF edition?', message: `PITF ${edition.year} will be removed.`, confirmLabel: 'Delete edition', danger: true })) return
    if (edition.id) await adminApi.deletePitfEdition(edition.id)
    setData(current => ({ ...current, editions: current.editions.filter((_, itemIndex) => itemIndex !== index) }))
    setToast('PITF edition removed.')
  }

  return <><PageHeader eyebrow="Festival" title="PITF" description="Edit the public PITF page and manage every festival edition." actions={<a className="admin-outline-button" href="#/sq/pitf" target="_blank" rel="noreferrer">Open PITF page ↗</a>} />
    <section className={`admin-panel admin-form language-${lang}`}><div className="panel-heading"><div><h2>PITF page content</h2><p>This content belongs only to the public PITF page. Homepage PITF content remains separate.</p></div><LanguageTabs active={lang} onChange={setLang} /></div><div className="form-grid"><label>Title *<input value={text.title} onChange={e => updateText('title', e.target.value)} /></label><label>Button text *<input value={text.buttonText} onChange={e => updateText('buttonText', e.target.value)} /></label><label className="full">Description *<textarea rows="5" value={text.description} onChange={e => updateText('description', e.target.value)} /></label><label className="full">Button destination URL<input type="url" value={data.buttonUrl ?? ''} onChange={e => setData({ ...data, buttonUrl: e.target.value })} /></label><div className="full"><MediaPicker label="PITF page image" value={data.imageMediaAssetId} currentUrl={data.imageUrl} type="image/" onSelect={media => setData(current => ({ ...current, imageMediaAssetId: media?.id ?? null, imageUrl: media?.fileUrl ?? null }))} /></div></div><button className="admin-primary-button" type="button" disabled={busy} onClick={savePage}>Save PITF page</button></section>
    <section className="admin-panel admin-form pitf-editions-admin"><div className="panel-heading"><div><h2>PITF editions</h2><p>Editions are automatically shown from the highest edition number to the lowest. A destination link is optional.</p></div><button className="admin-primary-button" type="button" onClick={addEdition}>+ Add edition</button></div><div className="pitf-admin-grid">{data.editions.map((edition, index) => <article id={`pitf-edition-${edition.id ?? edition.clientKey}`} key={edition.id ?? edition.clientKey}><header className="pitf-edition-admin-header"><div><span>Edition</span><strong>#{edition.editionNumber || '—'} · {edition.year || 'Year'}</strong></div></header><div className="form-grid"><label>Edition number<input type="number" min="1" value={edition.editionNumber} onChange={e => updateEdition(index, { editionNumber: Number(e.target.value) })} /></label><label>Year<input type="number" min="2000" max="2200" value={edition.year} onChange={e => updateEdition(index, { year: Number(e.target.value) })} /></label><label>Name (Albanian) *<input value={edition.translations.find(x => x.languageCode === 'sq')?.name ?? ''} onChange={e => updateEditionName(index, 'sq', e.target.value)} /></label><label>Name (English) *<input value={edition.translations.find(x => x.languageCode === 'en')?.name ?? ''} onChange={e => updateEditionName(index, 'en', e.target.value)} /></label><label className="full">Optional destination URL<input type="url" placeholder="https://…" value={edition.destinationUrl ?? ''} onChange={e => updateEdition(index, { destinationUrl: e.target.value })} /></label><div className="full"><MediaPicker label="Edition picture" value={edition.coverMediaAssetId} currentUrl={edition.coverUrl} type="image/" onSelect={media => updateEdition(index, { coverMediaAssetId: media?.id ?? null, coverUrl: media?.fileUrl ?? null })} /></div></div>{editionErrors[edition.id ?? edition.clientKey] && <div className="pitf-edition-error">{editionErrors[edition.id ?? edition.clientKey]}</div>}<footer><button type="button" className="admin-primary-button" disabled={busy} onClick={() => saveEdition(edition, index)}>Save edition</button><button type="button" className="admin-danger-button" onClick={() => removeEdition(edition, index)}>Delete</button></footer></article>)}</div></section>
    {error && <div className="admin-form-error">{error}</div>}<Toast message={toast} onClose={() => setToast('')} /></>
}
