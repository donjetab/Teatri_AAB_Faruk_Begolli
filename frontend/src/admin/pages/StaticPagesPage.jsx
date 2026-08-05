import { useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { adminApi } from '../api'
import { LanguageTabs, LoadingSkeleton, PageHeader, Toast } from '../components/AdminUi'
import { MediaPicker } from '../components/MediaPicker'
import { RichTextEditor } from '../components/RichTextEditor'
import { resolveMediaUrl } from '../../api/client'
import aboutHero from '../../assets/teatri/perne-bg.jpg'
import aboutIntro from '../../assets/teatri/AAB.jpg'
import aboutParallax from '../../assets/teatri/AAB-THEATER-SCENE.jpg'

const sectionLabels = {
  about: 'About', contact: 'Contact', 'shows-introduction': 'Shows', 'news-introduction': 'News',
  reservations: 'Reservations', 'pitf-introduction': 'PITF', 'gallery-introduction': 'Gallery',
}
const sectionOrder = ['about', 'contact', 'shows-introduction', 'reservations', 'pitf-introduction', 'gallery-introduction', 'news-introduction']

function PageList({ items, selectedId, onSelect }) {
  const render = item => <button type="button" className={selectedId === item.id ? 'active' : ''} key={item.id} onClick={() => onSelect(item.id)}><span><strong>{sectionLabels[item.pageKey]}</strong><small>{item.translations.find(x => x.languageCode === 'sq')?.title}</small></span></button>
  const visibleItems = sectionOrder.map(key => items.find(item => item.pageKey === key)).filter(Boolean)
  return <aside className="admin-panel static-page-list"><p className="static-page-group-label">Website sections</p>{visibleItems.map(render)}</aside>
}

function TextHeaderFields({ translation, change }) {
  return <><label>Page title<input value={translation.title} onChange={e => change('title', e.target.value)} /></label><label>Text below the title<input value={translation.subtitle ?? ''} onChange={e => change('subtitle', e.target.value)} /></label></>
}

export function StaticPagesPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [items, setItems] = useState(null)
  const [selectedId, setSelectedId] = useState(null)
  const [form, setForm] = useState(null)
  const [language, setLanguage] = useState('sq')
  const [website, setWebsite] = useState(null)
  const [gallery, setGallery] = useState([])
  const [draggedGalleryId, setDraggedGalleryId] = useState(null)
  const [previewImage, setPreviewImage] = useState(null)
  const [toast, setToast] = useState('')

  useEffect(() => { adminApi.staticPages().then(result => {
    const requestedId = Number(searchParams.get('page'))
    const selected = result.find(item => item.id === requestedId) ?? result[0]
    setItems(result); setSelectedId(selected?.id); setForm(selected)
    if (searchParams.get('language') === 'en') setLanguage('en')
  }) }, [])
  const loadGallery = () => adminApi.generalGallery().then(setGallery).catch(() => setGallery([]))
  useEffect(() => { adminApi.website().then(setWebsite); loadGallery() }, [])
  const translation = useMemo(() => form?.translations.find(x => x.languageCode === language), [form, language])
  const select = id => { setSelectedId(id); setForm(items.find(x => x.id === id)); setSearchParams({ page: String(id), language }) }
  const change = (field, value) => setForm(current => ({ ...current, translations: current.translations.map(x => x.languageCode === language ? { ...x, [field]: value } : x) }))
  const chooseImage = (idField, urlField, media) => setForm(current => ({ ...current, [idField]: media?.id ?? null, [urlField]: media?.fileUrl ?? null }))
  const addGalleryImage = async media => { if (!media) return; await adminApi.addGeneralGalleryMedia(media.id); await loadGallery(); setToast('Picture added to the About gallery.') }
  const removeGalleryImage = async id => { await adminApi.removeGeneralGalleryMedia(id); await loadGallery(); setToast('Picture removed from the About gallery.') }
  const reorderGallery = async targetId => {
    if (!draggedGalleryId || draggedGalleryId === targetId) return
    const ordered = [...gallery]; const from = ordered.findIndex(x => x.id === draggedGalleryId); const to = ordered.findIndex(x => x.id === targetId)
    const [moved] = ordered.splice(from, 1); ordered.splice(to, 0, moved); setGallery(ordered); setDraggedGalleryId(null)
    await adminApi.reorderGeneralGalleryMedia(ordered.map(x => x.id)); setToast('Gallery order saved.')
  }
  const save = async () => {
    const payload = { ...form, isPublished: true }
    const saved = await adminApi.saveStaticPage(form.id, payload)
    if (form.pageKey === 'contact' && website) setWebsite(await adminApi.saveWebsite(website))
    setForm(saved); setItems(current => current.map(x => x.id === saved.id ? saved : x)); setToast('Page saved.')
  }
  if (!items || !form || !translation) return <LoadingSkeleton rows={9} />
  const isAbout = form.pageKey === 'about'
  const isContact = form.pageKey === 'contact'

  return <>
    <PageHeader eyebrow="Content" title="Pages" description="Edit the text and images that are actually visible on each public page." />
    <div className="static-pages-layout">
      <PageList items={items} selectedId={selectedId} onSelect={select} />
      <section className={`admin-panel admin-form language-${language}`}>
        <div className="panel-heading"><div><h2>{sectionLabels[form.pageKey]}</h2><p>Public page content</p></div><LanguageTabs active={language} onChange={code => { setLanguage(code); setSearchParams({ page: String(form.id), language: code }) }} /></div>
        <div className="form-grid">
          <TextHeaderFields translation={translation} change={change} />

          {isAbout && <>
            <div className="full about-copy-editor"><div className="admin-rich-field"><span>About paragraphs</span><RichTextEditor key={`${form.id}-${language}`} value={translation.content} onChange={value => change('content', value)} /></div><MediaPicker label="Picture beside the paragraphs" value={form.featuredMediaAssetId} currentUrl={form.featuredImageUrl || aboutIntro} onSelect={media => chooseImage('featuredMediaAssetId', 'featuredImageUrl', media)} /></div>
            <div className="full page-image-fields">
              <MediaPicker label="Header background" value={form.socialSharingMediaAssetId} currentUrl={form.socialSharingImageUrl || aboutHero} onSelect={media => chooseImage('socialSharingMediaAssetId', 'socialSharingImageUrl', media)} />
              <MediaPicker label="Parallax picture" value={form.parallaxMediaAssetId} currentUrl={form.parallaxImageUrl || aboutParallax} onSelect={media => chooseImage('parallaxMediaAssetId', 'parallaxImageUrl', media)} />
            </div>
            <div className="full page-statistics"><h3>Statistics</h3>{['One','Two','Three'].map((key, index) => <div className="admin-panel" key={key}><strong>Statistic {index + 1}</strong><label>Number or value<input value={translation[`stat${key}Value`] ?? ''} onChange={e => change(`stat${key}Value`, e.target.value)} /></label><label>Label<input value={translation[`stat${key}Label`] ?? ''} onChange={e => change(`stat${key}Label`, e.target.value)} /></label></div>)}</div>
            <label className="full">Parallax quotation<textarea rows="4" value={translation.quoteText ?? ''} onChange={e => change('quoteText', e.target.value)} /></label><label className="full">Quotation author<input value={translation.quoteAuthor ?? ''} onChange={e => change('quoteAuthor', e.target.value)} /></label>
            <div className="full about-gallery-admin"><div className="panel-heading"><div><h3>About gallery</h3><p>Drag pictures to reorder them. The first six appear on the About page.</p></div><div className="about-gallery-actions"><MediaPicker compact label="+ Add or upload picture" onSelect={addGalleryImage} /><Link className="admin-outline-button" to="/admin/gallery">Open full gallery</Link></div></div><div className="about-gallery-admin-grid">{gallery.map(item => <article className="about-gallery-admin-card" key={item.id} draggable onDragStart={() => setDraggedGalleryId(item.id)} onDragOver={event => event.preventDefault()} onDrop={() => reorderGallery(item.id)}><button type="button" className="about-gallery-picture" onClick={() => setPreviewImage(resolveMediaUrl(item.fileUrl))}><img src={resolveMediaUrl(item.fileUrl)} alt={item.altText || ''} /></button><div><span className="gallery-drag-handle" title="Drag to reorder">⠿ Drag</span><button type="button" onClick={() => removeGalleryImage(item.id)}>Remove</button></div></article>)}</div></div>
          </>}

          {isContact && website && <>
            <div className="full panel-heading"><div><h3>Map</h3><p>Used in the location card on the Contact page.</p></div></div>
            <label className="full">Google Maps embed URL<input value={form.mapEmbedUrl ?? ''} onChange={e => setForm({ ...form, mapEmbedUrl: e.target.value })} /></label><label className="full">Link opened when the address is clicked<input value={form.mapLinkUrl ?? ''} onChange={e => setForm({ ...form, mapLinkUrl: e.target.value })} /></label>
            <div className="full panel-heading"><div><h3>Contact and social media</h3><p>Also used in the website header and footer.</p></div></div>
            <label>Address<input value={website.address} onChange={e => setWebsite({ ...website, address: e.target.value })} /></label><label>Phone<input value={website.phone} onChange={e => setWebsite({ ...website, phone: e.target.value })} /></label><label>Email<input type="email" value={website.email} onChange={e => setWebsite({ ...website, email: e.target.value })} /></label><label>Facebook URL<input type="url" value={website.facebookUrl ?? ''} onChange={e => setWebsite({ ...website, facebookUrl: e.target.value })} /></label><label>Instagram URL<input type="url" value={website.instagramUrl ?? ''} onChange={e => setWebsite({ ...website, instagramUrl: e.target.value })} /></label>
          </>}

          <div className="full panel-heading"><div><h3>Search and link preview</h3><p>Controls how this page is described in Google and when its link is shared.</p></div></div>
          <label>SEO page title<input maxLength="60" value={translation.metaTitle ?? ''} onChange={e => change('metaTitle', e.target.value)} /><small>{(translation.metaTitle ?? '').length}/60 characters</small></label>
          <label className="full">Meta description<textarea rows="3" maxLength="160" value={translation.metaDescription ?? ''} onChange={e => change('metaDescription', e.target.value)} /><small>{(translation.metaDescription ?? '').length}/160 characters</small></label>

        </div>
        <button type="button" className="admin-primary-button" onClick={save}>Save changes</button>
      </section>
    </div>
    {previewImage && <div className="admin-modal-backdrop" onMouseDown={e => e.target === e.currentTarget && setPreviewImage(null)}><section className="admin-modal page-image-preview"><button type="button" className="page-image-preview-close" onClick={() => setPreviewImage(null)}>×</button><img src={previewImage} alt="" /></section></div>}
    <Toast message={toast} onClose={() => setToast('')} />
  </>
}
