import { useLocation } from 'react-router-dom'
import { PageHeader } from '../components/AdminUi'
const names = { shows: 'Shows / Plays', performances: 'Performances', news: 'News', pitf: 'PITF', gallery: 'Gallery', pages: 'Pages', messages: 'Contact Messages', subscribers: 'Subscribers', navigation: 'Navigation & Footer', seo: 'SEO & Links', media: 'Media Library', users: 'Users & Roles', activity: 'Activity Log', backups: 'Backups & System', settings: 'Settings' }
export function SectionPlaceholderPage() {
  const key = useLocation().pathname.split('/').at(-1); const title = names[key] ?? 'Admin'
  return <><PageHeader eyebrow="Admin Panel" title={title} description="This module is ready to be connected to its database-driven management workflow." /><section className="admin-panel reservation-placeholder"><div>◇</div><h2>{title}</h2><p>The secure route and responsive page foundation are in place. CRUD tables and editors for this module are part of the next implementation slice.</p></section></>
}
