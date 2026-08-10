import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { adminApi } from '../api'
import { PageHeader } from '../components/AdminUi'
import { useAdminLanguage } from '../AdminLanguageContext'

export function ChangePasswordPage() {
  const { t } = useAdminLanguage()
  const [form, setForm] = useState({ currentPassword: '', newPassword: '', confirmPassword: '' })
  const [error, setError] = useState(''); const [busy, setBusy] = useState(false)
  const navigate = useNavigate()
  const submit = async e => {
    e.preventDefault(); setError('')
    if (form.newPassword !== form.confirmPassword) return setError('The new passwords do not match.')
    setBusy(true)
    try { await adminApi.changePassword({ currentPassword: form.currentPassword, newPassword: form.newPassword }); navigate('/admin/login', { replace: true }) }
    catch (e) { setError(e.response?.data?.detail ?? 'The password could not be changed.') }
    finally { setBusy(false) }
  }
  return <><PageHeader eyebrow="Account" title="Change Password" description="Use a unique password with at least 12 characters. You will be signed out after it changes." /><form className="admin-form" onSubmit={submit}><section className="admin-panel"><div className="form-grid"><label className="full">{t('Current password')}<input type="password" autoComplete="current-password" required value={form.currentPassword} onChange={e => setForm({ ...form, currentPassword: e.target.value })} /></label><label>{t('New password')}<input type="password" autoComplete="new-password" minLength="12" required value={form.newPassword} onChange={e => setForm({ ...form, newPassword: e.target.value })} /></label><label>{t('Confirm new password')}<input type="password" autoComplete="new-password" minLength="12" required value={form.confirmPassword} onChange={e => setForm({ ...form, confirmPassword: e.target.value })} /></label></div>{error && <div className="admin-form-error">{t(error)}</div>}</section><button className="admin-primary-button" disabled={busy}>{t(busy ? 'Changing…' : 'Change password')}</button></form></>
}
