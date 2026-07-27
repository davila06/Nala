import { lazy, Suspense } from "react";
import { createBrowserRouter, Navigate } from "react-router-dom";
import PublicLayout from "./layout/PublicLayout";
import AuthenticatedLayout from "./layout/AuthenticatedLayout";
import { RoleGuard } from "./layout/RoleGuard";
import NotFoundPage from "@/features/errors/NotFoundPage";
import AppErrorBoundary from "@/features/errors/AppErrorBoundary";
import { Skeleton } from "@/shared/ui/Spinner";

// ── Page skeleton shown during lazy-load ──────────────────────────────────────
const PageSkeleton = () => (
  <div className="mx-auto max-w-lg space-y-4 px-4 py-10 animate-pulse">
    <Skeleton className="h-8 w-48 rounded" />
    <Skeleton className="h-4 w-72 rounded" />
    <Skeleton className="h-48 rounded-2xl" />
    <Skeleton className="h-10 rounded-xl" />
    <Skeleton className="h-10 rounded-xl" />
  </div>
);

function S({ children }: { children: React.ReactNode }) {
  return <Suspense fallback={<PageSkeleton />}>{children}</Suspense>;
}

// Auth pages (Sprint 1)
const LoginPage = lazy(() => import("@/features/auth/pages/LoginPage"));
const ProfilePage = lazy(() => import("@/features/auth/pages/ProfilePage"));
const RegisterPage = lazy(() => import("@/features/auth/pages/RegisterPage"));
const ForgotPasswordPage = lazy(
  () => import("@/features/auth/pages/ForgotPasswordPage"),
);
const ResetPasswordPage = lazy(
  () => import("@/features/auth/pages/ResetPasswordPage"),
);
const VerifyEmailPage = lazy(
  () => import("@/features/auth/pages/VerifyEmailPage"),
);

// Pets pages (Sprint 2)
const DashboardPage = lazy(() => import("@/features/pets/pages/DashboardPage"));
const CreatePetPage = lazy(() => import("@/features/pets/pages/CreatePetPage"));
const PetDetailPage = lazy(() => import("@/features/pets/pages/PetDetailPage"));
const PublicPetProfilePage = lazy(
  () => import("@/features/pets/pages/PublicPetProfilePage"),
);

// LostPets + Notifications (Sprint 3)
const ReportLostPage = lazy(
  () => import("@/features/lost-pets/pages/ReportLostPage"),
);
const LostReportConfirmationPage = lazy(
  () => import("@/features/lost-pets/pages/LostReportConfirmationPage"),
);
const NotificationsPage = lazy(
  () => import("@/features/notifications/pages/NotificationsPage"),
);

// Sightings + Map (Sprint 4)
const ReportSightingPage = lazy(
  () => import("@/features/sightings/pages/ReportSightingPage"),
);
const VisualMatchPage = lazy(
  () => import("@/features/sightings/pages/VisualMatchPage"),
);
const PublicMapPage = lazy(() => import("@/features/map/pages/PublicMapPage"));
const RecoveryStatsPage = lazy(
  () => import("@/features/lost-pets/pages/RecoveryStatsPage"),
);

// Chat
const ChatPage = lazy(() => import("@/features/chat/pages/ChatPage"));

// Case Room
const CaseRoomPage = lazy(
  () => import("@/features/lost-pets/pages/CaseRoomPage"),
);
const AllyPanelPage = lazy(
  () => import("@/features/allies/pages/AllyPanelPage"),
);

// Encontré una mascota
const ReportFoundPetPage = lazy(
  () => import("@/features/sightings/pages/ReportFoundPetPage"),
);
const FoundPetMatchResultPage = lazy(
  () => import("@/features/sightings/pages/FoundPetMatchResultPage"),
);

// Coordinación de buscadores
const SearchCoordinationPage = lazy(
  () => import("@/features/lost-pets/pages/SearchCoordinationPage"),
);

// Red de veterinarias afiliadas
const ClinicRegisterPage = lazy(
  () => import("@/features/clinics/pages/ClinicRegisterPage"),
);
const ClinicPendingPage = lazy(
  () => import("@/features/clinics/pages/ClinicPendingPage"),
);
const ClinicDashboardPage = lazy(
  () => import("@/features/clinics/pages/ClinicDashboardPage"),
);

// Admin panel
const AdminPage = lazy(() => import("@/features/admin/pages/AdminPage"));

