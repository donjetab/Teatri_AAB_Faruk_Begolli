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

const contentNames = {
  AdminUser: 'Administrator', Media: 'Media file', 'Media File': 'Media file',
  StaticPage: 'Static page', 'Static Page': 'Static page', Homepage: 'Homepage',
  WebsiteInformation: 'Website information', Show: 'Play', NewsArticle: 'News article',
  ContactMessage: 'Contact message', NewsletterSubscriber: 'Newsletter subscriber',
}

function contentName(value) {
  return contentNames[value] || value?.replace(/([a-z])([A-Z])/g, '$1 $2') || 'Content'
}

function readableDetails(item) {
  const summary = item.summary || `${item.action} ${contentName(item.entityType).toLowerCase()}.`
  const endpoint = summary.match(/^(PUT|POST|PATCH|DELETE) \/api\/admin\//i)
  if (endpoint) {
    const verb = { PUT: 'Updated', PATCH: 'Updated', POST: 'Created or changed', DELETE: 'Deleted or removed' }[endpoint[1].toUpperCase()]
    return `${verb} ${contentName(item.entityType).toLowerCase()}.`
  }
  return summary.replace(/\. Admin endpoint: \/api\/admin\/.*$/i, '.')
}

export function ActivityLogPage() {
  const [filters, setFilters] = useState(initialFilters)
  const [data, setData] = useState(null)
  const [users, setUsers] = useState([])
  const [options, setOptions] = useState({ actions: [], entityTypes: [] })

  useEffect(() => {
    Promise.all([adminApi.activityUsers(), adminApi.activityFilters()]).then(([userNames, filterOptions]) => {
      setUsers(userNames)
      setOptions(filterOptions)
    })
  }, [])
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
    { key: 'entityType', label: 'Content type', render: item => contentName(item.entityType) },
    { key: 'summary', label: 'Details', render: item => <div className="activity-details"><strong>{readableDetails(item)}</strong></div> },
  ]

  return <>
    <PageHeader eyebrow="Administration" title="Activity Log" description="A read-only audit trail of actions by every administrator, including deleted accounts." />
    <section className="admin-panel">
      <div className="communication-summary activity-filter-bar">
        <input placeholder="Search activity…" value={filters.search} onChange={event => update({ search: event.target.value })} />
        <select aria-label="Administrator" value={filters.adminName} onChange={event => update({ adminName: event.target.value })}>
          <option value="">All administrators</option>
          {users.map(name => <option key={name} value={name}>{name}</option>)}
        </select>
        <label className="activity-day-filter"><span>Day</span><input type="date" value={filters.day} onChange={event => update({ day: event.target.value })} /></label>
        <select aria-label="Action" value={filters.action} onChange={event => update({ action: event.target.value })}>
          <option value="">All actions</option>
          {options.actions.map(action => <option key={action} value={action}>{action}</option>)}
        </select>
        <select aria-label="Content type" value={filters.entityType} onChange={event => update({ entityType: event.target.value })}>
          <option value="">All content types</option>
          {options.entityTypes.map(type => <option key={type} value={type}>{contentName(type)}</option>)}
        </select>
        <button className="activity-clear-filters" type="button" onClick={() => setFilters(initialFilters)}>Clear filters</button>
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
