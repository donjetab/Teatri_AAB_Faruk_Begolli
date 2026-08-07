import { useEffect, useState } from 'react'
import { adminApi } from '../api'
import { LanguageTabs, LoadingSkeleton, PageHeader, Toast } from '../components/AdminUi'
import { MediaPicker } from '../components/MediaPicker'
import publicHeaderLogo from '../../assets/Teatri Logo/Teatri White.png'
import publicFooterLogo from '../../assets/Teatri Logo/Teatri Logo -W-RED.png'

const publicFavicon = `${import.meta.env.BASE_URL}AAB-Theatre-Icon.png`

export function WebsiteInformationPage() {
  const [form, setForm] = useState(null); const [saved, setSaved] = useState(null); const [lang, setLang] = useState('sq'); const [toast, setToast] = useState('')
  useEffect(() => { adminApi.website().then(data => { setForm(data); setSaved(JSON.stringify(data)) }) }, [])
  const dirty = form && JSON.stringify(form) !== saved
  useEffect(() => { const warn = e => { if (dirty) { e.preventDefault(); e.returnValue = '' } }; addEventListener('beforeunload', warn); return () => removeEventListener('beforeunload', warn) }, [dirty])
  if (!form) return <LoadingSkeleton />
  const translation = form.translations.find(x => x.languageCode === lang)
  const setField = (key, value) => setForm({ ...form, [key]: value })
  const setMedia = (idKey, urlKey, item) => setForm(current => ({
    ...current,
    [idKey]: item?.id ?? null,
    [urlKey]: item?.fileUrl ?? null,
  }))
  const setTranslation = (key, value) => setForm({ ...form, translations: form.translations.map(x => x.languageCode === lang ? { ...x, [key]: value } : x) })
  const save = async e => { e.preventDefault(); const data = await adminApi.saveWebsite(form); setForm(data); setSaved(JSON.stringify(data)); setToast('Website information saved.') }
  return <><PageHeader eyebrow="Website" title="Website Information" description="Manage public contact details, branding, social links and localized footer content." actions={<span className={dirty ? 'unsaved-chip' : 'saved-chip'}>{dirty ? 'Unsaved changes' : 'All changes saved'}</span>} /><form className="admin-form" onSubmit={save}>
    <section className="admin-panel"><h2>Contact details</h2><div className="form-grid"><label>Address *<input required value={form.address} onChange={e => setField('address', e.target.value)} /></label><label>Phone *<input required pattern="[+0-9() .-]{6,}" value={form.phone} onChange={e => setField('phone', e.target.value)} /></label><label>Email *<input required type="email" value={form.email} onChange={e => setField('email', e.target.value)} /></label><label>Ticket reservation destination<select value={form.reservationUrl ?? '#/sq/rezervo'} onChange={e => setField('reservationUrl', e.target.value)}><option value="#/sq/rezervo">Reservation page</option></select><small>The matching Albanian or English page is opened automatically.</small></label><label>Facebook URL<input type="url" value={form.facebookUrl ?? ''} onChange={e => setField('facebookUrl', e.target.value)} /></label><label>Instagram URL<input type="url" value={form.instagramUrl ?? ''} onChange={e => setField('instagramUrl', e.target.value)} /></label></div></section>
    <section className="admin-panel"><h2>Localized information</h2><LanguageTabs active={lang} onChange={setLang} /><div className="form-grid"><label>Theatre name *<input required value={translation.theatreName} onChange={e => setTranslation('theatreName', e.target.value)} /></label><label>Public address *<input required value={translation.addressDisplayText} onChange={e => setTranslation('addressDisplayText', e.target.value)} /></label><label className="full">Footer copyright text<textarea rows="3" value={translation.footerCopyrightText} onChange={e => setTranslation('footerCopyrightText', e.target.value)} /></label></div></section>
    <section className="admin-panel"><h2>Brand media</h2><p className="admin-help">Preview the images currently used by the website, or choose and upload replacements from the Media Library.</p><div className="form-grid brand-media-grid">
      <div><MediaPicker label="Theatre logo" value={form.logoMediaAssetId} currentUrl={form.logoUrl || publicHeaderLogo} onSelect={item => setMedia('logoMediaAssetId', 'logoUrl', item)} /><p className="admin-help">The main logo displayed in the website header.</p></div>
      <div><MediaPicker label="Footer logo" value={form.footerLogoMediaAssetId} currentUrl={form.footerLogoUrl || publicFooterLogo} onSelect={item => setMedia('footerLogoMediaAssetId', 'footerLogoUrl', item)} /><p className="admin-help">Used only in the public website footer.</p></div>
      <div><MediaPicker label="Browser favicon" value={form.faviconMediaAssetId} currentUrl={form.faviconUrl || publicFavicon} onSelect={item => setMedia('faviconMediaAssetId', 'faviconUrl', item)} /><p className="admin-help">The small theatre icon displayed in the browser tab.</p></div>
      <div><MediaPicker label="Default social-sharing image" value={form.socialSharingMediaAssetId} currentUrl={form.socialSharingImageUrl} onSelect={item => setMedia('socialSharingMediaAssetId', 'socialSharingImageUrl', item)} /><p className="admin-help">Used as the link-preview cover on social media. No default image is currently configured.</p></div>
      <label>Facebook display name<input value={form.facebookDisplayName ?? ''} onChange={e => setField('facebookDisplayName', e.target.value)} placeholder="Teatri AAB Faruk Begolli" /></label>
      <label>Instagram display name<input value={form.instagramDisplayName ?? ''} onChange={e => setField('instagramDisplayName', e.target.value)} placeholder="@teatriaabfarukbegolli" /></label>
    </div></section>
    <div className="sticky-save"><button className="admin-primary-button" disabled={!dirty}>Save changes</button><a className="admin-outline-button" href={`#/${lang}`} target="_blank">Preview public page ↗</a></div></form><Toast message={toast} onClose={() => setToast('')} /></>
}
