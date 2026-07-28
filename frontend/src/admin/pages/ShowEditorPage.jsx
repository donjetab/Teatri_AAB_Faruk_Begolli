import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { adminApi } from '../api'
import { LanguageTabs, LoadingSkeleton, PageHeader, StatusBadge, Toast } from '../components/AdminUi'
import { CreditRepeater } from '../components/CreditRepeater'
import { MediaPicker } from '../components/MediaPicker'

const blankTranslation = code => ({ languageCode: code, title: '', slug: '', shortDescription: '', fullDescription: '', metaTitle: '', metaDescription: '' })
const blank = { showCategoryId: '', posterMediaAssetId: null, featuredMediaAssetId: null, durationMinutes: '', productionYear: '', ageRecommendation: '', originalLanguage: '', trailerUrl: '', videoUrl: '', premiereDate: '', lifecycleStatus: 'Upcoming', isFeatured: false, translations: [blankTranslation('sq'), blankTranslation('en')] }
const tabs = ['Basic information', 'Albanian content', 'English content', 'Credits', 'Media', 'Performances', 'SEO', 'Publication']

export function ShowEditorPage() {
  const { id } = useParams()
  const isNew = id === 'new'
  const navigate = useNavigate()
  const [form, setForm] = useState(blank)
  const [saved, setSaved] = useState('')
  const [categories, setCategories] = useState([])
  const [tab, setTab] = useState('Basic information')
  const [toast, setToast] = useState('')
  const [loading, setLoading] = useState(true)
  const dirty = useMemo(() => !loading && JSON.stringify(form) !== saved, [form, saved, loading])

  useEffect(() => {
    Promise.all([adminApi.shows({ pageSize: 1 }), isNew ? Promise.resolve(blank) : adminApi.show(id)]).then(([list, show]) => {
      const normalized = { ...show, premiereDate: show.premiereDate ?? '', durationMinutes: show.durationMinutes ?? '', productionYear: show.productionYear ?? '', ageRecommendation: show.ageRecommendation ?? '', originalLanguage: show.originalLanguage ?? '', trailerUrl: show.trailerUrl ?? '', videoUrl: show.videoUrl ?? '' }
      setCategories(list.categories); setForm(normalized); setSaved(JSON.stringify(normalized)); setLoading(false)
    })
  }, [id, isNew])
  useEffect(() => { const warn = e => { if (dirty) { e.preventDefault(); e.returnValue = '' } }; addEventListener('beforeunload', warn); return () => removeEventListener('beforeunload', warn) }, [dirty])

  const field = (key, value) => setForm(current => ({ ...current, [key]: value }))
  const translation = code => form.translations.find(x => x.languageCode === code)
  const changeTranslation = (code, key, value) => setForm(current => ({ ...current, translations: current.translations.map(x => x.languageCode === code ? { ...x, [key]: value } : x) }))
  const slugifyTitle = (code, value) => {
    changeTranslation(code, 'title', value)
    if (!translation(code).slug) changeTranslation(code, 'slug', value.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, ''))
  }
  const payload = () => ({ ...form, showCategoryId: Number(form.showCategoryId), durationMinutes: form.durationMinutes ? Number(form.durationMinutes) : null, productionYear: form.productionYear ? Number(form.productionYear) : null, ageRecommendation: form.ageRecommendation !== '' ? Number(form.ageRecommendation) : null, premiereDate: form.premiereDate || null })
  const save = async () => {
    const result = isNew ? await adminApi.createShow(payload()) : await adminApi.saveShow(id, payload())
    setForm({ ...result, premiereDate: result.premiereDate ?? '', durationMinutes: result.durationMinutes ?? '', productionYear: result.productionYear ?? '', ageRecommendation: result.ageRecommendation ?? '', originalLanguage: result.originalLanguage ?? '', trailerUrl: result.trailerUrl ?? '', videoUrl: result.videoUrl ?? '' })
    setSaved(JSON.stringify({ ...result, premiereDate: result.premiereDate ?? '', durationMinutes: result.durationMinutes ?? '', productionYear: result.productionYear ?? '', ageRecommendation: result.ageRecommendation ?? '', originalLanguage: result.originalLanguage ?? '', trailerUrl: result.trailerUrl ?? '', videoUrl: result.videoUrl ?? '' }))
    setToast('Play saved successfully.')
    if (isNew) navigate(`/admin/shows/${result.id}`, { replace: true })
    return result
  }
  const action = async name => {
    if (dirty) await save()
    if (!window.confirm(`${name[0].toUpperCase() + name.slice(1)} this play?`)) return
    const result = await adminApi.showAction(id, name); setForm(current => ({ ...current, ...result })); setSaved(JSON.stringify({ ...form, ...result })); setToast(`Play ${name}ed.`)
  }
  if (loading) return <LoadingSkeleton rows={7} />
  const contentFields = code => <div className="form-grid"><label>Title *<input required value={translation(code).title} onChange={e => slugifyTitle(code, e.target.value)} /></label><label>Slug *<input required pattern="[a-z0-9]+(?:-[a-z0-9]+)*" value={translation(code).slug} onChange={e => changeTranslation(code, 'slug', e.target.value)} /></label><label className="full">Short description *<textarea rows="4" required maxLength="700" value={translation(code).shortDescription} onChange={e => changeTranslation(code, 'shortDescription', e.target.value)} /></label><label className="full">Synopsis *<textarea rows="12" required value={translation(code).fullDescription} onChange={e => changeTranslation(code, 'fullDescription', e.target.value)} /></label></div>

  return <><PageHeader eyebrow="Shows / Plays" title={isNew ? 'Create play' : translation('sq').title || 'Edit play'} description="Manage bilingual content, production information, media and publication state." actions={<><Link className="admin-outline-button" to="/admin/shows">Back to plays</Link>{!isNew && <StatusBadge status={form.status} />}</>} />
    <div className="editor-tabs" role="tablist">{tabs.map(x => <button type="button" className={tab === x ? 'active' : ''} onClick={() => setTab(x)} key={x}>{x}</button>)}</div>
    <form className="admin-form" onSubmit={e => { e.preventDefault(); save() }}>
      {tab === 'Basic information' && <section className="admin-panel"><h2>Basic information</h2><div className="form-grid"><label>Category *<select required value={form.showCategoryId} onChange={e => field('showCategoryId', e.target.value)}><option value="">Select category</option>{categories.map(x => <option value={x.id} key={x.id}>{x.label}</option>)}</select></label><label>Lifecycle status<select value={form.lifecycleStatus} onChange={e => field('lifecycleStatus', e.target.value)}><option>Upcoming</option><option>Active</option><option>Completed</option><option>SoldOut</option></select></label><label>Premiere date<input type="date" value={form.premiereDate} onChange={e => field('premiereDate', e.target.value)} /></label><label>Production year<input type="number" min="1900" max="2200" value={form.productionYear} onChange={e => field('productionYear', e.target.value)} /></label><label>Duration (minutes)<input type="number" min="1" max="600" value={form.durationMinutes} onChange={e => field('durationMinutes', e.target.value)} /></label><label>Age recommendation<input type="number" min="0" max="21" value={form.ageRecommendation} onChange={e => field('ageRecommendation', e.target.value)} /></label><label>Original language<input value={form.originalLanguage} onChange={e => field('originalLanguage', e.target.value)} /></label><label className="admin-switch-row"><input type="checkbox" checked={form.isFeatured} onChange={e => field('isFeatured', e.target.checked)} /> Feature this play on the website</label></div></section>}
      {tab === 'Albanian content' && <section className="admin-panel"><LanguageTabs active="sq" onChange={code => setTab(code === 'sq' ? 'Albanian content' : 'English content')} />{contentFields('sq')}</section>}
      {tab === 'English content' && <section className="admin-panel"><LanguageTabs active="en" onChange={code => setTab(code === 'sq' ? 'Albanian content' : 'English content')} />{contentFields('en')}</section>}
      {tab === 'Credits' && <section className="admin-panel">{isNew ? <div className="admin-empty"><strong>Save the play first</strong><p>Create the draft before adding structured credits.</p></div> : <CreditRepeater showId={id} onSaved={() => setToast('Credits saved successfully.')} />}</section>}
      {tab === 'Media' && <section className="admin-panel"><h2>Play media</h2><div className="form-grid"><MediaPicker label="Poster" value={form.posterMediaAssetId} onChange={value => field('posterMediaAssetId', value)} /><MediaPicker label="Featured image" value={form.featuredMediaAssetId} onChange={value => field('featuredMediaAssetId', value)} /><label>Trailer URL<input type="url" value={form.trailerUrl} onChange={e => field('trailerUrl', e.target.value)} /></label><label>Video URL<input type="url" value={form.videoUrl} onChange={e => field('videoUrl', e.target.value)} /></label></div></section>}
      {tab === 'Performances' && <section className="admin-panel editor-coming-next"><h2>Performances</h2><p>Performance dates will be managed here after the performance table and calendar module is connected.</p></section>}
      {tab === 'SEO' && <section className="admin-panel"><h2>Search appearance</h2>{['sq', 'en'].map(code => <div className="seo-language" key={code}><strong>{code === 'sq' ? 'Albanian' : 'English'}</strong><div className="form-grid"><label>Meta title<input maxLength="220" value={translation(code).metaTitle ?? ''} onChange={e => changeTranslation(code, 'metaTitle', e.target.value)} /></label><label>Meta description<textarea rows="3" maxLength="320" value={translation(code).metaDescription ?? ''} onChange={e => changeTranslation(code, 'metaDescription', e.target.value)} /></label></div></div>)}</section>}
      {tab === 'Publication' && <section className="admin-panel"><h2>Publication settings</h2><div className="publication-state"><StatusBadge status={form.status ?? 'Draft'} /><p>{form.status === 'Published' ? 'This play is visible on the public website.' : form.status === 'Archived' ? 'This historical play is archived.' : 'This play is private and can be reviewed before publishing.'}</p></div>{!isNew && <div className="publication-actions">{form.status !== 'Published' && <button type="button" className="admin-primary-button" onClick={() => action('publish')}>Publish play</button>}{form.status === 'Published' && <button type="button" className="admin-outline-button" onClick={() => action('unpublish')}>Unpublish</button>}{form.status !== 'Archived' ? <button type="button" className="admin-danger-button" onClick={() => action('archive')}>Archive play</button> : <button type="button" className="admin-outline-button" onClick={() => action('restore')}>Restore as draft</button>}<button type="button" className="admin-text-button" onClick={() => adminApi.showAction(id, 'duplicate').then(x => navigate(`/admin/shows/${x.id}`))}>Duplicate</button></div>}</section>}
      <div className="sticky-save"><button className="admin-primary-button" type="submit" disabled={!dirty}>{isNew ? 'Create draft' : 'Save changes'}</button><span className={dirty ? 'unsaved-chip' : 'saved-chip'}>{dirty ? 'Unsaved changes' : 'All changes saved'}</span>{!isNew && <a className="admin-outline-button" href={`#/sq/shfaqjet/${translation('sq').slug}`} target="_blank">Preview →</a>}</div>
    </form><Toast message={toast} onClose={() => setToast('')} /></>
}
