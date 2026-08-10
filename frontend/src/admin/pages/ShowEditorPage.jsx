import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { adminApi } from '../api'
import { LanguageTabs, LoadingSkeleton, PageHeader, StatusBadge, Toast } from '../components/AdminUi'
import { CreditRepeater } from '../components/CreditRepeater'
import { MediaPicker } from '../components/MediaPicker'
import { resolveMediaUrl } from '../../api/client'
import { useAdminDialog } from '../components/AdminDialog'
import { useAdminLanguage } from '../AdminLanguageContext'

const blankTranslation = code => ({ languageCode: code, title: '', slug: '', shortDescription: '', fullDescription: '', metaTitle: '', metaDescription: '' })
const blank = { showCategoryId: '', posterMediaAssetId: null, featuredMediaAssetId: null, durationMinutes: '', productionYear: '', ageRecommendation: '', originalLanguage: '', trailerUrl: '', videoUrl: '', premiereDate: '', lifecycleStatus: 'Upcoming', isFeatured: false, isGuestPerformance: false, galleryMedia: [], translations: [blankTranslation('sq'), blankTranslation('en')] }
const tabs = ['Basic information', 'Albanian content', 'English content', 'Credits', 'Media', 'Publication']

export function ShowEditorPage() {
  const { t } = useAdminLanguage()
  const dialog = useAdminDialog()
  const { id } = useParams()
  const isNew = id === 'new'
  const navigate = useNavigate()
  const [editorSearchParams] = useSearchParams()
  const [form, setForm] = useState(blank)
  const [saved, setSaved] = useState('')
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
    const newPlay = isNew ? { ...blank, isGuestPerformance: editorSearchParams.get('guest') === 'true' } : null
    Promise.all([adminApi.shows({ pageSize: 1 }), isNew ? Promise.resolve(newPlay) : adminApi.show(id)]).then(([list, show]) => {
      const normalized = { ...show, showCategoryId: show.showCategoryId || list.categories[0]?.id || '', premiereDate: show.premiereDate ?? '', durationMinutes: show.durationMinutes ?? '', productionYear: show.productionYear ?? '', ageRecommendation: show.ageRecommendation ?? '', originalLanguage: show.originalLanguage ?? '', trailerUrl: show.trailerUrl ?? '', videoUrl: show.videoUrl ?? '' }
      setTrailerMode(normalized.trailerUrl && !normalized.trailerUrl.startsWith('/uploads/') ? 'external' : 'local')
      setForm(normalized); setSaved(JSON.stringify(normalized)); setLoading(false)
    })
  }, [id, isNew])
  useEffect(() => { const warn = e => { if (dirty) { e.preventDefault(); e.returnValue = '' } }; addEventListener('beforeunload', warn); return () => removeEventListener('beforeunload', warn) }, [dirty])

  const field = (key, value) => setForm(current => ({ ...current, [key]: value }))
  const translation = code => form.translations.find(x => x.languageCode === code)
  const changeTranslation = (code, key, value) => setForm(current => ({ ...current, translations: current.translations.map(x => x.languageCode === code ? { ...x, [key]: value } : x) }))
  const payload = () => ({ ...form, translations: form.translations.map(item => ({ languageCode: item.languageCode, title: item.title, shortDescription: item.shortDescription, fullDescription: item.fullDescription })), showCategoryId: Number(form.showCategoryId), durationMinutes: form.durationMinutes ? Number(form.durationMinutes) : null, productionYear: form.productionYear ? Number(form.productionYear) : null, ageRecommendation: form.ageRecommendation !== '' ? Number(form.ageRecommendation) : null, premiereDate: form.premiereDate || null, trailerUrl: form.trailerUrl?.trim() || null, videoUrl: form.videoUrl?.trim() || null })
  const validateBeforeSave = () => {
    if (!form.showCategoryId) return { tab: 'Basic information', message: 'No active repertoire category is configured. Please contact an administrator.' }
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
    if (dirty && !await save()) return
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
  const contentFields = code => <div className="form-grid"><label>{t('Title *')}<input required value={translation(code).title} onChange={e => changeTranslation(code, 'title', e.target.value)} /></label><label className="full">{t('Short description *')}<textarea rows="4" required maxLength="700" value={translation(code).shortDescription} onChange={e => changeTranslation(code, 'shortDescription', e.target.value)} /></label><label className="full">{t('Synopsis *')}<textarea rows="12" required value={translation(code).fullDescription} onChange={e => changeTranslation(code, 'fullDescription', e.target.value)} /></label></div>
  const publicationChecks = [
    { label: 'Basic information and play type', complete: Boolean(form.showCategoryId), tab: 'Basic information' },
    { label: 'Albanian title, description and synopsis', complete: Boolean(translation('sq')?.title?.trim() && translation('sq')?.shortDescription?.trim() && translation('sq')?.fullDescription?.trim()), tab: 'Albanian content' },
    { label: 'English title, description and synopsis', complete: Boolean(translation('en')?.title?.trim() && translation('en')?.shortDescription?.trim() && translation('en')?.fullDescription?.trim()), tab: 'English content' },
    { label: 'Main poster selected', complete: Boolean(form.posterMediaAssetId || form.posterUrl), tab: 'Media', recommended: true },
  ]
  const publicationReady = publicationChecks.filter(item => !item.recommended).every(item => item.complete)

  return <><PageHeader eyebrow="Shows / Plays" title={isNew ? 'Create play' : translation('sq').title || 'Edit play'} description="Manage bilingual content, production information, media and publication state." actions={<><Link className="admin-outline-button" to="/admin/shows">{t('Back to plays')}</Link>{!isNew && <StatusBadge status={form.status} />}</>} />
    <div className="editor-tabs" role="tablist">{tabs.map(x => <button type="button" className={tab === x ? 'active' : ''} onClick={() => setTab(x)} key={x}>{t(x)}</button>)}</div>
    <form className={`admin-form${tab === 'English content' ? ' language-en' : tab === 'Albanian content' ? ' language-sq' : ''}`} onSubmit={e => { e.preventDefault(); save() }}>
      {tab === 'Basic information' && <section className="admin-panel"><h2>{t('Basic information')}</h2><div className="form-grid"><label>{t('Play type *')}<select required value={form.isGuestPerformance ? 'guest' : 'ours'} onChange={e => field('isGuestPerformance', e.target.value === 'guest')}><option value="ours">{t('Our production')}</option><option value="guest">{t('Guest play')}</option></select><small>{t('Choose whether this is produced by AAB Theatre or hosted as a guest performance.')}</small></label><label>{t('Lifecycle status')}<select value={form.lifecycleStatus} onChange={e => field('lifecycleStatus', e.target.value)}><option value="Upcoming">{t('Upcoming')}</option><option value="Active">{t('Active')}</option><option value="Completed">{t('Completed')}</option><option value="SoldOut">{t('Sold out')}</option></select></label><label>{t('Premiere date')}<input type="date" value={form.premiereDate} onChange={e => field('premiereDate', e.target.value)} /></label><label>{t('Production year')}<input type="number" min="1900" max="2200" value={form.productionYear} onChange={e => field('productionYear', e.target.value)} /></label><label>{t('Duration (minutes)')}<input type="number" min="1" max="600" value={form.durationMinutes} onChange={e => field('durationMinutes', e.target.value)} /></label><label>{t('Age recommendation')}<input type="number" min="0" max="21" value={form.ageRecommendation} onChange={e => field('ageRecommendation', e.target.value)} /></label><label>{t('Original language')}<input value={form.originalLanguage} onChange={e => field('originalLanguage', e.target.value)} /></label><label className="admin-switch-row"><input type="checkbox" checked={form.isFeatured} onChange={e => field('isFeatured', e.target.checked)} /> {t('Feature this play on the website')}</label></div></section>}
      {tab === 'Albanian content' && <section className="admin-panel show-language-panel"><LanguageTabs active="sq" onChange={code => setTab(code === 'sq' ? 'Albanian content' : 'English content')} />{contentFields('sq')}</section>}
      {tab === 'English content' && <section className="admin-panel show-language-panel"><LanguageTabs active="en" onChange={code => setTab(code === 'sq' ? 'Albanian content' : 'English content')} />{contentFields('en')}</section>}
      {tab === 'Credits' && <section className="admin-panel">{isNew ? <><p className="admin-help">{t('These credits will be saved together with the new play.')}</p><CreditRepeater value={draftCredits} onChange={setDraftCredits} /></> : <CreditRepeater showId={id} onSaved={() => { setToastType('success'); setToast('Credits saved successfully.') }} />}</section>}
      {tab === 'Media' && <section className="admin-panel show-media-editor"><div className="panel-heading"><div><h2>{t('Play media')}</h2><p>{t('The poster is the main public image. Add a trailer and review gallery images already attached to this play.')}</p></div></div>
        <div className="show-media-primary"><div className="show-media-poster-preview">{form.posterUrl ? <img src={resolveMediaUrl(form.posterUrl)} alt="" /> : <span>{t('No poster available')}</span>}</div><div><MediaPicker label={t('Main poster')} value={form.posterMediaAssetId} currentUrl={form.posterUrl} onChange={value => field('posterMediaAssetId', value)} onSelect={item => field('posterUrl', item?.fileUrl ?? null)} /><MediaPicker label={t('Featured / hero image')} value={form.featuredMediaAssetId} currentUrl={form.featuredImageUrl} onChange={value => field('featuredMediaAssetId', value)} onSelect={item => field('featuredImageUrl', item?.fileUrl ?? null)} /></div></div>
        <div className="show-media-block"><h3>{t('Trailer')}</h3><div className="view-switch"><button type="button" className={trailerMode === 'local' ? 'active' : ''} onClick={() => { setTrailerMode('local'); field('trailerUrl', '') }}>{t('Local video')}</button><button type="button" className={trailerMode === 'external' ? 'active' : ''} onClick={() => { setTrailerMode('external'); setLocalTrailerMediaId(null); field('trailerUrl', '') }}>{t('External video link')}</button></div>
          {trailerMode === 'local' ? <MediaPicker label={t('Trailer from Media Library')} type="video/" value={localTrailerMediaId} currentUrl={form.trailerUrl} onChange={setLocalTrailerMediaId} onSelect={item => field('trailerUrl', item?.fileUrl ?? '')} /> : <label>{t('Trailer URL')}<input type="url" placeholder="https://www.youtube.com/watch?v=…" value={form.trailerUrl} onChange={e => field('trailerUrl', e.target.value)} /></label>}
          {effectiveTrailer && <div className="show-trailer-admin-preview">{trailerMode === 'local' ? <video src={resolveMediaUrl(form.trailerUrl)} controls /> : <a href={form.trailerUrl} target="_blank" rel="noreferrer">{t('Open external trailer')} ↗</a>}</div>}
        </div>
        <div className="show-media-block"><div className="show-gallery-admin-heading"><h3>{t('Gallery')} <small>{form.galleryMedia?.length ?? 0} {t('items')}</small></h3><div className="show-gallery-heading-actions"><label className={`admin-primary-button ${galleryBusy ? 'disabled' : ''}`}>{t(galleryBusy ? 'Uploading…' : isNew ? '+ Upload images' : '+ Upload more')}<input type="file" accept="image/jpeg,image/png,image/webp" multiple hidden disabled={galleryBusy} onChange={uploadGallery} /></label></div></div><p className="admin-help">{t('Add pictures after the trailer by uploading files or choosing existing images.')}</p>{isNew && <MediaPicker label={t('Choose from Media Library')} type="image/" onChange={() => {}} onSelect={chooseGalleryMedia} />}{form.galleryMedia?.length ? <div className="show-admin-gallery">{form.galleryMedia.map(item => <figure key={`database-${item.id}`}><img src={resolveMediaUrl(item.fileUrl)} alt={item.captionSq ?? ''} /><figcaption>{item.captionSq || item.fileName}</figcaption><div className="show-gallery-item-actions"><button type="button" onClick={() => editGalleryItem(item)}>{t('Edit')}</button><button type="button" className="danger" onClick={() => removeGalleryItem(item)}>{t('Remove')}</button></div></figure>)}</div> : <div className="admin-empty"><strong>{t('No gallery attached')}</strong><p>{t('Add the first gallery images using either option above.')}</p></div>}</div>
      </section>}
      {tab === 'Publication' && <><section className="admin-panel"><div className="panel-heading"><div><h2>Ready to publish?</h2><p>Complete the required content below. Search metadata and the page address are generated automatically when you save.</p></div><StatusBadge status={form.status ?? 'Draft'} /></div><div className="publication-checklist">{publicationChecks.map(item => <button type="button" className={item.complete ? 'complete' : 'incomplete'} onClick={() => setTab(item.tab)} key={item.label}><span>{item.complete ? '✓' : '!'}</span><strong>{item.label}</strong><small>{item.complete ? 'Ready' : item.recommended ? 'Recommended' : 'Required'}</small></button>)}</div><div className="publication-state"><StatusBadge status={form.status ?? 'Draft'} /><p>{form.status === 'Published' ? 'This play is visible on the public website.' : form.status === 'Archived' ? 'This historical play is archived and hidden from the public repertoire.' : isNew ? 'Create the play first, then return here to publish it and add performance dates.' : publicationReady ? 'Required content is ready. You can publish this play.' : 'Complete the required items before publishing.'}</p></div>{!isNew && <div className="publication-actions">{form.status !== 'Published' && <button type="button" className="admin-primary-button" disabled={!publicationReady} onClick={() => action('publish')}>Publish play</button>}{form.status === 'Published' && <button type="button" className="admin-outline-button" onClick={() => action('unpublish')}>Unpublish</button>}<Link className="admin-outline-button" to="/admin/performances">Manage performances</Link>{form.status !== 'Archived' ? <button type="button" className="admin-danger-button" onClick={() => action('archive')}>Archive play</button> : <button type="button" className="admin-outline-button" onClick={() => action('restore')}>Restore as draft</button>}<button type="button" className="admin-text-button" onClick={() => adminApi.showAction(id, 'duplicate').then(x => navigate(`/admin/shows/${x.id}`))}>Duplicate</button><button type="button" className="admin-danger-button" onClick={deletePlay}>Delete permanently</button></div>}</section></>}
      <div className="sticky-save"><button className="admin-primary-button" type="submit" disabled={!dirty}>{t(isNew ? 'Create play' : 'Save changes')}</button><span className={dirty ? 'unsaved-chip' : 'saved-chip'}>{t(dirty ? 'Unsaved changes' : 'All changes saved')}</span>{!isNew && <a className="admin-outline-button" href={`#/sq/shfaqjet/${translation('sq').slug}`} target="_blank">{t('Preview')} →</a>}</div>
    </form><Toast message={toast} type={toastType} onClose={() => setToast('')} /></>
}
