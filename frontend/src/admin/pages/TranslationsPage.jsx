import { useEffect, useState } from 'react'
import { adminApi } from '../api'
import { DataTable, PageHeader, StatusBadge } from '../components/AdminUi'

export function TranslationsPage() {
  const [type, setType] = useState(''); const [data, setData] = useState({ items: [], totalCount: 0 })
  useEffect(() => { adminApi.translations({ contentType: type || undefined, page: 1, pageSize: 50 }).then(setData) }, [type])
  const columns = [{ key: 'contentType', label: 'Content type' }, { key: 'title', label: 'Content' }, { key: 'missingLanguage', label: 'Missing language', render: x => x.missingLanguage === 'sq' ? 'Albanian' : 'English' }, { key: 'status', label: 'State', render: x => <StatusBadge status={x.status} /> }, { key: 'updatedAt', label: 'Last changed', render: x => new Date(x.updatedAt).toLocaleDateString() }]
  return <><PageHeader eyebrow="Website" title="Translations" description="Review missing and partially translated public content before publication." />
    <div className="translation-summary"><article><strong>{data.items.filter(x => x.missingLanguage === 'sq').length}</strong><span>Missing Albanian</span></article><article><strong>{data.items.filter(x => x.missingLanguage === 'en').length}</strong><span>Missing English</span></article><article><strong>{data.totalCount}</strong><span>Total issues</span></article></div>
    <section className="admin-panel"><div className="panel-heading"><h2>Translation issues</h2><label className="compact-filter">Content type<select value={type} onChange={e => setType(e.target.value)}><option value="">All</option><option value="shows">Shows</option><option value="news">News</option></select></label></div><div className="translation-note">Required translations must be reviewed before publishing. Publication endpoints should enforce this warning when the content editors are added.</div><DataTable columns={columns} rows={data.items} emptyText="All checked content has Albanian and English translations." /></section></>
}
