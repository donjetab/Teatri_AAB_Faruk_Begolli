import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { adminApi } from '../api'
import { DataTable, LoadingSkeleton, PageHeader, StatusBadge } from '../components/AdminUi'

const size = bytes => `${(bytes / 1024 / 1024).toFixed(1)} MB`

export function BackupsSystemPage() {
  const [data, setData] = useState(null)
  useEffect(() => { adminApi.systemStatus().then(setData) }, [])
  if (!data) return <LoadingSkeleton rows={6} />
  const mediaColumns = [
    { key: 'fileName', label: 'Missing file' },
    { key: 'fileUrl', label: 'Expected location' },
    { key: 'open', label: '', render: () => <Link to="/admin/media">Open Media Library →</Link> },
  ]
  const errorColumns = [
    { key: 'createdAt', label: 'Time', render: row => new Date(row.createdAt).toLocaleString() },
    { key: 'eventType', label: 'Type' },
    { key: 'summary', label: 'What happened' },
    { key: 'requestPath', label: 'Request' },
    { key: 'correlationId', label: 'Reference ID' },
  ]
  return <>
    <PageHeader eyebrow="Administration" title="Backups & System" description="Read-only operational status. This screen never runs a backup or restore." />
    <div className="translation-summary"><article><strong><StatusBadge status={data.databaseStatus} /></strong><span>Database</span></article><article><strong>{size(data.mediaStorageBytes)}</strong><span>{data.mediaFileCount} stored files</span></article><article><strong>{data.brokenMediaReferences}</strong><span>Broken media references</span></article></div>
    <section className="admin-panel"><h2>How backups work</h2><p>{data.backupManagement}</p><p className="admin-help">The database contains website text and records. Media storage contains uploaded images, documents and videos. A complete recovery requires both. Because no backup provider is connected to this application, dates remain “not reported” rather than pretending a backup exists.</p><dl className="system-details"><div><dt>Last database backup</dt><dd>{data.lastDatabaseBackupAt ? new Date(data.lastDatabaseBackupAt).toLocaleString() : 'Not reported by an external backup provider'}</dd></div><div><dt>Last media backup</dt><dd>{data.lastMediaBackupAt ? new Date(data.lastMediaBackupAt).toLocaleString() : 'Not reported by an external backup provider'}</dd></div><div><dt>Environment</dt><dd>{data.environment}</dd></div><div><dt>Application version</dt><dd>{data.applicationVersion}</dd></div><div><dt>Failed uploads</dt><dd>{data.failedUploads} recorded</dd></div></dl></section>
    <section className="admin-panel"><h2>Broken media references</h2><p className="admin-help">Only active Media Library records are checked here. Deleted media is ignored. A listed record still exists as active, but its physical file is missing from upload storage.</p><DataTable columns={mediaColumns} rows={data.brokenMedia ?? []} emptyText="No active media records point to missing files." /></section>
    <section className="admin-panel"><h2>Recent application errors</h2><p className="admin-help">Use the reference ID to match an error with server logs. Technical exception details are never exposed here.</p><DataTable columns={errorColumns} rows={data.recentErrors ?? []} emptyText="No recent server errors have been recorded." /></section>
  </>
}
