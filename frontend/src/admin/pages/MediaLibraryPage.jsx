import { useEffect, useRef, useState } from 'react'
import { resolveMediaUrl } from '../../api/client'
import { adminApi } from '../api'
import { useAdminDialog } from '../components/AdminDialog'
import { PageHeader, Toast } from '../components/AdminUi'

export function MediaLibraryPage() {
  const dialog = useAdminDialog()
  const [filters, setFilters] = useState({ search: '', type: '', unused: false })
  const [data, setData] = useState(null)
  const [selectedIds, setSelectedIds] = useState([])
  const [selected, setSelected] = useState(null)
  const [usage, setUsage] = useState(null)
  const [usageError, setUsageError] = useState('')
  const [toast, setToast] = useState('')
  const [uploading, setUploading] = useState(false)
  const [loadingMore, setLoadingMore] = useState(false)
  const input = useRef(null)
  const replaceInput = useRef(null)
  const load = (page = 1, append = false) => adminApi.media({ ...filters, page, pageSize: 30 }).then(result => setData(current => append ? { ...result, items: [...(current?.items ?? []), ...result.items] } : result))
  useEffect(() => { setData(null); setSelectedIds([]); const timer = setTimeout(() => load(1), 200); return () => clearTimeout(timer) }, [filters.search, filters.type, filters.unused])
  const loadMore = async () => { setLoadingMore(true); try { await load((data?.page ?? 1) + 1, true) } finally { setLoadingMore(false) } }

  const open = async item => { setSelected(item); setUsage(null); setUsageError(''); try { setUsage(await adminApi.mediaUsage(item.id)) } catch (error) { setUsageError(error.response?.data?.detail ?? 'Usage information could not be loaded.'); setUsage([]) } }
  const upload = async files => { setUploading(true); try { for (const file of files) await adminApi.uploadMedia(file); setToast(`${files.length} file${files.length === 1 ? '' : 's'} uploaded.`); load() } finally { setUploading(false) } }
  const save = async event => { event.preventDefault(); await adminApi.saveMedia(selected.id, selected); setToast('File details saved.'); setSelected(null); load() }
  const remove = async item => {
    if (!await dialog.confirm({ title: 'Delete this file?', message: `“${item.fileName}” will be permanently removed.`, confirmLabel: 'Delete file', danger: true })) return
    try { await adminApi.deleteMedia(item.id); setToast('Unused file deleted.'); setSelected(null); load() }
    catch (error) { setToast(error.response?.data?.detail ?? 'This file cannot be deleted because it is being used.') }
  }
  const removeSelected = async () => {
    if (!selectedIds.length) return
    if (!await dialog.confirm({ title: `Delete ${selectedIds.length} unused files?`, message: 'The selected files will be permanently removed. Files that became used will remain protected.', confirmLabel: 'Delete selected files', danger: true })) return
    let deleted = 0
    let protectedCount = 0
    for (const id of selectedIds) {
      try { await adminApi.deleteMedia(id); deleted += 1 } catch { protectedCount += 1 }
    }
    setSelectedIds([])
    setToast(protectedCount ? `${deleted} files deleted; ${protectedCount} could not be deleted.` : `${deleted} unused files deleted.`)
    load()
  }
  const toggleSelected = id => setSelectedIds(current => current.includes(id) ? current.filter(value => value !== id) : [...current, id])
  const replace = async event => {
    const file = event.target.files?.[0]; event.target.value = ''; if (!file) return
    try { const updated = await adminApi.replaceMedia(selected.id, file); setSelected(current => ({ ...current, ...updated })); setToast('Physical file replaced. Existing references were preserved.'); load() }
    catch (error) { setToast(error.response?.data?.detail ?? 'The file could not be replaced.') }
  }
  const types = [['', 'All'], ['image/', 'Pictures'], ['video/', 'Videos'], ['application/', 'Documents']]

  return <>
    <PageHeader eyebrow="Website" title="Media Library" description="Every reusable website file and exactly where it is used." actions={<><input ref={input} hidden multiple type="file" accept=".jpg,.jpeg,.png,.webp,.svg,.mp4,.pdf,.doc,.docx" onChange={event => upload([...event.target.files])} /><button className="admin-primary-button" disabled={uploading} onClick={() => input.current.click()}>{uploading ? 'Uploading…' : '+ Upload files'}</button></>} />
    <section className="admin-panel media-simple-toolbar">
      <input value={filters.search} onChange={event => setFilters(current => ({ ...current, search: event.target.value }))} placeholder="Search by file name…" />
      <div className="media-type-tabs">{types.map(([value, label]) => <button type="button" className={filters.type === value ? 'active' : ''} key={label} onClick={() => setFilters(current => ({ ...current, type: value }))}>{label}</button>)}</div>
      <label className="admin-check"><input type="checkbox" checked={filters.unused} onChange={event => setFilters(current => ({ ...current, unused: event.target.checked }))} /> Unused only</label>
      <span>{data?.totalCount ?? 0} files</span>
    </section>
    {filters.unused && data?.items?.length > 0 && <section className="admin-panel media-bulk-actions"><label className="admin-check"><input type="checkbox" checked={selectedIds.length === data.items.length} onChange={event => setSelectedIds(event.target.checked ? data.items.map(item => item.id) : [])} /> Select all shown</label><button type="button" className="danger" disabled={!selectedIds.length} onClick={removeSelected}>Delete selected ({selectedIds.length})</button></section>}
    <section className="media-grid media-grid-simple">{data?.items?.map(item => <article className={selectedIds.includes(item.id) ? 'selected' : ''} key={item.id}>{filters.unused && <label className="media-select"><input type="checkbox" checked={selectedIds.includes(item.id)} onChange={() => toggleSelected(item.id)} /> Select</label>}<button className="media-preview" onClick={() => open(item)}>{item.mimeType.startsWith('image/') ? <img src={resolveMediaUrl(item.fileUrl)} alt={item.altTextSq ?? ''} /> : item.mimeType.startsWith('video/') ? <video src={resolveMediaUrl(item.fileUrl)} muted preload="metadata" /> : <span>{item.mimeType === 'application/pdf' ? 'PDF' : 'FILE'}</span>}<i>{item.usageCount ? `Used ${item.usageCount}×` : 'Unused'}</i></button><div><strong title={item.fileName}>{item.fileName}</strong><small>Click to view, edit and check usage</small></div></article>)}</section>
    {data && data.items.length < data.totalCount && <div className="media-load-more"><button type="button" className="admin-outline-button" disabled={loadingMore} onClick={loadMore}>{loadingMore ? 'Loading…' : `Load more (${data.totalCount - data.items.length} remaining)`}</button><span>Showing {data.items.length} of {data.totalCount}</span></div>}
    {selected && <div className="admin-modal-backdrop" onMouseDown={event => event.target === event.currentTarget && setSelected(null)}>
      <section className="admin-modal media-editor">
        <div className="admin-modal-heading"><div>
          <span>File details</span>
          <h2>{selected.fileName}</h2>
        </div>
        <button onClick={() => setSelected(null)}>×</button>
        </div>{selected.mimeType.startsWith('image/') ? <img src={resolveMediaUrl(selected.fileUrl)} alt="" /> : selected.mimeType.startsWith('video/') && <video controls src={resolveMediaUrl(selected.fileUrl)} />}<p className="admin-help">{selected.width && selected.height ? `${selected.width} × ${selected.height} px · ` : ''}{selected.fileSize ? `${(selected.fileSize / 1024 / 1024).toFixed(2)} MB · ` : ''}{selected.mimeType}</p><section className="media-usage-panel"><h3>Used in</h3>{usageError ? <p className="error">{usageError}</p> : usage === null ? <p>Checking usage…</p> : usage.length ? <div>{usage.map((item, index) => <article key={`${item.contentType}-${item.contentId}-${item.role}-${index}`}><span><strong>{item.title}</strong><small>{item.contentType} · {item.role}</small></span>{item.adminPath && <a href={`#${item.adminPath}`}>Open →</a>}</article>)}</div> : <p>This file is not used by any content.</p>}</section><div className="media-detail-actions"><button type="button" onClick={() => navigator.clipboard.writeText(resolveMediaUrl(selected.fileUrl)).then(() => setToast('URL copied.'))}>Copy file URL</button><input ref={replaceInput} hidden type="file" onChange={replace} /><button type="button" onClick={() => replaceInput.current?.click()}>Replace file</button><button type="button" className="danger" disabled={Boolean(usageError) || (usage?.length ?? selected.usageCount) > 0} onClick={() => remove(selected)}>Delete unused file</button></div><form className="admin-form" onSubmit={save}><div className="form-grid"><label className="full">File name<input required value={selected.fileName} onChange={event => setSelected({ ...selected, fileName: event.target.value })} /></label><label className="full">Photographer credit<input value={selected.photographerCredit ?? ''} onChange={event => setSelected({ ...selected, photographerCredit: event.target.value })} /></label><label>Albanian alternative text<textarea rows="3" value={selected.altTextSq ?? ''} onChange={event => setSelected({ ...selected, altTextSq: event.target.value })} /></label><label>English alternative text<textarea rows="3" value={selected.altTextEn ?? ''} onChange={event => setSelected({ ...selected, altTextEn: event.target.value })} /></label><label>Albanian caption<textarea rows="3" value={selected.captionSq ?? ''} onChange={event => setSelected({ ...selected, captionSq: event.target.value })} /></label><label>English caption<textarea rows="3" value={selected.captionEn ?? ''} onChange={event => setSelected({ ...selected, captionEn: event.target.value })} /></label></div><div className="admin-modal-actions"><button type="button" className="admin-text-button" onClick={() => setSelected(null)}>Close</button><button className="admin-primary-button">Save changes</button></div></form></section></div>}
    <Toast message={toast} onClose={() => setToast('')} />
  </>
}
