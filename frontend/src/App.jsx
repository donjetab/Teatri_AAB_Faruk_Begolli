import { Navigate, Route, Routes, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { AppLayout } from './components/layout/AppLayout'
import { EmptyState } from './components/ui/EmptyState'
import { AboutPage } from './pages/AboutPage'
import { HomePage } from './pages/HomePage'
import { ShowsPage } from './pages/ShowsPage'
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
        <Route path="shows" element={<ShowsPage />} />
        <Route path="lajme" element={<ShellPlaceholder />} />
        <Route path="news" element={<ShellPlaceholder />} />
        <Route path="pitf" element={<ShellPlaceholder />} />
        <Route path="galeria" element={<ShellPlaceholder />} />
        <Route path="gallery" element={<ShellPlaceholder />} />
        <Route path="kontakti" element={<ShellPlaceholder />} />
        <Route path="contact" element={<ShellPlaceholder />} />
      </Route>
      <Route path="*" element={<Navigate to={getLocalizedPath('home', defaultLanguage)} replace />} />
    </Routes>
  )
}

export default App
