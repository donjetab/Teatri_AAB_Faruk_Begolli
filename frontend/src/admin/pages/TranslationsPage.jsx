import { useEffect, useState } from 'react'
import { adminApi } from '../api'
import { DataTable, PageHeader, StatusBadge } from '../components/AdminUi'

export function TranslationsPage() {
  const [type, setType] = useState(''); const [data, setData] = useState({ items: [], totalCount: 0 })
  useEffect(() => { adminApi.translations({ contentType: type || undefined, page: 1, pageSize: 50 }).then(setData) }, [type])
  const columns = [{ key: 'contentType', label: 'Content type' }, { key: 'title', label: 'Content' }, { key: 'missingLanguage', label: 'Language to review', render: x => x.missingLanguage === 'sq' ? 'Albanian' : 'English' }, { key: 'status', label: 'State', render: x => <StatusBadge status={x.status} /> }, { key: 'updatedAt', label: 'Last changed', render: x => new Date(x.updatedAt).toLocaleDateString() }]
  return <><PageHeader eyebrow="Website" title="Translations" description="Review missing and partially translated public content before publication." />
    <div className="translation-summary"><article><strong>{data.items.filter(x => x.missingLanguage === 'sq').length}</strong><span>Albanian to review</span></article><article><strong>{data.items.filter(x => x.missingLanguage === 'en').length}</strong><span>English to review</span></article><article><strong>{data.totalCount}</strong><span>Total issues</span></article></div>
    <section className="admin-panel"><div className="panel-heading"><h2>Translation issues</h2><label className="compact-filter">Content type<select value={type} onChange={e => setType(e.target.value)}><option value="">All</option><option value="shows">Shows</option><option value="news">News</option><option value="pages">Pages</option><option value="pitf">PITF</option><option value="website">Homepage & website</option></select></label></div><div className="translation-note">Missing means the language does not exist. Incomplete means one or more required fields are blank and should be reviewed before publication.</div><DataTable columns={columns} rows={data.items} emptyText="All checked content has complete Albanian and English translations." /></section></>
}
