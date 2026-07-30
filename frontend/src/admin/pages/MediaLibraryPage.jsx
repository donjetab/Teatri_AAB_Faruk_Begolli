import { useEffect, useRef, useState } from 'react'
import { resolveMediaUrl } from '../../api/client'
import { adminApi } from '../api'
import { useAdminDialog } from '../components/AdminDialog'
import { PageHeader, Toast } from '../components/AdminUi'

export function MediaLibraryPage() {
  const dialog = useAdminDialog()
  const [filters, setFilters] = useState({ search: '', type: '' })
  const [data, setData] = useState(null)
  const [selected, setSelected] = useState(null)
  const [toast, setToast] = useState('')
  const [uploading, setUploading] = useState(false)
  const input = useRef(null)
  const load = () => adminApi.media(filters).then(setData)

  useEffect(() => {
    const timer = setTimeout(load, 200)
    return () => clearTimeout(timer)
  }, [filters.search, filters.type])

  const upload = async files => {
    setUploading(true)
    try {
      for (const file of files) await adminApi.uploadMedia(file)
      setToast(`${files.length} file${files.length === 1 ? '' : 's'} uploaded.`)
      load()
    } finally { setUploading(false) }
  }
  const save = async event => {
    event.preventDefault()
    await adminApi.saveMedia(selected.id, selected)
    setToast('File details saved.')
    setSelected(null)
    load()
  }
  const remove = async item => {
    if (!await dialog.confirm({ title: 'Delete this file?', message: `"${item.fileName}" will be permanently removed.`, confirmLabel: 'Delete file', danger: true })) return
    try {
      await adminApi.deleteMedia(item.id)
      setToast('Unused file deleted.')
      setSelected(null)
      load()
    } catch (error) { setToast(error.response?.data?.detail ?? 'This file cannot be deleted because it is being used.') }
  }

  const types = [['', 'All'], ['image/', 'Pictures'], ['video/', 'Videos'], ['application/', 'Documents']]

  return <><PageHeader eyebrow="Website" title="Media Library" description="Upload files here, then choose them while editing website content." actions={<><input ref={input} hidden multiple type="file" accept=".jpg,.jpeg,.png,.webp,.svg,.mp4,.pdf,.doc,.docx" onChange={event => upload([...event.target.files])} /><button className="admin-primary-button" disabled={uploading} onClick={() => input.current.click()}>{uploading ? 'Uploading…' : '+ Upload files'}</button></>} />
    <section className="admin-panel media-simple-toolbar"><input value={filters.search} onChange={event => setFilters(current => ({ ...current, search: event.target.value }))} placeholder="Search by file name…" /><div className="media-type-tabs">{types.map(([value, label]) => <button type="button" className={filters.type === value ? 'active' : ''} key={label} onClick={() => setFilters(current => ({ ...current, type: value }))}>{label}</button>)}</div><span>{data?.totalCount ?? 0} files</span></section>
    <section className="media-grid media-grid-simple">{data?.items?.map(item => <article key={item.id}><button className="media-preview" onClick={() => setSelected(item)}>{item.mimeType.startsWith('image/') ? <img src={resolveMediaUrl(item.fileUrl)} alt={item.altTextSq ?? ''} /> : item.mimeType.startsWith('video/') ? <video src={resolveMediaUrl(item.fileUrl)} muted preload="metadata" /> : <span>{item.mimeType === 'application/pdf' ? 'PDF' : 'FILE'}</span>}<i>{item.usageCount ? `Used ${item.usageCount}×` : 'Unused'}</i></button><div><strong title={item.fileName}>{item.fileName}</strong><small>Click to view or edit</small></div></article>)}</section>
    {selected && <div className="admin-modal-backdrop" onMouseDown={event => event.target === event.currentTarget && setSelected(null)}><section className="admin-modal media-editor"><div className="admin-modal-heading"><div><span>File details</span><h2>{selected.fileName}</h2></div><button onClick={() => setSelected(null)}>×</button></div>{selected.mimeType.startsWith('image/') ? <img src={resolveMediaUrl(selected.fileUrl)} alt="" /> : selected.mimeType.startsWith('video/') && <video controls src={resolveMediaUrl(selected.fileUrl)} />}<div className="media-detail-actions"><button type="button" onClick={() => navigator.clipboard.writeText(resolveMediaUrl(selected.fileUrl)).then(() => setToast('URL copied.'))}>Copy file URL</button><button type="button" className="danger" disabled={selected.usageCount > 0} onClick={() => remove(selected)}>Delete unused file</button></div><form className="admin-form" onSubmit={save}><div className="form-grid"><label className="full">File name<input required value={selected.fileName} onChange={event => setSelected({ ...selected, fileName: event.target.value })} /></label><label>Albanian alternative text<textarea rows="3" value={selected.altTextSq ?? ''} onChange={event => setSelected({ ...selected, altTextSq: event.target.value })} /></label><label>English alternative text<textarea rows="3" value={selected.altTextEn ?? ''} onChange={event => setSelected({ ...selected, altTextEn: event.target.value })} /></label><label>Albanian caption<textarea rows="3" value={selected.captionSq ?? ''} onChange={event => setSelected({ ...selected, captionSq: event.target.value })} /></label><label>English caption<textarea rows="3" value={selected.captionEn ?? ''} onChange={event => setSelected({ ...selected, captionEn: event.target.value })} /></label></div><div className="admin-modal-actions"><button type="button" className="admin-text-button" onClick={() => setSelected(null)}>Close</button><button className="admin-primary-button">Save changes</button></div></form></section></div>}
    <Toast message={toast} onClose={() => setToast('')} /></>
}
