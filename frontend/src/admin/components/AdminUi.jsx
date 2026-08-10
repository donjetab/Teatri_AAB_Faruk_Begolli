import { useEffect, useRef } from 'react'

import { useAdminLanguage } from '../AdminLanguageContext'

export function PageHeader({ eyebrow, title, description, actions }) {
  const { t } = useAdminLanguage()
  return <header className="admin-page-header"><div><span>{t(eyebrow)}</span><h1>{t(title)}</h1><p>{t(description)}</p></div>{actions && <div className="admin-page-actions">{actions}</div>}</header>
}
export function StatusBadge({ status }) { const { t } = useAdminLanguage(); return <span className={`status-badge status-${String(status).toLowerCase().replaceAll(' ', '-')}`}>{t(status)}</span> }
export function LoadingSkeleton({ rows = 4 }) { return <div className="admin-skeleton">{Array.from({ length: rows }, (_, i) => <i key={i} />)}</div> }
export function EmptyState({ title, text }) { const { t } = useAdminLanguage(); return <div className="admin-empty"><strong>{t(title)}</strong><p>{t(text)}</p></div> }
export function Toast({ message, type = 'success', onClose, duration }) {
  const { t } = useAdminLanguage()
  const closeRef = useRef(onClose)
  closeRef.current = onClose
  useEffect(() => {
    if (!message) return undefined
    const timeout = duration ?? (type === 'error' ? 7000 : type === 'warning' ? 5500 : 4000)
    const timer = window.setTimeout(() => closeRef.current?.(), timeout)
    return () => window.clearTimeout(timer)
  }, [message, type, duration])
  return message ? <div className={`admin-toast ${type}`} role={type === 'error' ? 'alert' : 'status'}>{t(message)}<button type="button" onClick={onClose} aria-label={t('Dismiss notification')}>×</button></div> : null
}
export function LanguageTabs({ active, onChange }) { const { t } = useAdminLanguage(); return <div className="language-tabs" role="tablist">{['sq', 'en'].map(code => <button type="button" role="tab" aria-selected={active === code} className={active === code ? 'active' : ''} onClick={() => onChange(code)} key={code}>{code === 'sq' ? 'Shqip' : t('English')}</button>)}</div> }
export function DataTable({ columns, rows, emptyText = 'No records found.' }) {
  const { t } = useAdminLanguage()
  if (!rows?.length) return <EmptyState title="Nothing here yet" text={emptyText} />
  return <div className="admin-table-wrap"><table className="admin-table"><thead><tr>{columns.map(c => <th key={c.key}>{t(c.label)}</th>)}</tr></thead><tbody>{rows.map((row, i) => <tr key={row.id ?? i}>{columns.map(c => <td key={c.key}>{c.render ? c.render(row) : row[c.key]}</td>)}</tr>)}</tbody></table></div>
}
