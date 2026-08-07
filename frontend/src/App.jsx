import { Navigate, Route, Routes, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { AppLayout } from './components/layout/AppLayout'
import { EmptyState } from './components/ui/EmptyState'
import { AboutPage } from './pages/AboutPage'
import { HomePage } from './pages/HomePage'
import { ShowsPage } from './pages/ShowsPage'
import { ShowDetailPage } from './pages/ShowDetailPage'
import { PitfPage } from './pages/PitfPage'
import { GalleryPage } from './pages/GalleryPage'
import { ContactPage } from './pages/ContactPage'
import { ReservePage } from './pages/ReservePage'
import { InternalReservationPage } from './pages/InternalReservationPage'
import { NewsPage } from './pages/NewsPage'
import { NewsDetailPage } from './pages/NewsDetailPage'
import { defaultLanguage, getLocalizedPath } from './routes/localizedRoutes'
import './App.css'
import { AdminAuthProvider, ProtectedAdminRoute, SuperAdminRoute } from './admin/AuthContext'
import { AdminLayout } from './admin/components/AdminLayout'
import { LoginPage } from './admin/pages/LoginPage'
import { DashboardPage } from './admin/pages/DashboardPage'
import { WebsiteInformationPage } from './admin/pages/WebsiteInformationPage'
import { HomepageManagementPage } from './admin/pages/HomepageManagementPage'
import { TranslationsPage } from './admin/pages/TranslationsPage'
import { ReservationsPage } from './admin/pages/ReservationsPage'
import { CustomersPage } from './admin/pages/CustomersPage'
import { SeatingTemplatesPage } from './admin/pages/SeatingTemplatesPage'
import { SectionPlaceholderPage } from './admin/pages/SectionPlaceholderPage'
import { ChangePasswordPage } from './admin/pages/ChangePasswordPage'
import { AdminShowsPage } from './admin/pages/ShowsPage'
import { ShowEditorPage } from './admin/pages/ShowEditorPage'
import { PerformancesPage } from './admin/pages/PerformancesPage'
import { MessagesPage } from './admin/pages/MessagesPage'
import { SubscribersPage } from './admin/pages/SubscribersPage'
import { ExistingContentPage } from './admin/pages/ExistingContentPage'
import { NewsEditorPage } from './admin/pages/NewsEditorPage'
import { PitfManagementPage } from './admin/pages/PitfManagementPage'
import { GalleryManagementPage } from './admin/pages/GalleryManagementPage'
import { StaticPagesPage } from './admin/pages/StaticPagesPage'
import { MediaLibraryPage } from './admin/pages/MediaLibraryPage'
import { NavigationFooterPage } from './admin/pages/NavigationFooterPage'
import { UsersRolesPage } from './admin/pages/UsersRolesPage'
import { ActivityLogPage } from './admin/pages/ActivityLogPage'
import { BackupsSystemPage } from './admin/pages/BackupsSystemPage'
import { SettingsPage } from './admin/pages/SettingsPage'
import { SeoIssuesPage } from './admin/pages/SeoIssuesPage'
import './admin/admin.css'

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
    <AdminAuthProvider><Routes>
      <Route path="/admin/login" element={<LoginPage />} />
      <Route element={<ProtectedAdminRoute />}>
        <Route path="/admin" element={<AdminLayout />}>
          <Route index element={<DashboardPage />} />
          <Route path="website-information" element={<WebsiteInformationPage />} />
          <Route path="homepage" element={<HomepageManagementPage />} />
          <Route path="translations" element={<TranslationsPage />} />
          <Route path="reservations" element={<ReservationsPage />} />
          <Route path="customers" element={<CustomersPage />} />
          <Route path="seating-templates" element={<SeatingTemplatesPage />} />
          <Route path="performances/:performanceId/seating" element={<SeatingTemplatesPage />} />
          <Route path="account" element={<ChangePasswordPage />} />
          <Route path="shows" element={<AdminShowsPage />} />
          <Route path="shows/:id" element={<ShowEditorPage />} />
          <Route path="performances" element={<PerformancesPage />} />
          <Route path="news" element={<ExistingContentPage type="news" />} />
          <Route path="news/new" element={<NewsEditorPage />} />
          <Route path="news/:id" element={<NewsEditorPage />} />
          <Route path="pitf" element={<PitfManagementPage />} />
          <Route path="gallery" element={<GalleryManagementPage />} />
          <Route path="pages" element={<StaticPagesPage />} />
          <Route path="messages" element={<MessagesPage />} />
          <Route path="subscribers" element={<SubscribersPage />} />
          <Route path="seo" element={<SeoIssuesPage />} />
          <Route path="media" element={<MediaLibraryPage />} />
          <Route element={<SuperAdminRoute />}>
            <Route path="navigation" element={<NavigationFooterPage />} />
            <Route path="users" element={<UsersRolesPage />} />
            <Route path="activity" element={<ActivityLogPage />} />
            <Route path="backups" element={<BackupsSystemPage />} />
            <Route path="settings" element={<SettingsPage />} />
          </Route>
        </Route>
      </Route>
      <Route path="/" element={<Navigate to={getLocalizedPath('home', defaultLanguage)} replace />} />
      <Route path="/:language" element={<AppLayout />}>
        <Route index element={<HomePage />} />
        <Route path="per-ne" element={<AboutPage />} />
        <Route path="about" element={<AboutPage />} />
        <Route path="shfaqjet" element={<ShowsPage />} />
        <Route path="shfaqjet/:slug" element={<ShowDetailPage />} />
        <Route path="shows" element={<ShowsPage />} />
        <Route path="shows/:slug" element={<ShowDetailPage />} />
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
        <Route path="rezervo/:performanceId" element={<InternalReservationPage />} />
        <Route path="reserve" element={<ReservePage />} />
        <Route path="reserve/:performanceId" element={<InternalReservationPage />} />
      </Route>
      <Route path="*" element={<Navigate to={getLocalizedPath('home', defaultLanguage)} replace />} />
    </Routes></AdminAuthProvider>
  )
}

export default App
