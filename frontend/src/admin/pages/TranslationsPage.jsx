import { useEffect, useState } from 'react'
import { adminApi } from '../api'
import { DataTable, PageHeader, StatusBadge } from '../components/AdminUi'
import { useAdminLanguage } from '../AdminLanguageContext'

export function TranslationsPage() {
  const { language, t } = useAdminLanguage()
  const locale = language === 'sq' ? 'sq-AL' : 'en-GB'
  const [type, setType] = useState(''); const [data, setData] = useState({ items: [], totalCount: 0 })
  useEffect(() => { adminApi.translations({ contentType: type || undefined, page: 1, pageSize: 50 }).then(setData) }, [type])
  const columns = [{ key: 'contentType', label: 'Content type', render: x => t(x.contentType) }, { key: 'title', label: 'Content' }, { key: 'missingLanguage', label: 'Language to review', render: x => t(x.missingLanguage === 'sq' ? 'Albanian' : 'English') }, { key: 'status', label: 'State', render: x => <StatusBadge status={x.status} /> }, { key: 'updatedAt', label: 'Last changed', render: x => new Date(x.updatedAt).toLocaleDateString(locale) }]
  return <><PageHeader eyebrow="Website" title="Translations" description="Review missing and partially translated public content before publication." />
    <div className="translation-summary"><article><strong>{data.items.filter(x => x.missingLanguage === 'sq').length}</strong><span>{t('Albanian to review')}</span></article><article><strong>{data.items.filter(x => x.missingLanguage === 'en').length}</strong><span>{t('English to review')}</span></article><article><strong>{data.totalCount}</strong><span>{t('Total issues')}</span></article></div>
    <section className="admin-panel"><div className="panel-heading"><h2>{t('Translation issues')}</h2><label className="compact-filter">{t('Content type')}<select value={type} onChange={e => setType(e.target.value)}><option value="">{t('All')}</option><option value="shows">{t('Shows')}</option><option value="news">{t('News')}</option><option value="pages">{t('Pages')}</option><option value="pitf">PITF</option><option value="website">{t('Homepage & website')}</option></select></label></div><div className="translation-note">{t('Missing means the language does not exist. Incomplete means one or more required fields are blank and should be reviewed before publication.')}</div><DataTable columns={columns} rows={data.items} emptyText="All checked content has complete Albanian and English translations." /></section></>
}
