import { createBrowserRouter, Navigate } from 'react-router-dom'
import PublicLayout from './layout/PublicLayout'
import AuthenticatedLayout from './layout/AuthenticatedLayout'
import { RoleGuard } from './layout/RoleGuard'
import NotFoundPage from '@/features/errors/NotFoundPage'
import AppErrorBoundary from '@/features/errors/AppErrorBoundary'

// Auth pages (Sprint 1)
import LoginPage from '@/features/auth/pages/LoginPage'
import ProfilePage from '@/features/auth/pages/ProfilePage'
import RegisterPage from '@/features/auth/pages/RegisterPage'
import ForgotPasswordPage from '@/features/auth/pages/ForgotPasswordPage'
import ResetPasswordPage from '@/features/auth/pages/ResetPasswordPage'
import VerifyEmailPage from '@/features/auth/pages/VerifyEmailPage'

// Pets pages (Sprint 2)
import DashboardPage from '@/features/pets/pages/DashboardPage'
import CreatePetPage from '@/features/pets/pages/CreatePetPage'
import PetDetailPage from '@/features/pets/pages/PetDetailPage'
import PublicPetProfilePage from '@/features/pets/pages/PublicPetProfilePage'

// LostPets + Notifications (Sprint 3)
import ReportLostPage from '@/features/lost-pets/pages/ReportLostPage'
import LostReportConfirmationPage from '@/features/lost-pets/pages/LostReportConfirmationPage'
import NotificationsPage from '@/features/notifications/pages/NotificationsPage'

// Sightings + Map (Sprint 4)
import ReportSightingPage from '@/features/sightings/pages/ReportSightingPage'
import VisualMatchPage from '@/features/sightings/pages/VisualMatchPage'
import PublicMapPage from '@/features/map/pages/PublicMapPage'
import RecoveryStatsPage from '@/features/lost-pets/pages/RecoveryStatsPage'

// Chat (Red de aliados verificados — Mejora #6)
import ChatPage from '@/features/chat/pages/ChatPage'

// Case Room — Mejora #4
import CaseRoomPage from '@/features/lost-pets/pages/CaseRoomPage'
import AllyPanelPage from '@/features/allies/pages/AllyPanelPage'

// Encontré una mascota — Mejora B
import ReportFoundPetPage from '@/features/sightings/pages/ReportFoundPetPage'
import FoundPetMatchResultPage from '@/features/sightings/pages/FoundPetMatchResultPage'

// Coordinación de buscadores en campo — Mejora H
import SearchCoordinationPage from '@/features/lost-pets/pages/SearchCoordinationPage'

// Red de veterinarias afiliadas — Mejora E
import ClinicRegisterPage from '@/features/clinics/pages/ClinicRegisterPage'
import ClinicPendingPage from '@/features/clinics/pages/ClinicPendingPage'
import ClinicDashboardPage from '@/features/clinics/pages/ClinicDashboardPage'

// Admin panel
import AdminPage from '@/features/admin/pages/AdminPage'

export const router = createBrowserRouter([
  {
    errorElement: <AppErrorBoundary />,
    children: [
  {
    element: <PublicLayout />,
    children: [
      { path: '/login', element: <LoginPage /> },
      { path: '/register', element: <RegisterPage /> },
      { path: '/forgot-password', element: <ForgotPasswordPage /> },
      { path: '/reset-password', element: <ResetPasswordPage /> },
      { path: '/verify-email', element: <VerifyEmailPage /> },
      { path: '/p/:id', element: <PublicPetProfilePage /> },
      { path: '/p/:id/report-sighting', element: <ReportSightingPage /> },
      { path: '/map', element: <PublicMapPage /> },
      { path: '/map/match', element: <VisualMatchPage /> },
      { path: '/encontre-mascota', element: <ReportFoundPetPage /> },
      { path: '/encontre-mascota/resultados', element: <FoundPetMatchResultPage /> },
      // Clinic public pages
      { path: '/clinica/registro', element: <ClinicRegisterPage /> },
      { path: '/clinica/pendiente', element: <ClinicPendingPage /> },
    ],
  },

  // ── Ally + Admin only ─────────────────────────────────────────────────────
  {
    element: <RoleGuard roles={['Ally', 'Admin']} />,
    children: [
      {
        element: <PublicLayout />,
        children: [
          { path: '/estadisticas', element: <RecoveryStatsPage /> },
        ],
      },
    ],
  },

  {
    element: <AuthenticatedLayout />,
    children: [
      { path: '/', element: <Navigate to="/dashboard" replace /> },
      { path: '/dashboard', element: <DashboardPage /> },
      { path: '/perfil', element: <ProfilePage /> },
      { path: '/pets/new', element: <CreatePetPage /> },
      { path: '/pets/:id', element: <PetDetailPage /> },
      { path: '/pets/:id/edit', element: <CreatePetPage /> },
      { path: '/pets/:id/report-lost', element: <ReportLostPage /> },
      { path: '/pets/:id/lost-confirmed', element: <LostReportConfirmationPage /> },
      { path: '/lost/:id/case', element: <CaseRoomPage /> },
      { path: '/lost/:lostEventId/busqueda', element: <SearchCoordinationPage /> },
      { path: '/notifications', element: <NotificationsPage /> },
      { path: '/chat/:lostPetEventId/:ownerUserId', element: <ChatPage /> },
      { path: '/chat/:lostPetEventId/:ownerUserId/:threadId', element: <ChatPage /> },

      // ── Ally + Admin only ────────────────────────────────────────────────
      {
        element: <RoleGuard roles={['Ally', 'Admin']} />,
        children: [
          { path: '/allies/panel', element: <AllyPanelPage /> },
        ],
      },

      // ── Clinic + Admin only ──────────────────────────────────────────────
      {
        element: <RoleGuard roles={['Clinic', 'Admin']} />,
        children: [
          { path: '/clinica/portal', element: <ClinicDashboardPage /> },
        ],
      },

      // ── Admin only ───────────────────────────────────────────────────────
      {
        element: <RoleGuard roles={['Admin']} />,
        children: [
          { path: '/admin', element: <AdminPage /> },
        ],
      },
    ],
  },

  // ── Catch-all 404 ─────────────────────────────────────────────────────
  { path: '*', element: <NotFoundPage /> },
    ],
  },
])
