import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { adminApi } from '../api'
import { LanguageTabs, LoadingSkeleton, PageHeader, StatusBadge, Toast } from '../components/AdminUi'
import { MediaPicker } from '../components/MediaPicker'
import { RichTextEditor } from '../components/RichTextEditor'
import { useAdminDialog } from '../components/AdminDialog'
import { resolveMediaUrl } from '../../api/client'

const emptyTranslation = languageCode => ({
  languageCode,
  title: '',
  slug: '',
  summary: '',
  content: '',
  metaTitle: '',
  metaDescription: '',
})

const makeSlug = value => value
  .normalize('NFD')
  .replace(/[\u0300-\u036f]/g, '')
  .toLowerCase()
  .replace(/[^a-z0-9]+/g, '-')
  .replace(/^-+|-+$/g, '')

const makeSeoDescription = value => value
  .replace(/<[^>]*>/g, ' ')
  .replace(/\s+/g, ' ')
  .trim()
  .slice(0, 160)

const withBothTranslations = article => ({
  ...article,
  translations: ['sq', 'en'].map(languageCode => {
    const translation = {
      ...emptyTranslation(languageCode),
      ...(article.translations?.find(item => item.languageCode === languageCode) ?? {}),
    }
    return {
      ...translation,
      slug: translation.slug || makeSlug(translation.title),
      metaTitle: translation.metaTitle || translation.title,
      metaDescription: translation.metaDescription || makeSeoDescription(translation.summary),
    }
  }),
})

