import { useEffect, useState } from 'react'
import { adminApi } from '../api'
import { DataTable, LoadingSkeleton, PageHeader } from '../components/AdminUi'

const initialFilters = { search: '', entityType: '', action: '', adminName: '', day: '', page: 1, pageSize: 25 }

function activityQuery(filters) {
  const { day, ...query } = filters
  if (!day) return query
  const from = new Date(`${day}T00:00:00`)
  const to = new Date(from)
  to.setDate(to.getDate() + 1)
  return { ...query, from: from.toISOString(), to: to.toISOString() }
}

export function ActivityLogPage() {
  const [filters, setFilters] = useState(initialFilters)
  const [data, setData] = useState(null)
  const [users, setUsers] = useState([])

  useEffect(() => { adminApi.activityUsers().then(setUsers) }, [])
  useEffect(() => {
    const timer = setTimeout(() => adminApi.activityLog(activityQuery(filters)).then(setData), 200)
    return () => clearTimeout(timer)
  }, [filters])

  if (!data) return <LoadingSkeleton rows={8} />
  const update = values => setFilters(current => ({ ...current, ...values, page: 1 }))
  const columns = [
    { key: 'createdAt', label: 'Date', render: item => new Date(item.createdAt).toLocaleString() },
    { key: 'adminName', label: 'Administrator' },
    { key: 'action', label: 'Action' },
    { key: 'entityType', label: 'Content type' },
    { key: 'summary', label: 'Details' },
  ]

  return <>
    <PageHeader eyebrow="Administration" title="Activity Log" description="A read-only audit trail of actions by every administrator, including deleted accounts." />
    <section className="admin-panel">
      <div className="communication-summary">
        <input placeholder="Search activity…" value={filters.search} onChange={event => update({ search: event.target.value })} />
        <select aria-label="Administrator" value={filters.adminName} onChange={event => update({ adminName: event.target.value })}>
          <option value="">All administrators</option>
          {users.map(name => <option key={name} value={name}>{name}</option>)}
        </select>
        <label>Day<input type="date" value={filters.day} onChange={event => update({ day: event.target.value })} /></label>
        <input placeholder="Action" value={filters.action} onChange={event => update({ action: event.target.value })} />
        <input placeholder="Content type" value={filters.entityType} onChange={event => update({ entityType: event.target.value })} />
        <button type="button" onClick={() => setFilters(initialFilters)}>Clear filters</button>
      </div>
      <DataTable columns={columns} rows={data.items} emptyText="No matching activity." />
      <div className="admin-pagination"><span>{data.totalCount} actions</span><div>
        <button disabled={filters.page <= 1} onClick={() => setFilters(current => ({ ...current, page: current.page - 1 }))}>← Previous</button>
        <span>Page {filters.page}</span>
        <button disabled={filters.page * filters.pageSize >= data.totalCount} onClick={() => setFilters(current => ({ ...current, page: current.page + 1 }))}>Next →</button>
      </div></div>
    </section>
  </>
}
