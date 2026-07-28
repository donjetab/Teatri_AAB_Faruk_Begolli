export function PageHeader({ eyebrow, title, description, actions }) {
  return <header className="admin-page-header"><div><span>{eyebrow}</span><h1>{title}</h1><p>{description}</p></div>{actions && <div className="admin-page-actions">{actions}</div>}</header>
}
export function StatusBadge({ status }) { return <span className={`status-badge status-${String(status).toLowerCase().replaceAll(' ', '-')}`}>{status}</span> }
export function LoadingSkeleton({ rows = 4 }) { return <div className="admin-skeleton">{Array.from({ length: rows }, (_, i) => <i key={i} />)}</div> }
export function EmptyState({ title, text }) { return <div className="admin-empty"><strong>{title}</strong><p>{text}</p></div> }
export function Toast({ message, type = 'success', onClose }) { return message ? <div className={`admin-toast ${type}`} role="status">{message}<button onClick={onClose}>×</button></div> : null }
export function LanguageTabs({ active, onChange }) { return <div className="language-tabs" role="tablist">{['sq', 'en'].map(code => <button type="button" role="tab" aria-selected={active === code} className={active === code ? 'active' : ''} onClick={() => onChange(code)} key={code}>{code === 'sq' ? 'Shqip' : 'English'}</button>)}</div> }
export function DataTable({ columns, rows, emptyText = 'No records found.' }) {
  if (!rows?.length) return <EmptyState title="Nothing here yet" text={emptyText} />
  return <div className="admin-table-wrap"><table className="admin-table"><thead><tr>{columns.map(c => <th key={c.key}>{c.label}</th>)}</tr></thead><tbody>{rows.map((row, i) => <tr key={row.id ?? i}>{columns.map(c => <td key={c.key}>{c.render ? c.render(row) : row[c.key]}</td>)}</tr>)}</tbody></table></div>
}
