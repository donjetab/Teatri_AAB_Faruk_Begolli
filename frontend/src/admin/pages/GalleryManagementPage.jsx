import { useEffect, useState } from 'react'
import { resolveMediaUrl } from '../../api/client'
import { adminApi } from '../api'
import { LoadingSkeleton, PageHeader } from '../components/AdminUi'

export function GalleryManagementPage() {
  const [filters, setFilters] = useState({ search: '', status: '', page: 1, pageSize: 20 })
  const [data, setData] = useState(null)
  const [error, setError] = useState('')
  const [openAlbumId, setOpenAlbumId] = useState(null)

  const load = () => {
    setError('')
    return adminApi.galleryAlbums(filters).then(setData).catch(requestError =>
      setError(requestError.response?.data?.detail ?? 'Gallery albums could not be loaded.'))
  }

  useEffect(() => {
    setData(null)
    const timer = setTimeout(() => { void load() }, filters.search ? 180 : 0)
    return () => clearTimeout(timer)
  }, [filters.search, filters.status, filters.page])

  const totalPages = Math.max(1, Math.ceil((data?.totalCount ?? 0) / filters.pageSize))

  return <><PageHeader eyebrow="Media" title="Gallery" description="See the pictures grouped by gallery album." actions={<a className="admin-outline-button" href="#/sq/galeria" target="_blank" rel="noreferrer">Open public gallery ↗</a>} />
    <section className="admin-panel gallery-admin-filter"><div className="communication-summary"><input placeholder="Search albums…" value={filters.search} onChange={event => setFilters(current => ({ ...current, search: event.target.value, page: 1 }))} /><select value={filters.status} onChange={event => setFilters(current => ({ ...current, status: event.target.value, page: 1 }))}><option value="">All albums</option><option value="published">Published</option><option value="draft">Draft</option></select><span>{data?.totalCount ?? 0} albums</span></div></section>
    {error ? <section className="admin-panel admin-request-error"><div>!</div><h2>Gallery unavailable</h2><p>{error}</p><button className="admin-primary-button" type="button" onClick={load}>Try again</button></section>
      : !data ? <LoadingSkeleton rows={8} />
        : data.items.length === 0 ? <section className="admin-panel admin-empty-state"><h2>No albums found</h2><p>Try another search or filter.</p></section>
          : <section className="gallery-simple-list">{data.items.map(album => {
            const expanded = openAlbumId === album.id
            const visibleMedia = expanded ? album.media : album.media.slice(0, 8)
            return <article className="gallery-simple-album" key={album.id}>
              <header><div><h2>{album.titleSq}</h2><p>{album.relatedContent || album.titleEn}</p></div><div><span>{album.media.length} picture{album.media.length === 1 ? '' : 's'}</span><i className={album.isPublished ? 'published' : ''}>{album.isPublished ? 'Published' : 'Draft'}</i></div></header>
              {visibleMedia.length ? <div className="gallery-simple-pictures">{visibleMedia.map(media => <figure key={media.id}><img src={resolveMediaUrl(media.fileUrl)} alt={media.altText ?? album.titleSq} loading="lazy" />{media.isCover && <figcaption>Cover</figcaption>}</figure>)}</div> : <div className="gallery-simple-empty">No pictures have been added to this album.</div>}
              {album.media.length > 8 && <button className="gallery-simple-more" type="button" onClick={() => setOpenAlbumId(expanded ? null : album.id)}>{expanded ? 'Show fewer pictures' : `Show all ${album.media.length} pictures`}</button>}
            </article>
          })}</section>}
    {data && <div className="admin-pagination"><span>{data.totalCount} albums</span><div><button disabled={filters.page <= 1} onClick={() => setFilters(current => ({ ...current, page: current.page - 1 }))}>← Previous</button><span>Page {filters.page} of {totalPages}</span><button disabled={filters.page >= totalPages} onClick={() => setFilters(current => ({ ...current, page: current.page + 1 }))}>Next →</button></div></div>}
  </>
}
