import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { adminApi } from '../api'
import { DataTable, LoadingSkeleton, PageHeader, StatusBadge, Toast } from '../components/AdminUi'
import { useAdminDialog } from '../components/AdminDialog'

export function SeoIssuesPage() {
  const [data, setData] = useState(null)
  const [severity, setSeverity] = useState('')
  const [search, setSearch] = useState('')
  const [fixing, setFixing] = useState(false)
  const [toast, setToast] = useState('')
  const dialog = useAdminDialog()
  const load = () => adminApi.seoIssues().then(setData)
  useEffect(() => { load() }, [])
  const fixSafe = async () => {
    if (!await dialog.confirm({ title: 'Complete safe SEO fields?', message: 'Empty SEO titles and descriptions will be derived from existing titles and summaries. Identified posters and cover images will receive descriptive alternative text. Existing values will never be overwritten.', confirmLabel: 'Complete safe fields' })) return
    setFixing(true)
    try { const result = await adminApi.fixSafeSeoIssues(); setToast(result.message); await load() }
    catch (error) { setToast(error.response?.data?.detail ?? 'The warnings could not be repaired.') }
    finally { setFixing(false) }
  }
  if (!data) return <LoadingSkeleton rows={8} />
  const rows = data.issues.filter(item => (!severity || item.severity === severity) && (!search || `${item.title} ${item.detail} ${item.contentType}`.toLowerCase().includes(search.toLowerCase())))
  const columns = [
    { key: 'severity', label: 'Level', render: item => <StatusBadge status={item.severity} /> },
    { key: 'category', label: 'Issue' },
    { key: 'contentType', label: 'Content' },
    { key: 'title', label: 'Affected item' },
    { key: 'detail', label: 'What to do' },
    { key: 'open', label: '', render: item => <Link className="admin-outline-button seo-issue-open" to={item.adminPath}>Open and fix →</Link> },
  ]
  return <>
    <PageHeader eyebrow="Website" title="SEO & Content Issues" description="Find search, translation, link, image and accessibility problems stored in the website database." actions={<button className="admin-primary-button" disabled={fixing} onClick={fixSafe}>{fixing ? 'Checking…' : 'Fix safe warnings'}</button>} />
    <div className="translation-summary"><article><strong>{data.errors}</strong><span>Errors</span></article><article><strong>{data.warnings}</strong><span>Warnings</span></article><article><strong>{data.information}</strong><span>Information</span></article></div>
    <section className="admin-panel">
      <div className="communication-summary activity-filter-bar"><input placeholder="Search issues…" value={search} onChange={event => setSearch(event.target.value)} /><select value={severity} onChange={event => setSeverity(event.target.value)}><option value="">All levels</option><option>Error</option><option>Warning</option><option>Information</option></select><button className="activity-clear-filters" onClick={() => { setSearch(''); setSeverity('') }}>Clear filters</button></div>
      <DataTable columns={columns} rows={rows} emptyText="No matching content issues. Everything checked here looks complete." />
    </section>
    <Toast message={toast} onClose={() => setToast('')} />
  </>
}
