import { useEffect, useState } from 'react'
import { resolveMediaUrl } from '../../api/client'
import { adminApi } from '../api'

export function MediaPicker({ label, value, onChange }) {
  const [open, setOpen] = useState(false); const [data, setData] = useState(null); const [search, setSearch] = useState('')
  useEffect(() => { if (!open) return; const timer = setTimeout(() => adminApi.media({ type: 'image/', search, pageSize: 50 }).then(setData), 150); return () => clearTimeout(timer) }, [open, search])
  const current = data?.items?.find(x => x.id === value)
  return <div className="media-picker-field"><span>{label}</span><div>{current ? <img src={resolveMediaUrl(current.fileUrl)} alt="" /> : <i>◇</i>}<strong>{current?.fileName ?? (value ? `Media #${value}` : 'No image selected')}</strong><button type="button" onClick={() => setOpen(true)}>Choose</button>{value && <button type="button" onClick={() => onChange(null)}>Remove</button>}</div>
    {open && <div className="admin-modal-backdrop" onMouseDown={e => e.target === e.currentTarget && setOpen(false)}><section className="admin-modal"><div className="admin-modal-heading"><div><span>Media Library</span><h2>Select image</h2></div><button type="button" onClick={() => setOpen(false)}>×</button></div><input className="media-picker-search" placeholder="Search images…" value={search} onChange={e => setSearch(e.target.value)} /><div className="media-picker-grid">{data?.items?.map(item => <button type="button" className={item.id === value ? 'selected' : ''} key={item.id} onClick={() => { onChange(item.id); setOpen(false) }}><img src={resolveMediaUrl(item.fileUrl)} alt={item.altTextSq ?? ''} /><span>{item.fileName}</span></button>)}</div></section></div>}
  </div>
}
