import { useEffect, useState } from 'react'
import { resolveMediaUrl } from '../../api/client'
import { adminApi } from '../api'
import { useAdminLanguage } from '../AdminLanguageContext'

export function MediaPicker({ label, value, currentUrl, type = 'image/', onChange, onSelect, compact = false }) {
  const { t } = useAdminLanguage()
  const [open, setOpen] = useState(false)
  const [data, setData] = useState(null)
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [uploading, setUploading] = useState(false)
  const [previewing, setPreviewing] = useState(false)
  const [savedItem, setSavedItem] = useState(null)
  useEffect(() => {
    if (!value) { setSavedItem(null); return undefined }
    let active = true
    adminApi.mediaItem(value).then(item => { if (active) setSavedItem(item) }).catch(() => { if (active) setSavedItem(null) })
    return () => { active = false }
  }, [value])
  useEffect(() => {
    if (!open) return
    const timer = setTimeout(() => adminApi.media({ type, search, page, pageSize: 100 }).then(result => {
      setData(current => page === 1 ? result : { ...result, items: [...(current?.items ?? []), ...result.items] })
    }), 150)
    return () => clearTimeout(timer)
  }, [open, search, type, page])
  const current = data?.items?.find(x => x.id === value) ?? (savedItem?.id === value ? savedItem : null)
  const previewUrl = current?.fileUrl ?? currentUrl
  const isVideo = type.startsWith('video')
  const isMixed = !type
  const previewIsVideo = current?.mimeType?.startsWith('video/') || (isVideo && !current)
  const choose = item => { setSavedItem(item); onChange?.(item.id); onSelect?.(item); setOpen(false) }
  const remove = () => { setSavedItem(null); onChange?.(null); onSelect?.(null) }
  const upload = async event => {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return
    setUploading(true)
    try {
      const item = await adminApi.uploadMedia(file)
      choose(item)
    } finally {
      setUploading(false)
    }
  }
  return <div className={`media-picker-field ${compact ? 'compact' : ''}`}>{compact ? <button className="admin-outline-button" type="button" onClick={() => setOpen(true)}>{t(label)}</button> : <><span>{t(label)}</span><div>
    {previewUrl ? <button type="button" className="media-picker-preview-button" onClick={() => setPreviewing(true)} aria-label="Preview selected file">{previewIsVideo ? <video src={resolveMediaUrl(previewUrl)} muted /> : <img src={resolveMediaUrl(previewUrl)} alt="" />}</button> : <i>◇</i>}
    <strong>{current?.fileName ?? (value ? `Media #${value}` : previewUrl ? 'Current website image' : isMixed ? 'No media selected' : isVideo ? 'No local video selected' : 'No image selected')}</strong>
    <button type="button" onClick={() => setOpen(true)}>{t('Choose or upload')}</button>{(value || previewUrl) && <button type="button" onClick={remove}>{t('Remove')}</button>}
  </div></>}{open && <div className="admin-modal-backdrop" onMouseDown={e => e.target === e.currentTarget && setOpen(false)}><section className="admin-modal">
    <div className="admin-modal-heading"><div><span>Media Library</span><h2>Select {isMixed ? 'image or video' : isVideo ? 'video' : 'image'}</h2></div><button type="button" onClick={() => setOpen(false)}>×</button></div>
    <div className="media-picker-toolbar">
      <input className="media-picker-search" placeholder={`Search ${isMixed ? 'media' : isVideo ? 'videos' : 'images'}…`} value={search} onChange={e => { setSearch(e.target.value); setPage(1); setData(null) }} />
      <label className={`admin-primary-button ${uploading ? 'disabled' : ''}`}>{uploading ? 'Uploading…' : `Upload new ${isMixed ? 'media' : isVideo ? 'video' : 'image'}`}<input type="file" hidden disabled={uploading} accept={isMixed ? 'image/jpeg,image/png,image/webp,video/mp4' : isVideo ? 'video/mp4,video/webm,video/quicktime' : 'image/jpeg,image/png,image/webp'} onChange={upload} /></label>
    </div>
    <div className="media-picker-grid">{data?.items?.map(item => <button type="button" className={item.id === value ? 'selected' : ''} key={item.id} onClick={() => choose(item)}>{item.mimeType?.startsWith('video/') ? <video src={resolveMediaUrl(item.fileUrl)} muted preload="metadata" /> : <img src={resolveMediaUrl(item.fileUrl)} alt={item.altTextSq ?? ''} />}<span>{item.fileName}</span></button>)}</div>
    {data && data.items.length < data.totalCount && <div className="admin-pagination"><span>{t('Showing')} {data.items.length} {t('of')} {data.totalCount}</span><button type="button" className="admin-outline-button" onClick={() => setPage(currentPage => currentPage + 1)}>{t('Load more media')}</button></div>}
  </section></div>}{previewing && <div className="admin-modal-backdrop" onMouseDown={e => e.target === e.currentTarget && setPreviewing(false)}><section className="admin-modal page-image-preview"><button type="button" className="page-image-preview-close" onClick={() => setPreviewing(false)}>×</button>{previewIsVideo ? <video src={resolveMediaUrl(previewUrl)} controls autoPlay /> : <img src={resolveMediaUrl(previewUrl)} alt="Selected media preview" />}</section></div>}</div>
}
