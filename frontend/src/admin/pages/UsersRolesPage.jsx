import { useEffect, useState } from 'react'
import { adminApi } from '../api'
import { DataTable, LoadingSkeleton, PageHeader, StatusBadge, Toast } from '../components/AdminUi'
import { useAdminDialog } from '../components/AdminDialog'
import { useAdminLanguage } from '../AdminLanguageContext'

const blank = { displayName: '', email: '', role: 'ContentEditor', isActive: true, password: '' }
export function UsersRolesPage() {
  const { language, t } = useAdminLanguage()
  const dialog = useAdminDialog(); const [items, setItems] = useState(null); const [form, setForm] = useState(blank); const [selected, setSelected] = useState(null); const [toast, setToast] = useState('')
  const load = () => adminApi.adminUsers().then(setItems); useEffect(() => { load() }, [])
  const edit = item => { setSelected(item); setForm({ ...item, password: '' }) }
  const save = async e => {
    e.preventDefault()
    try {
      if (selected) {
        const { password: _unusedPassword, ...account } = form
        await adminApi.saveAdminUser(selected.id, account)
      } else {
        await adminApi.createAdminUser(form)
      }
      setSelected(null); setForm(blank); setToast('Administrator saved.'); load()
    } catch (error) {
      const validation = Object.values(error.response?.data?.errors ?? {}).flat().join(' ')
      setToast(validation || error.response?.data?.detail || error.response?.data?.title || 'Administrator could not be saved.')
    }
  }
  const reset = async item => { const value = await dialog.prompt({ title: 'Reset password', label: 'New password (at least 12 characters)', confirmLabel: 'Reset password' }); if (!value) return; try { await adminApi.resetAdminPassword(item.id, value); setToast('Password reset successfully.') } catch (error) { setToast(error.response?.data?.detail ?? 'Password could not be reset.') } }
  const remove = async item => { if (!await dialog.confirm({ title: 'Remove administrator access?', message: `${item.displayName} will no longer be able to sign in.`, confirmLabel: 'Remove access', danger: true })) return; try { await adminApi.removeAdminAccess(item.id); setToast('Access removed.'); load() } catch (error) { setToast(error.response?.data?.detail ?? 'Access could not be removed.') } }
  const destroy = async item => { if (!await dialog.confirm({ title: 'Permanently delete this account?', message: `${item.displayName}'s account data will be deleted. Their name will remain only on historical activity entries.`, confirmLabel: 'Delete permanently', danger: true })) return; try { await adminApi.permanentlyDeleteAdminUser(item.id); setToast('Disabled account permanently deleted.'); if (selected?.id === item.id) { setSelected(null); setForm(blank) } load() } catch (error) { setToast(error.response?.data?.detail ?? 'Account could not be deleted.') } }
  if (!items) return <LoadingSkeleton rows={6} />
  const columns = [{ key: 'displayName', label: 'Administrator' }, { key: 'email', label: 'Email' }, { key: 'role', label: 'Role' }, { key: 'isActive', label: 'Status', render: x => <StatusBadge status={x.isActive ? 'Active' : 'Inactive'} /> }, { key: 'lastLoginAt', label: 'Last login', render: x => x.lastLoginAt ? new Date(x.lastLoginAt).toLocaleString(language === 'sq' ? 'sq-AL' : 'en-GB') : t('Never') }, { key: 'actions', label: '', render: x => <div className="table-actions"><button onClick={() => edit(x)}>{t('Edit')}</button>{x.isActive && <button onClick={() => reset(x)}>{t('Reset password')}</button>}{x.isActive ? <button className="danger" onClick={() => remove(x)}>{t('Remove access')}</button> : <button className="danger" onClick={() => destroy(x)}>{t('Delete permanently')}</button>}</div> }]
  return <><PageHeader eyebrow="Administration" title="Users & Roles" description="Create administrators, assign roles, reset passwords and control access." /><section className="admin-panel"><DataTable columns={columns} rows={items} /></section><section className="admin-panel admin-form"><h2>{t(selected ? 'Edit administrator' : 'Create administrator')}</h2><form className="form-grid" onSubmit={save}><label>{t('Full name')}<input required value={form.displayName} onChange={e => setForm({ ...form, displayName: e.target.value })} /></label><label>Email<input required type="email" value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} /></label><label>{t('Role')}<select value={form.role} onChange={e => setForm({ ...form, role: e.target.value })}><option value="ContentEditor">{t('Content Editor')}</option><option value="SuperAdmin">{t('Super Admin')}</option></select></label><label className="admin-check"><input type="checkbox" checked={form.isActive} onChange={e => setForm({ ...form, isActive: e.target.checked })} /> {t('Active account')}</label>{!selected && <label>{t('Password')}<input required minLength="12" type="password" value={form.password} onChange={e => setForm({ ...form, password: e.target.value })} /></label>}<div className="full table-actions"><button className="admin-primary-button" type="submit">{t(selected ? 'Save administrator' : 'Create administrator')}</button>{selected && <button type="button" onClick={() => { setSelected(null); setForm(blank) }}>{t('Cancel')}</button>}</div></form></section><Toast message={toast} onClose={() => setToast('')} /></>
}
