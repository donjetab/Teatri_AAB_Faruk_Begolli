import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { resolveMediaUrl } from '../../api/client'
import { adminApi } from '../api'
import { EmptyState, LoadingSkeleton, PageHeader, StatusBadge, Toast } from '../components/AdminUi'

const initialFilters = { search: '', categoryId: '', status: '', lifecycleStatus: '', year: '', featured: '', sort: 'updated', page: 1, pageSize: 20 }

export function AdminShowsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [filters, setFilters] = useState(() => ({ ...initialFilters, ...Object.fromEntries(searchParams) }))
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [toast, setToast] = useState('')
  const queryKey = JSON.stringify(filters)

  useEffect(() => {
    const timer = setTimeout(() => {
      setLoading(true)
      const params = Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== ''))
      setSearchParams(params, { replace: true })
      adminApi.shows(params).then(setData).finally(() => setLoading(false))
    }, filters.search ? 250 : 0)
    return () => clearTimeout(timer)
  }, [queryKey, setSearchParams])

  const update = (key, value) => setFilters(current => ({ ...current, [key]: value, page: key === 'page' ? value : 1 }))
  const clear = () => setFilters(initialFilters)
  const action = async (show, nextAction) => {
    const question = nextAction === 'archive' ? `Archive “${show.titleSq}”?` : nextAction === 'publish' ? `Publish “${show.titleSq}”?` : null
    if (question && !window.confirm(question)) return
    await adminApi.showAction(show.id, nextAction)
    setToast(`Play ${nextAction === 'publish' ? 'published' : nextAction === 'unpublish' ? 'unpublished' : nextAction + 'd'}.`)
    setFilters(current => ({ ...current }))
  }
  const totalPages = Math.max(1, Math.ceil((data?.totalCount ?? 0) / Number(filters.pageSize)))

  return <><PageHeader eyebrow="Content" title="Shows / Plays" description="Create, translate, publish and maintain the theatre repertoire." actions={<Link className="admin-primary-button" to="/admin/shows/new">Create play</Link>} />
    <section className="admin-panel admin-filter-panel">
      <div className="show-filters">
        <label className="filter-search"><span>Search</span><input value={filters.search} onChange={e => update('search', e.target.value)} placeholder="Search by title…" /></label>
        <label><span>Category</span><select value={filters.categoryId} onChange={e => update('categoryId', e.target.value)}><option value="">All categories</option>{data?.categories?.map(x => <option value={x.id} key={x.id}>{x.label}</option>)}</select></label>
        <label><span>Publication</span><select value={filters.status} onChange={e => update('status', e.target.value)}><option value="">All states</option><option>Draft</option><option>Published</option><option>Archived</option></select></label>
        <label><span>Performance</span><select value={filters.lifecycleStatus} onChange={e => update('lifecycleStatus', e.target.value)}><option value="">All states</option><option>Upcoming</option><option>Active</option><option>Completed</option><option>SoldOut</option></select></label>
        <label><span>Year</span><select value={filters.year} onChange={e => update('year', e.target.value)}><option value="">All years</option>{data?.years?.map(x => <option key={x}>{x}</option>)}</select></label>
        <label><span>Featured</span><select value={filters.featured} onChange={e => update('featured', e.target.value)}><option value="">All</option><option value="true">Featured</option><option value="false">Not featured</option></select></label>
        <label><span>Sort</span><select value={filters.sort} onChange={e => update('sort', e.target.value)}><option value="updated">Last updated</option><option value="title">Title</option><option value="premiere">Premiere date</option></select></label>
        <button className="admin-text-button" onClick={clear}>Clear filters</button>
      </div>
    </section>
    {loading ? <LoadingSkeleton rows={6} /> : !data?.items?.length ? <EmptyState title="No plays found" text="Try clearing the filters or create the first play." /> :
      <section className="admin-panel shows-table-panel"><div className="admin-table-wrap"><table className="admin-table shows-table"><thead><tr><th>Play</th><th>Category</th><th>Year</th><th>Publication</th><th>Performance</th><th>Updated</th><th aria-label="Actions" /></tr></thead><tbody>{data.items.map(show => <tr key={show.id}>
        <td><div className="show-list-title">{show.posterUrl ? <img src={resolveMediaUrl(show.posterUrl)} alt="" /> : <span className="show-poster-empty">◇</span>}<div><strong>{show.titleSq}</strong><small>{show.titleEn}{show.isFeatured ? ' · Featured' : ''}</small></div></div></td>
        <td>{show.category}</td><td>{show.productionYear ?? '—'}</td><td><StatusBadge status={show.status} /></td><td><StatusBadge status={show.lifecycleStatus} /></td><td>{new Date(show.updatedAt).toLocaleDateString()}</td>
        <td><div className="table-actions"><Link to={`/admin/shows/${show.id}`}>Edit</Link><a href={`#/sq/shfaqjet/${show.slugSq}`} target="_blank">Preview</a>{show.status === 'Published' ? <button onClick={() => action(show, 'unpublish')}>Unpublish</button> : show.status === 'Archived' ? <button onClick={() => action(show, 'restore')}>Restore</button> : <button onClick={() => action(show, 'publish')}>Publish</button>}{show.status !== 'Archived' && <button onClick={() => action(show, 'archive')}>Archive</button>}</div></td>
      </tr>)}</tbody></table></div>
      <div className="admin-pagination"><span>{data.totalCount} plays</span><div><button disabled={Number(filters.page) <= 1} onClick={() => update('page', Number(filters.page) - 1)}>← Previous</button><span>Page {filters.page} of {totalPages}</span><button disabled={Number(filters.page) >= totalPages} onClick={() => update('page', Number(filters.page) + 1)}>Next →</button></div></div></section>}
    <Toast message={toast} onClose={() => setToast('')} /></>
}
