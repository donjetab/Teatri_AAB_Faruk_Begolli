import { useEffect, useState } from 'react'
import { adminApi } from '../api'
import { useAdminDialog } from '../components/AdminDialog'
import { LoadingSkeleton, PageHeader, StatusBadge, Toast } from '../components/AdminUi'

export function MessagesPage() {
  const dialog = useAdminDialog()
  const [filters, setFilters] = useState({ search: '', status: '' })
  const [data, setData] = useState(null)
  const [selected, setSelected] = useState(null)
  const [toast, setToast] = useState('')
  const load = () => adminApi.messages(filters).then(setData)

  useEffect(() => { const timer = setTimeout(() => { void load() }, 180); return () => clearTimeout(timer) }, [filters.search, filters.status])

  const open = item => {
    setSelected(item)
    if (item.status === 'New') adminApi.saveMessage(item.id, { status: 'Read', internalNotes: item.internalNotes }).then(updated => { setSelected(updated); load() })
  }
  const save = async () => { const updated = await adminApi.saveMessage(selected.id, { status: selected.status, internalNotes: selected.internalNotes }); setSelected(updated); setToast('Message updated.'); load() }
  const remove = async () => {
    if (!await dialog.confirm({ title: 'Delete spam message?', message: 'This contact message will be permanently removed.', confirmLabel: 'Delete message', danger: true })) return
    try {
      if (selected.status === 'Spam') await adminApi.saveMessage(selected.id, { status: 'Spam', internalNotes: selected.internalNotes })
      await adminApi.deleteMessage(selected.id); setSelected(null); setToast('Spam deleted.'); await load()
    } catch (error) { setToast(error.response?.data?.detail ?? error.response?.data?.title ?? 'Spam could not be deleted.') }
  }
  const replyUrl = selected ? `https://mail.google.com/mail/?view=cm&fs=1&to=${encodeURIComponent(selected.email)}&su=${encodeURIComponent(selected.subject.toLowerCase().startsWith('re:') ? selected.subject : `Re: ${selected.subject}`)}&body=${encodeURIComponent(`Hello ${selected.name},\n\nThank you for contacting Teatri AAB “Faruk Begolli”.\n\n\n\nKind regards,\nTeatri AAB “Faruk Begolli”`)}` : ''

  return <>
    <PageHeader eyebrow="Communication" title="Contact Messages" description="Review contact submissions while keeping personal information inside the protected admin area." />
    <section className="admin-panel"><div className="communication-summary"><div><strong>{data?.unreadCount ?? 0}</strong><span>New messages</span></div><div><strong>{data?.totalCount ?? 0}</strong><span>Matching messages</span></div><input placeholder="Search sender, email or subject…" value={filters.search} onChange={e => setFilters({ ...filters, search: e.target.value })} /><select value={filters.status} onChange={e => setFilters({ ...filters, status: e.target.value })}><option value="">All statuses</option>{['New','Read','Resolved','Archived','Spam'].map(x => <option key={x}>{x}</option>)}</select></div></section>
    {!data ? <LoadingSkeleton /> : <section className="admin-panel"><div className="admin-table-wrap"><table className="admin-table"><thead><tr><th>Sender</th><th>Subject</th><th>Submitted</th><th>Status</th><th /></tr></thead><tbody>{data.items.map(item => <tr key={item.id} className={item.status === 'New' ? 'unread-row' : ''}><td><strong>{item.name}</strong><small className="private-detail">{item.email}</small></td><td>{item.subject}</td><td>{new Date(item.createdAt).toLocaleString()}</td><td><StatusBadge status={item.status} /></td><td><button className="admin-text-button" onClick={() => open(item)}>Open</button></td></tr>)}</tbody></table></div></section>}
    {selected && <div className="admin-modal-backdrop" onMouseDown={e => e.target === e.currentTarget && setSelected(null)}><section className="admin-modal message-detail">
      <div className="admin-modal-heading"><div><span>Contact submission</span><h2>{selected.subject}</h2></div><button type="button" onClick={() => setSelected(null)}>×</button></div>
      <div className="message-sender"><div><strong>{selected.name}</strong><a href={`mailto:${selected.email}`}>{selected.email}</a></div><time>{new Date(selected.createdAt).toLocaleString()}</time></div>
      <div className="message-body">{selected.message}</div>
      <form className="admin-form" onSubmit={event => { event.preventDefault(); save() }}><div className="form-grid"><label>Status<select value={selected.status} onChange={e => setSelected({ ...selected, status: e.target.value })}>{['New','Read','Resolved','Archived','Spam'].map(x => <option key={x}>{x}</option>)}</select></label><label className="full">Internal notes<textarea rows="4" value={selected.internalNotes ?? ''} onChange={e => setSelected({ ...selected, internalNotes: e.target.value })} placeholder="Visible only to administrators" /></label></div>
        <div className="admin-modal-actions">{selected.status === 'Spam' && <button type="button" className="admin-danger-button" onClick={remove}>Delete spam</button>}<a className="admin-outline-button message-reply-button" href={replyUrl} target="_blank" rel="noopener noreferrer">Reply in Gmail ↗</a><button className="admin-primary-button">Save message</button></div>
      </form>
    </section></div>}
    <Toast message={toast} onClose={() => setToast('')} />
  </>
}
