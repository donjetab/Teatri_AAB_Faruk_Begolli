import { Navigate, Route, Routes, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { AppLayout } from './components/layout/AppLayout'
import { EmptyState } from './components/ui/EmptyState'
import { AboutPage } from './pages/AboutPage'
import { HomePage } from './pages/HomePage'
import { ShowsPage } from './pages/ShowsPage'
import { ShowDetailPage } from './pages/ShowDetailPage'
<<<<<<< HEAD
=======
import { PitfPage } from './pages/PitfPage'
import { GalleryPage } from './pages/GalleryPage'
import { ContactPage } from './pages/ContactPage'
import { ReservePage } from './pages/ReservePage'
import { NewsPage } from './pages/NewsPage'
import { NewsDetailPage } from './pages/NewsDetailPage'
>>>>>>> 6f8afa3 (front pages almost done)
import { defaultLanguage, getLocalizedPath } from './routes/localizedRoutes'
import './App.css'

function ShellPlaceholder() {
  const { t } = useTranslation()
  const { language } = useParams()

  return (
    <section className="body-placeholder" aria-label={t('shell.title')}>
      <EmptyState message={language ? t('shell.title') : undefined} />
    </section>
  )
}

function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to={getLocalizedPath('home', defaultLanguage)} replace />} />
      <Route path="/:language" element={<AppLayout />}>
        <Route index element={<HomePage />} />
        <Route path="per-ne" element={<AboutPage />} />
        <Route path="about" element={<AboutPage />} />
        <Route path="shfaqjet" element={<ShowsPage />} />
        <Route path="shfaqjet/:slug" element={<ShowDetailPage />} />
        <Route path="shows" element={<ShowsPage />} />
        <Route path="shows/:slug" element={<ShowDetailPage />} />
<<<<<<< HEAD
        <Route path="lajme" element={<ShellPlaceholder />} />
        <Route path="news" element={<ShellPlaceholder />} />
        <Route path="pitf" element={<ShellPlaceholder />} />
        <Route path="galeria" element={<ShellPlaceholder />} />
        <Route path="gallery" element={<ShellPlaceholder />} />
        <Route path="kontakti" element={<ShellPlaceholder />} />
        <Route path="contact" element={<ShellPlaceholder />} />
=======
        <Route path="lajme" element={<NewsPage />} />
        <Route path="lajme/:slug" element={<NewsDetailPage />} />
        <Route path="news" element={<NewsPage />} />
        <Route path="news/:slug" element={<NewsDetailPage />} />
        <Route path="pitf" element={<PitfPage />} />
        <Route path="galeria" element={<GalleryPage />} />
        <Route path="gallery" element={<GalleryPage />} />
        <Route path="kontakti" element={<ContactPage />} />
        <Route path="contact" element={<ContactPage />} />
        <Route path="rezervo" element={<ReservePage />} />
        <Route path="reserve" element={<ReservePage />} />
>>>>>>> 6f8afa3 (front pages almost done)
      </Route>
      <Route path="*" element={<Navigate to={getLocalizedPath('home', defaultLanguage)} replace />} />
    </Routes>
  )
}

export default App