export const router = createBrowserRouter([
  {
    errorElement: <AppErrorBoundary />,
    children: [
      {
        element: <PublicLayout />,
        children: [
          {
            path: "/login",
            element: (
              <S>
                <LoginPage />
              </S>
            ),
          },
          {
            path: "/register",
            element: (
              <S>
                <RegisterPage />
              </S>
            ),
          },
          {
            path: "/forgot-password",
            element: (
              <S>
                <ForgotPasswordPage />
              </S>
            ),
          },
          {
            path: "/reset-password",
            element: (
              <S>
                <ResetPasswordPage />
              </S>
            ),
          },
          {
            path: "/verify-email",
            element: (
              <S>
                <VerifyEmailPage />
              </S>
            ),
          },
          {
            path: "/p/:id",
            element: (
              <S>
                <PublicPetProfilePage />
              </S>
            ),
          },
          {
            path: "/p/:id/report-sighting",
            element: (
              <S>
                <ReportSightingPage />
              </S>
            ),
          },
          {
            path: "/map",
            element: (
              <S>
                <PublicMapPage />
              </S>
            ),
          },
          {
            path: "/map/match",
            element: (
              <S>
                <VisualMatchPage />
              </S>
            ),
          },
          {
            path: "/encontre-mascota",
            element: (
              <S>
                <ReportFoundPetPage />
              </S>
            ),
          },
          {
            path: "/encontre-mascota/resultados",
            element: (
              <S>
                <FoundPetMatchResultPage />
              </S>
            ),
          },
          {
            path: "/clinica/registro",
            element: (
              <S>
                <ClinicRegisterPage />
              </S>
            ),
          },
          {
            path: "/clinica/pendiente",
            element: (
              <S>
                <ClinicPendingPage />
              </S>
            ),
          },
        ],
      },

      // ── Ally + Admin only ─────────────────────────────────────────────────────
      {
        element: <RoleGuard roles={["Ally", "Admin"]} />,
        children: [
          {
            element: <PublicLayout />,
            children: [
              {
                path: "/estadisticas",
                element: (
                  <S>
                    <RecoveryStatsPage />
                  </S>
                ),
              },
            ],
          },
        ],
      },

      {
        element: <AuthenticatedLayout />,
        children: [
          { path: "/", element: <Navigate to="/dashboard" replace /> },
          {
            path: "/dashboard",
            element: (
              <S>
                <DashboardPage />
              </S>
            ),
          },
          {
            path: "/perfil",
            element: (
              <S>
                <ProfilePage />
              </S>
            ),
          },
          {
            path: "/pets/new",
            element: (
              <S>
                <CreatePetPage />
              </S>
            ),
          },
          {
            path: "/pets/:id",
            element: (
              <S>
                <PetDetailPage />
              </S>
            ),
          },
          {
            path: "/pets/:id/edit",
            element: (
              <S>
                <CreatePetPage />
              </S>
            ),
          },
          {
            path: "/pets/:id/report-lost",
            element: (
              <S>
                <ReportLostPage />
              </S>
            ),
          },
          {
            path: "/pets/:id/lost-confirmed",
            element: (
              <S>
                <LostReportConfirmationPage />
              </S>
            ),
          },
          {
            path: "/lost/:id/case",
            element: (
              <S>
                <CaseRoomPage />
              </S>
            ),
          },
          {
            path: "/lost/:lostEventId/busqueda",
            element: (
              <S>
                <SearchCoordinationPage />
              </S>
            ),
          },
          {
            path: "/notifications",
            element: (
              <S>
                <NotificationsPage />
              </S>
            ),
          },
          {
            path: "/chat/:lostPetEventId/:ownerUserId",
            element: (
              <S>
                <ChatPage />
              </S>
            ),
          },
          {
            path: "/chat/:lostPetEventId/:ownerUserId/:threadId",
            element: (
              <S>
                <ChatPage />
              </S>
            ),
          },

          // ── Ally + Admin only ────────────────────────────────────────────────
          {
            element: <RoleGuard roles={["Ally", "Admin"]} />,
            children: [
              {
                path: "/allies/panel",
                element: (
                  <S>
                    <AllyPanelPage />
                  </S>
                ),
              },
            ],
          },

          // ── Clinic + Admin only ──────────────────────────────────────────────
          {
            element: <RoleGuard roles={["Clinic", "Admin"]} />,
            children: [
              {
                path: "/clinica/portal",
                element: (
                  <S>
                    <ClinicDashboardPage />
                  </S>
                ),
              },
            ],
          },

          // ── Admin only ───────────────────────────────────────────────────────
          {
            element: <RoleGuard roles={["Admin"]} />,
            children: [
              {
                path: "/admin",
                element: (
                  <S>
                    <AdminPage />
                  </S>
                ),
              },
            ],
          },
        ],
      },

      // ── Catch-all 404 ─────────────────────────────────────────────────────
      { path: "*", element: <NotFoundPage /> },
    ],
  },
]);
