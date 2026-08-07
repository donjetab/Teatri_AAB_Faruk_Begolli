import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { adminApi } from '../api'
import { LanguageTabs, LoadingSkeleton, PageHeader, StatusBadge, Toast } from '../components/AdminUi'
import { CreditRepeater } from '../components/CreditRepeater'
import { MediaPicker } from '../components/MediaPicker'
import { resolveMediaUrl } from '../../api/client'
import { useAdminDialog } from '../components/AdminDialog'

const blankTranslation = code => ({ languageCode: code, title: '', slug: '', shortDescription: '', fullDescription: '', metaTitle: '', metaDescription: '' })
const blank = { showCategoryId: '', posterMediaAssetId: null, featuredMediaAssetId: null, durationMinutes: '', productionYear: '', ageRecommendation: '', originalLanguage: '', trailerUrl: '', videoUrl: '', premiereDate: '', lifecycleStatus: 'Upcoming', isFeatured: false, isGuestPerformance: false, galleryMedia: [], translations: [blankTranslation('sq'), blankTranslation('en')] }
const tabs = ['Basic information', 'Albanian content', 'English content', 'Credits', 'Media', 'Performances', 'SEO', 'Publication']

export function ShowEditorPage() {
  const dialog = useAdminDialog()
  const { id } = useParams()
  const isNew = id === 'new'
  const navigate = useNavigate()
  const [form, setForm] = useState(blank)
  const [saved, setSaved] = useState('')
  const [categories, setCategories] = useState([])
  const [tab, setTab] = useState('Basic information')
  const [toast, setToast] = useState('')
  const [toastType, setToastType] = useState('success')
  const [loading, setLoading] = useState(true)
  const [trailerMode, setTrailerMode] = useState('local')
  const [localTrailerMediaId, setLocalTrailerMediaId] = useState(null)
  const [galleryBusy, setGalleryBusy] = useState(false)
  const [draftCredits, setDraftCredits] = useState([])
  const dirty = useMemo(() => !loading && (JSON.stringify(form) !== saved || (isNew && draftCredits.length > 0)), [form, saved, loading, isNew, draftCredits])

  useEffect(() => {
    Promise.all([adminApi.shows({ pageSize: 1 }), isNew ? Promise.resolve(blank) : adminApi.show(id)]).then(([list, show]) => {
      const normalized = { ...show, premiereDate: show.premiereDate ?? '', durationMinutes: show.durationMinutes ?? '', productionYear: show.productionYear ?? '', ageRecommendation: show.ageRecommendation ?? '', originalLanguage: show.originalLanguage ?? '', trailerUrl: show.trailerUrl ?? '', videoUrl: show.videoUrl ?? '' }
      setTrailerMode(normalized.trailerUrl && !normalized.trailerUrl.startsWith('/uploads/') ? 'external' : 'local')
      setCategories(list.categories); setForm(normalized); setSaved(JSON.stringify(normalized)); setLoading(false)
    })
  }, [id, isNew])
  useEffect(() => { const warn = e => { if (dirty) { e.preventDefault(); e.returnValue = '' } }; addEventListener('beforeunload', warn); return () => removeEventListener('beforeunload', warn) }, [dirty])

  const field = (key, value) => setForm(current => ({ ...current, [key]: value }))
  const translation = code => form.translations.find(x => x.languageCode === code)
  const changeTranslation = (code, key, value) => setForm(current => ({ ...current, translations: current.translations.map(x => x.languageCode === code ? { ...x, [key]: value } : x) }))
  const payload = () => ({ ...form, translations: form.translations.map(({ slug, ...item }) => item), showCategoryId: Number(form.showCategoryId), durationMinutes: form.durationMinutes ? Number(form.durationMinutes) : null, productionYear: form.productionYear ? Number(form.productionYear) : null, ageRecommendation: form.ageRecommendation !== '' ? Number(form.ageRecommendation) : null, premiereDate: form.premiereDate || null, trailerUrl: form.trailerUrl?.trim() || null, videoUrl: form.videoUrl?.trim() || null })
  const validateBeforeSave = () => {
    if (!form.showCategoryId) return { tab: 'Basic information', message: 'Choose a category before creating the play.' }
    for (const [code, targetTab] of [['sq', 'Albanian content'], ['en', 'English content']]) {
      const item = translation(code)
      if (!item?.title?.trim()) return { tab: targetTab, message: `Enter the ${code === 'sq' ? 'Albanian' : 'English'} title.` }
      if (!item?.shortDescription?.trim()) return { tab: targetTab, message: `Enter the ${code === 'sq' ? 'Albanian' : 'English'} short description.` }
      if (!item?.fullDescription?.trim()) return { tab: targetTab, message: `Enter the ${code === 'sq' ? 'Albanian' : 'English'} synopsis.` }
    }
    const incompleteCredit = draftCredits.find(credit => !credit.personName?.trim() || !credit.creditTypeId)
    if (isNew && incompleteCredit) return { tab: 'Credits', message: 'Every credit needs a person and credit type.' }
    return null
  }
  const apiErrorMessage = error => {
    const errors = error.response?.data?.errors
    if (errors) return Object.values(errors).flat().join(' ')
    return error.response?.data?.detail ?? error.response?.data?.title ?? 'The play could not be saved.'
  }
  const save = async () => {
    const validation = validateBeforeSave()
    if (validation) {
      setTab(validation.tab)
      setToastType('warning')
      setToast(validation.message)
      return null
    }
    try {
      const result = isNew ? await adminApi.createShow(payload()) : await adminApi.saveShow(id, payload())
      if (isNew) {
        if (draftCredits.length) await adminApi.saveShowCredits(result.id, draftCredits.map((credit, index) => ({ ...credit, creditTypeId: Number(credit.creditTypeId), displayOrder: index })))
        for (const media of form.galleryMedia ?? []) await adminApi.attachShowGalleryMedia(result.id, media.id)
      }
      const normalized = { ...result, premiereDate: result.premiereDate ?? '', durationMinutes: result.durationMinutes ?? '', productionYear: result.productionYear ?? '', ageRecommendation: result.ageRecommendation ?? '', originalLanguage: result.originalLanguage ?? '', trailerUrl: result.trailerUrl ?? '', videoUrl: result.videoUrl ?? '' }
      setForm(normalized)
      setSaved(JSON.stringify(normalized))
      setToastType('success')
      setToast('Play saved successfully.')
      if (isNew) navigate('/admin/shows', { replace: true })
      return result
    } catch (error) {
      setToastType('warning')
      setToast(apiErrorMessage(error))
      return null
    }
  }
  const action = async name => {
    if (dirty) await save()
    if (!await dialog.confirm({ title: `${name[0].toUpperCase() + name.slice(1)} play?`, message: `Confirm that you want to ${name} this play.`, confirmLabel: name[0].toUpperCase() + name.slice(1), danger: name === 'archive' })) return
    const result = await adminApi.showAction(id, name); setForm(current => ({ ...current, ...result })); setSaved(JSON.stringify({ ...form, ...result })); setToastType('success'); setToast(`Play ${name}ed.`)
  }
  const deletePlay = async () => {
    if (!await dialog.confirm({ title: 'Delete play and everything associated with it?', message: `“${translation('sq').title || 'This play'}”, all performances, reservations and seat history will be permanently removed. This cannot be undone.`, confirmLabel: 'Delete everything', danger: true })) return
    try {
      await adminApi.deleteShow(id, true)
      navigate('/admin/shows', { replace: true })
    } catch (error) {
      setToastType('warning')
      setToast(apiErrorMessage(error))
    }
  }
  const uploadGallery = async event => {
    const files = Array.from(event.target.files ?? [])
    event.target.value = ''
    if (!files.length) return
    setGalleryBusy(true)
    try {
      let result
      for (const file of files) {
        const media = await adminApi.uploadMedia(file)
        if (isNew) setForm(current => ({ ...current, galleryMedia: [...(current.galleryMedia ?? []), media] }))
        else result = await adminApi.attachShowGalleryMedia(id, media.id)
      }
      if (!isNew && result) setForm(current => ({ ...current, galleryMedia: result.galleryMedia }))
      setToastType('success')
      setToast(`${files.length} gallery image${files.length === 1 ? '' : 's'} uploaded.`)
    } catch (e) {
      setToastType('warning')
      setToast(e.response?.data?.detail ?? 'The gallery image could not be uploaded.')
    } finally {
      setGalleryBusy(false)
    }
  }
  const chooseGalleryMedia = async media => {
    if (!media || form.galleryMedia?.some(item => item.id === media.id)) return
    if (isNew) {
      setForm(current => ({ ...current, galleryMedia: [...(current.galleryMedia ?? []), media] }))
      return
    }
    const result = await adminApi.attachShowGalleryMedia(id, media.id)
    setForm(current => ({ ...current, galleryMedia: result.galleryMedia }))
  }
  const editGalleryItem = async item => {
    const captionSq = await dialog.prompt({ title: 'Edit Albanian caption', label: 'Albanian caption', defaultValue: item.captionSq ?? '', confirmLabel: 'Continue' })
    if (captionSq === null) return
    const captionEn = await dialog.prompt({ title: 'Edit English caption', label: 'English caption', defaultValue: item.captionEn ?? '', confirmLabel: 'Save captions' })
    if (captionEn === null) return
    await adminApi.saveMedia(item.id, { fileName: item.fileName, altTextSq: captionSq, altTextEn: captionEn, captionSq, captionEn })
    setForm(current => ({ ...current, galleryMedia: current.galleryMedia.map(x => x.id === item.id ? { ...x, captionSq, captionEn } : x) }))
    setToastType('success')
    setToast('Gallery captions updated.')
  }
  const removeGalleryItem = async item => {
    if (!await dialog.confirm({ title: 'Remove gallery image?', message: `“${item.fileName}” will be removed from this play. The Media Library file will be kept.`, confirmLabel: 'Remove image', danger: true })) return
    if (!isNew) await adminApi.detachShowGalleryMedia(id, item.id)
    setForm(current => ({ ...current, galleryMedia: current.galleryMedia.filter(x => x.id !== item.id) }))
    setToastType('success')
    setToast('Image removed from this play.')
  }
  if (loading) return <LoadingSkeleton rows={7} />
  const effectiveTrailer = form.trailerUrl
  const contentFields = code => <div className="form-grid"><label>Title *<input required value={translation(code).title} onChange={e => changeTranslation(code, 'title', e.target.value)} /></label><label className="full">Short description *<textarea rows="4" required maxLength="700" value={translation(code).shortDescription} onChange={e => changeTranslation(code, 'shortDescription', e.target.value)} /></label><label className="full">Synopsis *<textarea rows="12" required value={translation(code).fullDescription} onChange={e => changeTranslation(code, 'fullDescription', e.target.value)} /></label></div>

  return <><PageHeader eyebrow="Shows / Plays" title={isNew ? 'Create play' : translation('sq').title || 'Edit play'} description="Manage bilingual content, production information, media and publication state." actions={<><Link className="admin-outline-button" to="/admin/shows">Back to plays</Link>{!isNew && <StatusBadge status={form.status} />}</>} />
    <div className="editor-tabs" role="tablist">{tabs.map(x => <button type="button" className={tab === x ? 'active' : ''} onClick={() => setTab(x)} key={x}>{x}</button>)}</div>
    <form className="admin-form" onSubmit={e => { e.preventDefault(); save() }}>
      {tab === 'Basic information' && <section className="admin-panel"><h2>Basic information</h2><div className="form-grid"><label>Category *<select required value={form.showCategoryId} onChange={e => field('showCategoryId', e.target.value)}><option value="">Select category</option>{categories.map(x => <option value={x.id} key={x.id}>{x.label}</option>)}</select></label><label>Lifecycle status<select value={form.lifecycleStatus} onChange={e => field('lifecycleStatus', e.target.value)}><option>Upcoming</option><option>Active</option><option>Completed</option><option>SoldOut</option></select></label><label>Premiere date<input type="date" value={form.premiereDate} onChange={e => field('premiereDate', e.target.value)} /></label><label>Production year<input type="number" min="1900" max="2200" value={form.productionYear} onChange={e => field('productionYear', e.target.value)} /></label><label>Duration (minutes)<input type="number" min="1" max="600" value={form.durationMinutes} onChange={e => field('durationMinutes', e.target.value)} /></label><label>Age recommendation<input type="number" min="0" max="21" value={form.ageRecommendation} onChange={e => field('ageRecommendation', e.target.value)} /></label><label>Original language<input value={form.originalLanguage} onChange={e => field('originalLanguage', e.target.value)} /></label><label className="admin-switch-row"><input type="checkbox" checked={form.isFeatured} onChange={e => field('isFeatured', e.target.checked)} /> Feature this play on the website</label></div></section>}
      {tab === 'Basic information' && <section className="admin-panel"><h2>Performance ownership</h2><label className="admin-switch-row"><input type="checkbox" checked={form.isGuestPerformance} onChange={e => field('isGuestPerformance', e.target.checked)} /> Guest performance (Shfaqje mysafire)</label><p className="admin-help">Guest plays still use our theatres and the same internal, link, or phone reservation methods.</p></section>}
      {tab === 'Albanian content' && <section className="admin-panel"><LanguageTabs active="sq" onChange={code => setTab(code === 'sq' ? 'Albanian content' : 'English content')} />{contentFields('sq')}</section>}
      {tab === 'English content' && <section className="admin-panel"><LanguageTabs active="en" onChange={code => setTab(code === 'sq' ? 'Albanian content' : 'English content')} />{contentFields('en')}</section>}
      {tab === 'Credits' && <section className="admin-panel">{isNew ? <><p className="admin-help">These credits will be saved together with the new play.</p><CreditRepeater value={draftCredits} onChange={setDraftCredits} /></> : <CreditRepeater showId={id} onSaved={() => { setToastType('success'); setToast('Credits saved successfully.') }} />}</section>}
      {tab === 'Media' && isNew && <section className="admin-panel new-show-gallery-tools"><div className="panel-heading"><div><h2>Gallery images</h2><p>Choose images you already uploaded, or upload new pictures. They will be attached when you create the play.</p></div><label className={`admin-primary-button ${galleryBusy ? 'disabled' : ''}`}>{galleryBusy ? 'Uploading…' : '+ Upload images'}<input type="file" accept="image/jpeg,image/png,image/webp" multiple hidden disabled={galleryBusy} onChange={uploadGallery} /></label></div><MediaPicker label="Choose from Media Library" type="image/" onChange={() => {}} onSelect={chooseGalleryMedia} /></section>}
      {tab === 'Media' && <section className="admin-panel show-media-editor"><div className="panel-heading"><div><h2>Play media</h2><p>The poster is the main public image. Add a trailer and review gallery images already attached to this play.</p></div></div>
        <div className="show-media-primary"><div className="show-media-poster-preview">{form.posterUrl ? <img src={resolveMediaUrl(form.posterUrl)} alt="" /> : <span>No poster available</span>}</div><div><MediaPicker label="Main poster" value={form.posterMediaAssetId} currentUrl={form.posterUrl} onChange={value => field('posterMediaAssetId', value)} onSelect={item => field('posterUrl', item?.fileUrl ?? null)} /><MediaPicker label="Featured / hero image" value={form.featuredMediaAssetId} currentUrl={form.featuredImageUrl} onChange={value => field('featuredMediaAssetId', value)} onSelect={item => field('featuredImageUrl', item?.fileUrl ?? null)} /></div></div>
        <div className="show-media-block"><h3>Trailer</h3><div className="view-switch"><button type="button" className={trailerMode === 'local' ? 'active' : ''} onClick={() => { setTrailerMode('local'); field('trailerUrl', '') }}>Local video</button><button type="button" className={trailerMode === 'external' ? 'active' : ''} onClick={() => { setTrailerMode('external'); setLocalTrailerMediaId(null); field('trailerUrl', '') }}>External video link</button></div>
          {trailerMode === 'local' ? <MediaPicker label="Trailer from Media Library" type="video/" value={localTrailerMediaId} currentUrl={form.trailerUrl} onChange={setLocalTrailerMediaId} onSelect={item => field('trailerUrl', item?.fileUrl ?? '')} /> : <label>Trailer URL<input type="url" placeholder="https://www.youtube.com/watch?v=…" value={form.trailerUrl} onChange={e => field('trailerUrl', e.target.value)} /></label>}
          {effectiveTrailer && <div className="show-trailer-admin-preview">{trailerMode === 'local' ? <video src={resolveMediaUrl(form.trailerUrl)} controls /> : <a href={form.trailerUrl} target="_blank" rel="noreferrer">Open external trailer ↗</a>}</div>}
        </div>
        <div className="show-media-block"><div className="show-gallery-admin-heading"><h3>Gallery <small>{form.galleryMedia?.length ?? 0} items</small></h3><div className="show-gallery-heading-actions">{!isNew && <label className={`admin-primary-button ${galleryBusy ? 'disabled' : ''}`}>{galleryBusy ? 'Uploading…' : '+ Upload more'}<input type="file" accept="image/jpeg,image/png,image/webp" multiple hidden disabled={galleryBusy} onChange={uploadGallery} /></label>}</div></div>{form.galleryMedia?.length ? <><p className="admin-help">Every picture in this gallery is database-managed and editable.</p><div className="show-admin-gallery">{form.galleryMedia.map(item => <figure key={`database-${item.id}`}><img src={resolveMediaUrl(item.fileUrl)} alt={item.captionSq ?? ''} /><figcaption>{item.captionSq || item.fileName}</figcaption><div className="show-gallery-item-actions"><button type="button" onClick={() => editGalleryItem(item)}>Edit</button><button type="button" className="danger" onClick={() => removeGalleryItem(item)}>Remove</button></div></figure>)}</div></> : <div className="admin-empty"><strong>No gallery attached</strong><p>Use “Upload more” to add the first gallery images.</p></div>}</div>
      </section>}
      {tab === 'Media' && isNew && <div className="new-show-gallery-bottom"><label className={`admin-primary-button ${galleryBusy ? 'disabled' : ''}`}>{galleryBusy ? 'Uploading…' : '+ Upload gallery images'}<input type="file" accept="image/jpeg,image/png,image/webp" multiple hidden disabled={galleryBusy} onChange={uploadGallery} /></label><MediaPicker label="Or choose an image from Media Library" type="image/" onChange={() => {}} onSelect={chooseGalleryMedia} /></div>}
      {tab === 'Performances' && <section className="admin-panel editor-coming-next"><h2>Performances</h2><p>Performance dates will be managed here after the performance table and calendar module is connected.</p></section>}
      {tab === 'SEO' && <section className="admin-panel"><h2>Search appearance</h2>{['sq', 'en'].map(code => <div className="seo-language" key={code}><strong>{code === 'sq' ? 'Albanian' : 'English'}</strong><div className="form-grid"><label>Meta title<input maxLength="220" value={translation(code).metaTitle ?? ''} onChange={e => changeTranslation(code, 'metaTitle', e.target.value)} /></label><label>Meta description<textarea rows="3" maxLength="320" value={translation(code).metaDescription ?? ''} onChange={e => changeTranslation(code, 'metaDescription', e.target.value)} /></label></div></div>)}</section>}
      {tab === 'Publication' && <section className="admin-panel"><h2>Publication settings</h2><div className="publication-state"><StatusBadge status={form.status ?? 'Draft'} /><p>{form.status === 'Published' ? 'This play is visible on the public website.' : form.status === 'Archived' ? 'This historical play is archived.' : 'This play is private and can be reviewed before publishing.'}</p></div>{!isNew && <div className="publication-actions">{form.status !== 'Published' && <button type="button" className="admin-primary-button" onClick={() => action('publish')}>Publish play</button>}{form.status === 'Published' && <button type="button" className="admin-outline-button" onClick={() => action('unpublish')}>Unpublish</button>}{form.status !== 'Archived' ? <button type="button" className="admin-danger-button" onClick={() => action('archive')}>Archive play</button> : <button type="button" className="admin-outline-button" onClick={() => action('restore')}>Restore as draft</button>}<button type="button" className="admin-text-button" onClick={() => adminApi.showAction(id, 'duplicate').then(x => navigate(`/admin/shows/${x.id}`))}>Duplicate</button><button type="button" className="admin-danger-button" onClick={deletePlay}>Delete permanently</button></div>}</section>}
      <div className="sticky-save"><button className="admin-primary-button" type="submit" disabled={!dirty}>{isNew ? 'Create play' : 'Save changes'}</button><span className={dirty ? 'unsaved-chip' : 'saved-chip'}>{dirty ? 'Unsaved changes' : 'All changes saved'}</span>{!isNew && <a className="admin-outline-button" href={`#/sq/shfaqjet/${translation('sq').slug}`} target="_blank">Preview →</a>}</div>
    </form><Toast message={toast} type={toastType} onClose={() => setToast('')} /></>
}
