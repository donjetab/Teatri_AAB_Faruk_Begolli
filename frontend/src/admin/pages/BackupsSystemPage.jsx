import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { adminApi } from '../api'
import { DataTable, LoadingSkeleton, PageHeader, StatusBadge } from '../components/AdminUi'
import { useAdminLanguage } from '../AdminLanguageContext'

const size = bytes => `${(bytes / 1024 / 1024).toFixed(1)} MB`

export function BackupsSystemPage() {
  const { language, t } = useAdminLanguage()
  const locale = language === 'sq' ? 'sq-AL' : 'en-GB'
  const [data, setData] = useState(null)
  useEffect(() => { adminApi.systemStatus().then(setData) }, [])
  if (!data) return <LoadingSkeleton rows={6} />
  const mediaColumns = [
    { key: 'fileName', label: 'Missing file' },
    { key: 'fileUrl', label: 'Expected location' },
    { key: 'open', label: '', render: () => <Link to="/admin/media">{t('Open Media Library')} →</Link> },
  ]
  return <>
    <PageHeader eyebrow="Administration" title="Backups & System" description="Read-only operational status. This screen never runs a backup or restore." />
    <div className="translation-summary"><article><strong><StatusBadge status={data.databaseStatus} /></strong><span>{t('Database')}</span></article><article><strong>{size(data.mediaStorageBytes)}</strong><span>{data.mediaFileCount} {t('stored files')}</span></article><article><strong>{data.brokenMediaReferences}</strong><span>{t('Broken media references')}</span></article></div>
    <section className="admin-panel"><h2>{t('How backups work')}</h2><p>{t(data.backupManagement)}</p><p className="admin-help">{t('The database contains website text and records. Media storage contains uploaded images, documents and videos. A complete recovery requires both. Because no backup provider is connected to this application, dates remain “not reported” rather than pretending a backup exists.')}</p><dl className="system-details"><div><dt>{t('Last database backup')}</dt><dd>{data.lastDatabaseBackupAt ? new Date(data.lastDatabaseBackupAt).toLocaleString(locale) : t('Not reported by an external backup provider')}</dd></div><div><dt>{t('Last media backup')}</dt><dd>{data.lastMediaBackupAt ? new Date(data.lastMediaBackupAt).toLocaleString(locale) : t('Not reported by an external backup provider')}</dd></div><div><dt>{t('Environment')}</dt><dd>{t(data.environment)}</dd></div><div><dt>{t('Application version')}</dt><dd>{data.applicationVersion}</dd></div><div><dt>{t('Failed uploads')}</dt><dd>{data.failedUploads} {t('recorded')}</dd></div></dl></section>
    <section className="admin-panel"><h2>{t('Broken media references')}</h2><p className="admin-help">{t('Only active Media Library records are checked here. Deleted media is ignored. A listed record still exists as active, but its physical file is missing from upload storage.')}</p><DataTable columns={mediaColumns} rows={data.brokenMedia ?? []} emptyText="No active media records point to missing files." /></section>
  </>
}
