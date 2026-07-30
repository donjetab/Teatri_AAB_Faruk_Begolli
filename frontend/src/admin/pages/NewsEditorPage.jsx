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
        articleType: 'Authored', coverMediaAssetId: null, coverUrl: null, coverMimeType: null,
        cardThumbnailMediaAssetId: null, cardThumbnailUrl: null,
        externalUrl: null, externalSourceName: null, isPublished: false,
        isFeatured: false, publishedAt: null, galleryMedia: [], relatedLinks: [],
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
      coverMimeType: result.coverMimeType,
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
              coverMimeType: current.coverMimeType ?? media.mimeType,
              galleryMedia: [...galleryMedia, { ...media, isThumbnail: galleryMedia.length === 0 }],
            }
          })
        } else {
          updatePersistedGallery(await adminApi.attachNewsGalleryMedia(id, media.id))
        }
      }
      setToast(`${files.length} media file${files.length === 1 ? '' : 's'} uploaded.`)
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
          coverMimeType: current.coverMimeType ?? media.mimeType,
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
          coverMimeType: media.mimeType,
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
    if (!await dialog.confirm({ title: 'Remove media?', message: `“${media.fileName}” will be removed from this article. The Media Library file will be kept.`, confirmLabel: 'Remove media', danger: true })) return
    if (isNew) {
      setForm(current => {
        const galleryMedia = current.galleryMedia.filter(item => item.id !== media.id)
        const thumbnail = galleryMedia.find(item => item.isThumbnail) ?? galleryMedia[0]
        return {
          ...current,
          galleryMedia: galleryMedia.map(item => ({ ...item, isThumbnail: item.id === thumbnail?.id })),
          coverMediaAssetId: thumbnail?.id ?? null,
          coverUrl: thumbnail?.fileUrl ?? null,
          coverMimeType: thumbnail?.mimeType ?? null,
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
  const changeRelatedLink = (index, key, value) => setForm(current => ({
    ...current,
    relatedLinks: (current.relatedLinks ?? []).map((link, linkIndex) =>
      linkIndex === index ? { ...link, [key]: value } : link),
  }))
  const addRelatedLink = () => setForm(current => ({
    ...current,
    relatedLinks: [...(current.relatedLinks ?? []), {
      id: null, title: '', url: '', sourceName: '', publishedAt: null,
      displayOrder: current.relatedLinks?.length ?? 0,
    }],
  }))
  const removeRelatedLink = index => setForm(current => ({
    ...current,
    relatedLinks: (current.relatedLinks ?? []).filter((_, linkIndex) => linkIndex !== index)
      .map((link, displayOrder) => ({ ...link, displayOrder })),
  }))
  return <><PageHeader eyebrow="News" title={tr.title || 'New article'} description={isNew ? 'Create a bilingual news article for the public website.' : 'Edit bilingual article content, publication settings, source information and search metadata.'} actions={<><Link className="admin-outline-button" to="/admin/news">Back to news</Link><StatusBadge status={form.isPublished ? 'Published' : 'Draft'} /></>} />
    <form className={`admin-form news-editor-form language-${lang}`} onSubmit={save}><section className="admin-panel"><div className="news-editor-top"><LanguageTabs active={lang} onChange={setLang} /><button type="button" className="admin-text-button" onClick={() => setPreview(!preview)}>{preview ? 'Return to editor' : 'Preview article'}</button></div>
      {preview ? <article className="news-admin-preview"><h1>{tr.title}</h1><p>{tr.summary}</p><div dangerouslySetInnerHTML={{ __html: tr.content }} /></article> : <div className="form-grid"><label>Title *<input required value={tr.title} onChange={e => changeTr('title', e.target.value)} /></label><label>Slug *<input required value={tr.slug} onChange={e => changeTr('slug', e.target.value)} /></label><label className="full">Summary *<textarea rows="4" value={tr.summary} onChange={e => changeTr('summary', e.target.value)} /></label><div className="full admin-rich-field"><span>Article content {form.coverMimeType?.startsWith('video/') ? '(optional for video-led news)' : '*'}</span><RichTextEditor key={lang} value={tr.content} onChange={value => changeTr('content', value)} /></div><label>SEO title<input value={tr.metaTitle ?? ''} onChange={e => changeTr('metaTitle', e.target.value)} /></label><label>SEO description<textarea rows="3" value={tr.metaDescription ?? ''} onChange={e => changeTr('metaDescription', e.target.value)} /></label></div>}</section>
      <section className="admin-panel news-gallery-editor">
        <div className="panel-heading"><div><h2>Article media</h2><p>Upload images or MP4 videos. The main media is used on News cards and at the top of the article.</p></div><label className={`admin-primary-button ${galleryBusy ? 'disabled' : ''}`}>{galleryBusy ? 'Uploading…' : '+ Upload media'}<input type="file" accept="image/jpeg,image/png,image/webp,video/mp4" multiple hidden disabled={galleryBusy} onChange={uploadGallery} /></label></div>
        <MediaPicker label="Choose image or video from Media Library" type="" onChange={() => {}} onSelect={chooseGalleryMedia} />
        {thumbnailMedia ? <><div className="news-thumbnail-section"><div><span>Main media</span><strong>This {thumbnailMedia.mimeType?.startsWith('video/') ? 'video' : 'image'} is displayed on the News card and article cover.</strong></div>{thumbnailMedia.mimeType?.startsWith('video/') ? <video src={resolveMediaUrl(thumbnailMedia.fileUrl)} controls preload="metadata" /> : <img src={resolveMediaUrl(thumbnailMedia.fileUrl)} alt="" />}<button type="button" className="admin-text-button danger" onClick={() => removeGalleryMedia(thumbnailMedia)}>Remove main media</button></div>{sortableGalleryMedia.length > 0 && <><p className="admin-help">Drag media to arrange the article gallery. Choose “Use as main media” to replace the cover above.</p><div className="news-admin-gallery">{sortableGalleryMedia.map(media => <figure draggable className={draggedMediaId === media.id ? 'dragging' : ''} key={media.id} onDragStart={() => setDraggedMediaId(media.id)} onDragEnd={() => setDraggedMediaId(null)} onDragOver={event => event.preventDefault()} onDrop={() => reorderGallery(media.id)}><div className="news-gallery-image">{media.mimeType?.startsWith('video/') ? <video src={resolveMediaUrl(media.fileUrl)} muted preload="metadata" /> : <img src={resolveMediaUrl(media.fileUrl)} alt="" />}<span className="news-gallery-drag" aria-hidden="true">⠿ Drag</span></div><figcaption>{media.fileName}</figcaption><div><button type="button" onClick={() => setThumbnail(media)}>Use as main media</button><button type="button" className="danger" onClick={() => removeGalleryMedia(media)}>Remove</button></div></figure>)}</div></>}</> : <div className="admin-empty"><strong>No article media</strong><p>Upload an image or video. The first file automatically becomes the main media.</p></div>}
        {thumbnailMedia?.mimeType?.startsWith('video/') && <div className="news-card-thumbnail-choice"><div><h3>News card thumbnail</h3><p>Leave this empty to use the video preview on the News card, or choose a separate image.</p></div><MediaPicker label="Optional card image" value={form.cardThumbnailMediaAssetId} currentUrl={form.cardThumbnailUrl} type="image/" onSelect={media => setForm(current => ({ ...current, cardThumbnailMediaAssetId: media?.id ?? null, cardThumbnailUrl: media?.fileUrl ?? null }))} /></div>}
      </section>
      {form.articleType === 'Authored' && <section className="admin-panel news-related-links"><div className="panel-heading"><div><h2>Related external coverage</h2><p>Optional articles from other publishers covering the same topic. Links open in a new tab.</p></div><button type="button" className="admin-outline-button" onClick={addRelatedLink}>+ Add related link</button></div>{(form.relatedLinks ?? []).map((link, index) => <div className="news-related-link-row" key={link.id ?? `new-${index}`}><label>Link title *<input required value={link.title} onChange={e => changeRelatedLink(index, 'title', e.target.value)} /></label><label>Source name<input value={link.sourceName ?? ''} onChange={e => changeRelatedLink(index, 'sourceName', e.target.value)} /></label><label className="full">URL *<input type="url" required value={link.url} onChange={e => changeRelatedLink(index, 'url', e.target.value)} /></label><label>Publication date<input type="date" value={link.publishedAt ? String(link.publishedAt).slice(0, 10) : ''} onChange={e => changeRelatedLink(index, 'publishedAt', e.target.value ? new Date(`${e.target.value}T12:00:00Z`).toISOString() : null)} /></label><button type="button" className="admin-danger-button" onClick={() => removeRelatedLink(index)}>Remove</button></div>)}{!(form.relatedLinks ?? []).length && <div className="admin-empty"><strong>No related coverage</strong><p>Add links only when another publisher has covered this story.</p></div>}</section>}
      <section className="admin-panel"><h2>Article settings</h2><div className="form-grid"><label>Article ownership<select value={form.articleType} onChange={e => change('articleType', e.target.value)}><option value="Authored">Our article</option><option value="External">External article</option></select></label>{form.articleType === 'External' && <><label>External source name *<input required value={form.externalSourceName ?? ''} onChange={e => change('externalSourceName', e.target.value)} /></label><label>Original article URL *<input type="url" required value={form.externalUrl ?? ''} onChange={e => change('externalUrl', e.target.value)} /></label></>}<label>Publication date<input type="datetime-local" value={form.publishedAt ? new Date(form.publishedAt).toISOString().slice(0,16) : ''} onChange={e => change('publishedAt', e.target.value ? new Date(e.target.value).toISOString() : null)} /></label><label className="admin-switch-row"><input type="checkbox" checked={form.isFeatured} onChange={e => change('isFeatured', e.target.checked)} /> Featured article</label><label className="admin-switch-row"><input type="checkbox" checked={form.isPublished} onChange={e => change('isPublished', e.target.checked)} /> Published on website</label></div></section>
      {error && <div className="admin-form-error">{error}</div>}
      <div className="sticky-save"><button className="admin-primary-button" disabled={!dirty || busy}>{busy ? 'Saving…' : isNew ? 'Create article' : 'Save article'}</button><span className={dirty ? 'unsaved-chip' : 'saved-chip'}>{dirty ? 'Unsaved changes' : 'All changes saved'}</span>{!isNew && <button type="button" className="admin-danger-button" disabled={busy} onClick={remove}>Delete</button>}<Link className="admin-outline-button news-back-button" to="/admin/news">← Back to News</Link></div>
    </form><Toast message={toast} onClose={() => setToast('')} /></>
}
