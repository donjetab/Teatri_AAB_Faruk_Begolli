import { useEffect, useState } from 'react'
import { adminApi } from '../api'
import { LoadingSkeleton, PageHeader, Toast } from '../components/AdminUi'

export function SettingsPage() {
  const [data, setData] = useState(null)
  const [toast, setToast] = useState('')
  useEffect(() => { adminApi.settings().then(setData) }, [])
  if (!data) return <LoadingSkeleton rows={5} />
  const save = async () => { try { const result = await adminApi.saveSettings({ defaultLanguage: data.defaultLanguage }); setData(result); setToast('Default language saved.') } catch (error) { setToast(error.response?.data?.detail ?? 'Settings could not be saved.') } }
  return <>
    <PageHeader eyebrow="Administration" title="Settings" description="Application-wide configuration, with editable controls only where they have a real effect." />
    <section className="admin-panel admin-form"><h2>Language</h2><div className="form-grid"><label>Default website language<select value={data.defaultLanguage} onChange={event => setData({ ...data, defaultLanguage: event.target.value })}>{data.supportedLanguages.map(code => <option key={code} value={code}>{code === 'sq' ? 'Albanian' : 'English'}</option>)}</select></label><label>Supported languages<input value={data.supportedLanguages.map(code => code === 'sq' ? 'Albanian' : 'English').join(', ')} disabled /></label><label className="admin-check"><input type="checkbox" checked={data.missingTranslationsBlockPublication} disabled /> Missing translations block publication</label></div><p className="admin-help">Supported languages and publication validation are part of the application’s data model and cannot safely be changed from a checkbox yet.</p><button className="admin-primary-button" onClick={save}>Save language setting</button></section>
    <section className="admin-panel admin-form"><h2>Uploads and display</h2><div className="form-grid"><label>Default pagination size<input value={data.defaultPageSize} disabled /></label><label>Maximum upload size<input value={`${Math.round(data.maximumUploadBytes / 1024 / 1024)} MB`} disabled /></label><label>Allowed uploads<input value={data.allowedUploadTypes.join(', ')} disabled /></label><label>Date format<input value={data.dateFormat} disabled /></label><label>Time format<input value={data.timeFormat} disabled /></label></div><p className="admin-help">These values are enforced by server configuration or the interface. Making them editable here without changing those systems would create fake settings.</p></section>
    <section className="admin-panel"><h2>Settings managed elsewhere</h2><div className="settings-destinations"><a href="#/admin/website-information"><strong>Default social-sharing image</strong><span>Edit it in Website Information →</span></a><a href="#/admin/users"><strong>Subscriber export access</strong><span>Control it through administrator roles →</span></a><a href="#/admin/seo"><strong>Missing translations and content quality</strong><span>Review SEO & Content Issues →</span></a></div></section>
    <Toast message={toast} onClose={() => setToast('')} />
  </>
}
