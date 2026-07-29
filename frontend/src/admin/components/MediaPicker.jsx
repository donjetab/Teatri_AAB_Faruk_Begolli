import { useEffect, useState } from 'react'
import { resolveMediaUrl } from '../../api/client'
import { adminApi } from '../api'

export function MediaPicker({ label, value, currentUrl, type = 'image/', onChange, onSelect }) {
  const [open, setOpen] = useState(false)
  const [data, setData] = useState(null)
  const [search, setSearch] = useState('')
  const [uploading, setUploading] = useState(false)
  useEffect(() => {
    if (!open) return
    const timer = setTimeout(() => adminApi.media({ type, search, pageSize: 50 }).then(setData), 150)
    return () => clearTimeout(timer)
  }, [open, search, type])
  const current = data?.items?.find(x => x.id === value)
  const previewUrl = current?.fileUrl ?? currentUrl
  const isVideo = type.startsWith('video')
  const choose = item => { onChange?.(item.id); onSelect?.(item); setOpen(false) }
  const remove = () => { onChange?.(null); onSelect?.(null) }
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
  return <div className="media-picker-field"><span>{label}</span><div>
    {previewUrl ? isVideo ? <video src={resolveMediaUrl(previewUrl)} muted /> : <img src={resolveMediaUrl(previewUrl)} alt="" /> : <i>◇</i>}
    <strong>{current?.fileName ?? (value ? `Media #${value}` : isVideo ? 'No local video selected' : 'No image selected')}</strong>
    <button type="button" onClick={() => setOpen(true)}>Choose or upload</button>{(value || previewUrl) && <button type="button" onClick={remove}>Remove</button>}
  </div>{open && <div className="admin-modal-backdrop" onMouseDown={e => e.target === e.currentTarget && setOpen(false)}><section className="admin-modal">
    <div className="admin-modal-heading"><div><span>Media Library</span><h2>Select {isVideo ? 'video' : 'image'}</h2></div><button type="button" onClick={() => setOpen(false)}>×</button></div>
    <div className="media-picker-toolbar">
      <input className="media-picker-search" placeholder={`Search ${isVideo ? 'videos' : 'images'}…`} value={search} onChange={e => setSearch(e.target.value)} />
      <label className={`admin-primary-button ${uploading ? 'disabled' : ''}`}>{uploading ? 'Uploading…' : `Upload new ${isVideo ? 'video' : 'image'}`}<input type="file" hidden disabled={uploading} accept={isVideo ? 'video/mp4,video/webm,video/quicktime' : 'image/jpeg,image/png,image/webp'} onChange={upload} /></label>
    </div>
    <div className="media-picker-grid">{data?.items?.map(item => <button type="button" className={item.id === value ? 'selected' : ''} key={item.id} onClick={() => choose(item)}>{isVideo ? <video src={resolveMediaUrl(item.fileUrl)} muted preload="metadata" /> : <img src={resolveMediaUrl(item.fileUrl)} alt={item.altTextSq ?? ''} />}<span>{item.fileName}</span></button>)}</div>
  </section></div>}</div>
}