export function NewsEditorPage() {
  const dialog = useAdminDialog()
  const { id } = useParams()
  const navigate = useNavigate()
  const isNew = !id
  const [form, setForm] = useState(null)
  const [saved, setSaved] = useState('')
  const [lang, setLang] = useState('sq')
  const [preview, setPreview] = useState(false)
  const [toast, setToast] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [galleryBusy, setGalleryBusy] = useState(false)
  const [draggedMediaId, setDraggedMediaId] = useState(null)
  useEffect(() => {
    if (isNew) {
      const empty = {
        articleType: 'Authored', coverMediaAssetId: null, coverUrl: null,
        externalUrl: null, externalSourceName: null, isPublished: false,
        isFeatured: false, publishedAt: null,
        translations: ['sq', 'en'].map(emptyTranslation),
      }
      setForm(empty)
      setSaved(JSON.stringify(empty))
      return
    }
    adminApi.newsArticle(id)
      .then(x => {
        const normalized = withBothTranslations(x)
        setForm(normalized)
        setSaved(JSON.stringify(normalized))
      })
      .catch(e => setError(e.response?.data?.detail ?? 'The news article could not be loaded.'))
  }, [id, isNew])
  const dirty = useMemo(() => form && JSON.stringify(form) !== saved, [form, saved])
  useEffect(() => { const warn = e => { if (dirty) { e.preventDefault(); e.returnValue = '' } }; addEventListener('beforeunload', warn); return () => removeEventListener('beforeunload', warn) }, [dirty])
  if (error && !form) return <section className="admin-panel admin-request-error"><div>!</div><h2>Article unavailable</h2><p>{error}</p><Link className="admin-outline-button" to="/admin/news">Back to news</Link></section>
  if (!form) return <LoadingSkeleton rows={7} />
  const tr = form.translations.find(x => x.languageCode === lang) ?? emptyTranslation(lang)
  const change = (key, value) => setForm({ ...form, [key]: value })
  const changeTr = (key, value) => setForm(current => ({
    ...current,
    translations: current.translations.map(item => {
      if (item.languageCode !== lang) return item
      const next = { ...item, [key]: value }
      if (key === 'title') {
        if (!item.slug || item.slug === makeSlug(item.title)) next.slug = makeSlug(value)
        if (!item.metaTitle || item.metaTitle === item.title) next.metaTitle = value
      }
      if (key === 'summary') {
        if (!item.metaDescription || item.metaDescription === makeSeoDescription(item.summary)) {
          next.metaDescription = makeSeoDescription(value)
        }
      }
      return next
    }),
  }))
  const save = async e => {
    e?.preventDefault()
    setBusy(true)
    setError('')
    try {
      let result = isNew
        ? await adminApi.createNewsArticle(form)
        : await adminApi.saveNewsArticle(id, form)
      if (isNew && form.galleryMedia?.length) {
        for (const media of form.galleryMedia) {
          result = await adminApi.attachNewsGalleryMedia(result.id, media.id)
        }
        const thumbnail = form.galleryMedia.find(item => item.isThumbnail) ?? form.galleryMedia[0]
        if (thumbnail) result = await adminApi.setNewsThumbnail(result.id, thumbnail.id)
      }
      const normalized = withBothTranslations(result)
      setForm(normalized)
      setSaved(JSON.stringify(normalized))
      setToast(isNew ? 'News article created.' : 'News article saved safely.')
      if (isNew) navigate(`/admin/news/${result.id}`, { replace: true })
    } catch (e) {
      setError(e.response?.data?.detail ?? e.response?.data?.title ?? 'The article could not be saved.')
    } finally {
      setBusy(false)
    }
  }
  const remove = async () => {
    if (!await dialog.confirm({ title: 'Delete news article?', message: `“${tr.title}” and its gallery association will be removed. Media Library files will be kept.`, confirmLabel: 'Delete article', danger: true })) return
    setBusy(true)
    setError('')
    try {
      await adminApi.deleteNewsArticle(id)
      navigate('/admin/news', { replace: true })
    } catch (e) {
      setError(e.response?.data?.detail ?? 'The article could not be deleted.')
      setBusy(false)
    }
  }
  const updatePersistedGallery = result => {
    const mediaState = {
      galleryMedia: result.galleryMedia ?? [],
      coverMediaAssetId: result.coverMediaAssetId,
      coverUrl: result.coverUrl,
    }
    setForm(current => ({ ...current, ...mediaState }))
    setSaved(current => {
      const savedForm = current ? JSON.parse(current) : {}
      return JSON.stringify({ ...savedForm, ...mediaState })
    })
  }
  const uploadGallery = async event => {
    const files = Array.from(event.target.files ?? [])
    event.target.value = ''
    if (!files.length) return
    setGalleryBusy(true)
    setError('')
    try {
      for (const file of files) {
        const media = await adminApi.uploadMedia(file)
        if (isNew) {
          setForm(current => {
            const galleryMedia = current.galleryMedia ?? []
            return {
              ...current,
              coverMediaAssetId: current.coverMediaAssetId ?? media.id,
              coverUrl: current.coverUrl ?? media.fileUrl,
              galleryMedia: [...galleryMedia, { ...media, isThumbnail: galleryMedia.length === 0 }],
            }
          })
        } else {
          updatePersistedGallery(await adminApi.attachNewsGalleryMedia(id, media.id))
        }
      }
      setToast(`${files.length} gallery image${files.length === 1 ? '' : 's'} uploaded.`)
    } catch (e) {
      setError(e.response?.data?.detail ?? 'The gallery images could not be uploaded.')
    } finally {
      setGalleryBusy(false)
    }
  }
  const chooseGalleryMedia = async media => {
    if (!media || form.galleryMedia?.some(item => item.id === media.id)) return
    if (isNew) {
      setForm(current => {
        const galleryMedia = current.galleryMedia ?? []
        return {
          ...current,
          coverMediaAssetId: current.coverMediaAssetId ?? media.id,
          coverUrl: current.coverUrl ?? media.fileUrl,
          galleryMedia: [...galleryMedia, { ...media, isThumbnail: galleryMedia.length === 0 }],
        }
      })
    } else {
      updatePersistedGallery(await adminApi.attachNewsGalleryMedia(id, media.id))
    }
  }
  const setThumbnail = async media => {
    if (isNew) {
      setForm(current => {
        const selected = current.galleryMedia.find(item => item.id === media.id)
        const remaining = current.galleryMedia.filter(item => item.id !== media.id)
        return {
          ...current,
          coverMediaAssetId: media.id,
          coverUrl: media.fileUrl,
          galleryMedia: [
            { ...selected, isThumbnail: true },
            ...remaining.map(item => ({ ...item, isThumbnail: false })),
          ],
        }
      })
    } else {
      const selectedResult = await adminApi.setNewsThumbnail(id, media.id)
      const thumbnail = selectedResult.galleryMedia.find(item => item.id === media.id)
      const ordered = [
        thumbnail,
        ...selectedResult.galleryMedia.filter(item => item.id !== media.id),
      ].filter(Boolean)
      const orderedResult = await adminApi.reorderNewsGallery(id, ordered.map(item => item.id))
      updatePersistedGallery(orderedResult)
    }
  }
  const removeGalleryMedia = async media => {
    if (!await dialog.confirm({ title: 'Remove gallery image?', message: `“${media.fileName}” will be removed from this article. The Media Library file will be kept.`, confirmLabel: 'Remove image', danger: true })) return
    if (isNew) {
      setForm(current => {
        const galleryMedia = current.galleryMedia.filter(item => item.id !== media.id)
        const thumbnail = galleryMedia.find(item => item.isThumbnail) ?? galleryMedia[0]
        return {
          ...current,
          galleryMedia: galleryMedia.map(item => ({ ...item, isThumbnail: item.id === thumbnail?.id })),
          coverMediaAssetId: thumbnail?.id ?? null,
          coverUrl: thumbnail?.fileUrl ?? null,
        }
      })
    } else {
      updatePersistedGallery(await adminApi.detachNewsGalleryMedia(id, media.id))
    }
  }
  const reorderGallery = async targetMediaId => {
    if (!draggedMediaId || draggedMediaId === targetMediaId) return
    const current = form.galleryMedia ?? []
    const fromIndex = current.findIndex(item => item.id === draggedMediaId)
    const toIndex = current.findIndex(item => item.id === targetMediaId)
    if (fromIndex < 0 || toIndex < 0) return
    const ordered = [...current]
    const [moved] = ordered.splice(fromIndex, 1)
    ordered.splice(toIndex, 0, moved)
    setForm(value => ({ ...value, galleryMedia: ordered }))
    setDraggedMediaId(null)
    if (!isNew) {
      try {
        updatePersistedGallery(await adminApi.reorderNewsGallery(id, ordered.map(item => item.id)))
        setToast('Gallery order saved.')
      } catch (e) {
        setError(e.response?.data?.detail ?? 'The gallery order could not be saved.')
      }
    }
  }
  const thumbnailMedia = form.galleryMedia?.find(item => item.isThumbnail)
    ?? form.galleryMedia?.find(item => item.id === form.coverMediaAssetId)
    ?? form.galleryMedia?.[0]
  const sortableGalleryMedia = (form.galleryMedia ?? []).filter(item => item.id !== thumbnailMedia?.id)
  return <><PageHeader eyebrow="News" title={tr.title || 'New article'} description={isNew ? 'Create a bilingual news article for the public website.' : 'Edit bilingual article content, publication settings, source information and search metadata.'} actions={<><Link className="admin-outline-button" to="/admin/news">Back to news</Link><StatusBadge status={form.isPublished ? 'Published' : 'Draft'} /></>} />
    <form className={`admin-form news-editor-form language-${lang}`} onSubmit={save}><section className="admin-panel"><div className="news-editor-top"><LanguageTabs active={lang} onChange={setLang} /><button type="button" className="admin-text-button" onClick={() => setPreview(!preview)}>{preview ? 'Return to editor' : 'Preview article'}</button></div>
      {preview ? <article className="news-admin-preview"><h1>{tr.title}</h1><p>{tr.summary}</p><div dangerouslySetInnerHTML={{ __html: tr.content }} /></article> : <div className="form-grid"><label>Title *<input required value={tr.title} onChange={e => changeTr('title', e.target.value)} /></label><label>Slug *<input required value={tr.slug} onChange={e => changeTr('slug', e.target.value)} /></label><label className="full">Summary *<textarea rows="4" value={tr.summary} onChange={e => changeTr('summary', e.target.value)} /></label><label className="full">Article content *<RichTextEditor value={tr.content} onChange={value => changeTr('content', value)} /></label><label>SEO title<input value={tr.metaTitle ?? ''} onChange={e => changeTr('metaTitle', e.target.value)} /></label><label>SEO description<textarea rows="3" value={tr.metaDescription ?? ''} onChange={e => changeTr('metaDescription', e.target.value)} /></label></div>}</section>
      <section className="admin-panel news-gallery-editor">
        <div className="panel-heading"><div><h2>News gallery</h2><p>Upload article images and choose the thumbnail used on News cards. With one image, it becomes the thumbnail automatically.</p></div><label className={`admin-primary-button ${galleryBusy ? 'disabled' : ''}`}>{galleryBusy ? 'Uploading…' : '+ Upload images'}<input type="file" accept="image/jpeg,image/png,image/webp" multiple hidden disabled={galleryBusy} onChange={uploadGallery} /></label></div>
        <MediaPicker label="Choose from Media Library" type="image/" onChange={() => {}} onSelect={chooseGalleryMedia} />
        {thumbnailMedia ? <><div className="news-thumbnail-section"><div><span>Selected thumbnail</span><strong>This image is always displayed as the News card and article cover.</strong></div><img src={resolveMediaUrl(thumbnailMedia.fileUrl)} alt="" /><button type="button" className="admin-text-button danger" onClick={() => removeGalleryMedia(thumbnailMedia)}>Remove thumbnail</button></div>{sortableGalleryMedia.length > 0 && <><p className="admin-help">Drag the gallery images to arrange their order. Choose “Use as thumbnail” to replace the cover above.</p><div className="news-admin-gallery">{sortableGalleryMedia.map(media => <figure draggable className={draggedMediaId === media.id ? 'dragging' : ''} key={media.id} onDragStart={() => setDraggedMediaId(media.id)} onDragEnd={() => setDraggedMediaId(null)} onDragOver={event => event.preventDefault()} onDrop={() => reorderGallery(media.id)}><div className="news-gallery-image"><img src={resolveMediaUrl(media.fileUrl)} alt="" /><span className="news-gallery-drag" aria-hidden="true">⠿ Drag</span></div><figcaption>{media.fileName}</figcaption><div><button type="button" onClick={() => setThumbnail(media)}>Use as thumbnail</button><button type="button" className="danger" onClick={() => removeGalleryMedia(media)}>Remove</button></div></figure>)}</div></>}</> : <div className="admin-empty"><strong>No gallery images</strong><p>Upload the first image and it will automatically become the News card thumbnail.</p></div>}
      </section>
      <section className="admin-panel"><h2>Article settings</h2><div className="form-grid"><label>Article type<select value={form.articleType} onChange={e => change('articleType', e.target.value)}><option>Authored</option><option>External</option></select></label>{form.articleType === 'External' && <><label>External source name<input value={form.externalSourceName ?? ''} onChange={e => change('externalSourceName', e.target.value)} /></label><label>Original article URL<input type="url" required value={form.externalUrl ?? ''} onChange={e => change('externalUrl', e.target.value)} /></label></>}<label>Publication date<input type="datetime-local" value={form.publishedAt ? new Date(form.publishedAt).toISOString().slice(0,16) : ''} onChange={e => change('publishedAt', e.target.value ? new Date(e.target.value).toISOString() : null)} /></label><label className="admin-switch-row"><input type="checkbox" checked={form.isFeatured} onChange={e => change('isFeatured', e.target.checked)} /> Featured article</label><label className="admin-switch-row"><input type="checkbox" checked={form.isPublished} onChange={e => change('isPublished', e.target.checked)} /> Published on website</label></div></section>
      {error && <div className="admin-form-error">{error}</div>}
      <div className="sticky-save"><button className="admin-primary-button" disabled={!dirty || busy}>{busy ? 'Saving…' : isNew ? 'Create article' : 'Save article'}</button><span className={dirty ? 'unsaved-chip' : 'saved-chip'}>{dirty ? 'Unsaved changes' : 'All changes saved'}</span>{!isNew && <button type="button" className="admin-danger-button" disabled={busy} onClick={remove}>Delete</button>}<Link className="admin-outline-button news-back-button" to="/admin/news">← Back to News</Link></div>
    </form><Toast message={toast} onClose={() => setToast('')} /></>
}
