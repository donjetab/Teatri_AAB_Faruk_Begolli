import { useEffect, useMemo, useRef, useState } from 'react'
import { resolveMediaUrl } from '../../api/client'
import { adminApi } from '../api'
import { LoadingSkeleton, PageHeader, Toast } from '../components/AdminUi'
import { MediaPicker } from '../components/MediaPicker'

export function GalleryManagementPage() {
  const [data, setData] = useState(null)
  const [showAlbums, setShowAlbums] = useState(null)
  const [generalMedia, setGeneralMedia] = useState(null)
  const [error, setError] = useState('')
  const [toast, setToast] = useState('')
  const [search, setSearch] = useState('')
  const [openShowId, setOpenShowId] = useState(null)
  const [preview, setPreview] = useState(null)
  const [uploading, setUploading] = useState(false)
  const [draggedId, setDraggedId] = useState(null)
  const [draggedAlbumId, setDraggedAlbumId] = useState(null)
  const [editing, setEditing] = useState(null)
  const uploadInput = useRef(null)
  const load = () => Promise.all([adminApi.galleryAlbums({ page: 1, pageSize: 100 }), adminApi.generalGallery(), adminApi.showGalleryFolders()])
    .then(([albums, general, folders]) => { setData(albums); setGeneralMedia(general); setShowAlbums(folders); setError('') })
    .catch(e => setError(e.response?.data?.detail ?? 'Gallery pictures could not be loaded.'))
  useEffect(() => { void load() }, [])

  const allShowCards = useMemo(() => (showAlbums ?? [])
    .sort((a, b) => a.displayOrder - b.displayOrder)
    .map(album => ({
      id: `managed-${album.id}`,
      albumId: album.id,
      name: album.relatedContent || album.titleSq,
      isVisible: album.isVisibleInGeneralGallery,
      pictures: album.media.map(media => ({ id: media.id, src: resolveMediaUrl(media.fileUrl) })),
    })), [showAlbums])
  const showCards = useMemo(() => allShowCards.filter(card => !search.trim() || card.name.toLowerCase().includes(search.trim().toLowerCase())), [allShowCards, search])
  const openShow = allShowCards.find(card => card.id === openShowId)
  const uploaded = useMemo(() => (generalMedia ?? []).map(media => ({ id: `general-${media.id}`, mediaId: media.id, src: resolveMediaUrl(media.fileUrl), alt: media.altText ?? 'Teatri AAB Faruk Begolli', isFeatured: media.isFeatured })), [generalMedia])

  const add = async media => { if (!media) return; try { await adminApi.addGeneralGalleryMedia(media.id); setToast('Picture added to the public gallery.'); await load() } catch (e) { setToast(e.response?.data?.detail ?? 'The picture could not be added.') } }
  const remove = async picture => { await adminApi.removeGeneralGalleryMedia(picture.mediaId); setToast('Picture removed from the public gallery.'); await load() }
  const feature = async picture => { await adminApi.featureGeneralGalleryMedia(picture.mediaId, !picture.isFeatured); setToast(picture.isFeatured ? 'Picture unfeatured.' : 'Picture featured.'); await load() }
  const edit = async picture => setEditing(await adminApi.mediaItem(picture.mediaId))
  const saveEdit = async event => {
    event.preventDefault()
    await adminApi.saveMedia(editing.id, editing)
    setToast('Picture details saved.')
    setEditing(null)
    await load()
  }
  const upload = async event => {
    const files = [...(event.target.files ?? [])]; event.target.value = ''; if (!files.length) return
    setUploading(true)
    try { for (const file of files) await add(await adminApi.uploadMedia(file)); setToast(`${files.length} picture${files.length === 1 ? '' : 's'} uploaded.`) }
    finally { setUploading(false) }
  }
  const drop = async targetId => {
    if (!draggedId || draggedId === targetId) return
    const ordered = [...uploaded]; const from = ordered.findIndex(x => x.mediaId === draggedId); const to = ordered.findIndex(x => x.mediaId === targetId)
    const [moved] = ordered.splice(from, 1); ordered.splice(to, 0, moved); setDraggedId(null)
    await adminApi.reorderGeneralGalleryMedia(ordered.map(x => x.mediaId)); setToast('Picture order saved.'); await load()
  }
  const toggleAlbum = async card => {
    await adminApi.setGalleryAlbumVisibility(card.albumId, !card.isVisible)
    setToast(`${card.name} is now ${card.isVisible ? 'hidden from' : 'shown on'} the public gallery.`)
    await load()
  }
  const dropAlbum = async targetAlbumId => {
    if (!draggedAlbumId || draggedAlbumId === targetAlbumId || search.trim()) return
    const ordered = [...allShowCards]
    const from = ordered.findIndex(x => x.albumId === draggedAlbumId)
    const to = ordered.findIndex(x => x.albumId === targetAlbumId)
    const [moved] = ordered.splice(from, 1); ordered.splice(to, 0, moved); setDraggedAlbumId(null)
    await adminApi.reorderGalleryAlbums(ordered.map(x => x.albumId))
    setToast('Play gallery order saved.')
    await load()
  }

  return <>
    <PageHeader eyebrow="Media" title="Gallery" description="Manage the pictures and play folders shown on the public Gallery page." actions={<a className="admin-outline-button" href="#/sq/galeria" target="_blank" rel="noreferrer">Open public gallery ↗</a>} />
    {error ? <section className="admin-panel admin-request-error"><h2>Gallery unavailable</h2><p>{error}</p><button type="button" onClick={load}>Try again</button></section> : !data ? <LoadingSkeleton rows={8} /> : <>
      <section className="gallery-theatre-section gallery-uploaded-section"><header><div><h2>Theatre gallery pictures</h2><p>This is the main gallery. Drag pictures to change their public order.</p></div><div className="gallery-add-actions"><input ref={uploadInput} hidden multiple type="file" accept="image/jpeg,image/png,image/webp" onChange={upload} /><button type="button" className="admin-primary-button" disabled={uploading} onClick={() => uploadInput.current?.click()}>{uploading ? 'Uploading…' : '+ Upload pictures'}</button><MediaPicker compact label="Choose existing" type="image/" onSelect={add} /></div></header><div>{uploaded.map(picture => <figure draggable onDragStart={e => { e.dataTransfer.effectAllowed = 'move'; setDraggedId(picture.mediaId) }} onDragEnd={() => setDraggedId(null)} onDragOver={e => e.preventDefault()} onDrop={() => drop(picture.mediaId)} className={`${draggedId === picture.mediaId ? 'dragging' : ''} ${picture.isFeatured ? 'featured' : ''}`} key={picture.id}><span className="gallery-drag-handle">⠿ Drag</span><button className="gallery-picture-preview" type="button" onClick={() => setPreview(picture)}><img src={picture.src} alt={picture.alt} /></button><div className="gallery-picture-actions"><button type="button" onClick={() => edit(picture)}>Edit</button><button type="button" onClick={() => feature(picture)}>{picture.isFeatured ? 'Unfeature' : 'Feature'}</button><button type="button" onClick={() => remove(picture)}>Remove</button></div></figure>)}</div></section>
      <section className="admin-panel gallery-one-toolbar"><div><h2>Play gallery folders</h2><p>These appear below the main theatre gallery. Drag folders to arrange them and choose which ones are public.</p></div><input placeholder="Find a show…" value={search} onChange={e => setSearch(e.target.value)} /></section>
      {search.trim() && <p className="gallery-order-hint">Clear the search to rearrange folders.</p>}
      <section className="gallery-show-index">{showCards.map(card => <article draggable={!search.trim()} className={`${openShowId === card.id ? 'active ' : ''}${card.isVisible ? 'is-visible' : 'is-hidden'}${draggedAlbumId === card.albumId ? ' dragging' : ''}`} key={card.id} onDragStart={event => { event.dataTransfer.effectAllowed = 'move'; setDraggedAlbumId(card.albumId) }} onDragEnd={() => setDraggedAlbumId(null)} onDragOver={event => event.preventDefault()} onDrop={() => dropAlbum(card.albumId)}><button type="button" className="gallery-show-open" onClick={() => setOpenShowId(current => current === card.id ? null : card.id)}><img src={card.pictures[0]?.src} alt="" /><span><strong>{card.name}</strong><small>{card.pictures.length} pictures</small></span><i>{openShowId === card.id ? 'Close' : 'Open'} →</i></button><footer><span className="gallery-folder-drag">⠿ Drag folder</span><button type="button" className={card.isVisible ? 'visible' : 'hidden'} onClick={() => toggleAlbum(card)}>{card.isVisible ? 'Shown publicly' : 'Hidden'}</button></footer></article>)}</section>
      {openShow && <section className="admin-panel gallery-open-show"><header><div><h2>{openShow.name}</h2><p>{openShow.pictures.length} gallery pictures · {openShow.isVisible ? 'Shown on the public gallery' : 'Currently hidden'}</p></div><button type="button" className="admin-outline-button" onClick={() => setOpenShowId(null)}>Close gallery</button></header><div>{openShow.pictures.map(picture => <button type="button" key={picture.id} onClick={() => setPreview({ ...picture, alt: openShow.name })}><img src={picture.src} alt={openShow.name} /></button>)}</div></section>}
    </>}
    {preview && <div className="admin-modal-backdrop" onMouseDown={e => e.target === e.currentTarget && setPreview(null)}><section className="gallery-image-preview"><button type="button" onClick={() => setPreview(null)}>×</button><img src={preview.src} alt={preview.alt ?? ''} /></section></div>}
    {editing && <div className="admin-modal-backdrop" onMouseDown={e => e.target === e.currentTarget && setEditing(null)}><section className="admin-modal gallery-picture-editor"><div className="admin-modal-heading"><div><span>Gallery picture</span><h2>Edit picture details</h2></div><button type="button" onClick={() => setEditing(null)}>×</button></div><img src={resolveMediaUrl(editing.fileUrl)} alt="" /><form className="admin-form" onSubmit={saveEdit}><div className="form-grid"><label className="full">File name<input required value={editing.fileName} onChange={e => setEditing({ ...editing, fileName: e.target.value })} /></label><label className="full">Photographer credit<input value={editing.photographerCredit ?? ''} onChange={e => setEditing({ ...editing, photographerCredit: e.target.value })} /></label><label>Caption (Albanian)<textarea rows="3" value={editing.captionSq ?? ''} onChange={e => setEditing({ ...editing, captionSq: e.target.value })} /></label><label>Caption (English)<textarea rows="3" value={editing.captionEn ?? ''} onChange={e => setEditing({ ...editing, captionEn: e.target.value })} /></label><label>Alternative text (Albanian)<textarea rows="3" value={editing.altTextSq ?? ''} onChange={e => setEditing({ ...editing, altTextSq: e.target.value })} /></label><label>Alternative text (English)<textarea rows="3" value={editing.altTextEn ?? ''} onChange={e => setEditing({ ...editing, altTextEn: e.target.value })} /></label></div><div className="admin-modal-actions"><button type="button" className="admin-text-button" onClick={() => setEditing(null)}>Cancel</button><button className="admin-primary-button">Save picture</button></div></form></section></div>}
    <Toast message={toast} onClose={() => setToast('')} />
  </>
}
