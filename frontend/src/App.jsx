import { Navigate, Route, Routes, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { AppLayout } from './components/layout/AppLayout'
import { EmptyState } from './components/ui/EmptyState'
import { HomePage } from './pages/HomePage'
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
        <Route path="per-ne" element={<ShellPlaceholder />} />
        <Route path="about" element={<ShellPlaceholder />} />
        <Route path="shfaqjet" element={<ShellPlaceholder />} />
        <Route path="shows" element={<ShellPlaceholder />} />
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
