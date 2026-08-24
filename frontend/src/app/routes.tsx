import { lazy, Suspense } from "react";
import { createBrowserRouter, Navigate } from "react-router-dom";
import PublicLayout from "./layout/PublicLayout";
import AuthenticatedLayout from "./layout/AuthenticatedLayout";
import { RoleGuard } from "./layout/RoleGuard";
import NotFoundPage from "@/features/errors/NotFoundPage";
import AppErrorBoundary from "@/features/errors/AppErrorBoundary";
import { FeatureErrorBoundary } from "@/shared/ui/FeatureErrorBoundary";
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

function S({ children, name }: { children: React.ReactNode; name?: string }) {
  return (
    <FeatureErrorBoundary featureName={name}>
      <Suspense fallback={<PageSkeleton />}>{children}</Suspense>
    </FeatureErrorBoundary>
  );
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
const ChatThreadPage = lazy(
  () => import("@/features/chat/pages/ChatThreadPage"),
);

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
const QuickFoundPetPage = lazy(
  () => import("@/features/sightings/pages/QuickFoundPetPage"),
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

// Tiendas de mascotas
const StoreRegistrationPage = lazy(
  () => import("@/features/stores/pages/StoreRegistrationPage"),
);
const StorePendingPage = lazy(
  () => import("@/features/stores/pages/StorePendingPage"),
);
const StoreDashboardPage = lazy(
  () => import("@/features/stores/pages/StoreDashboardPage"),
);
const StoreProductsPage = lazy(
  () => import("@/features/stores/pages/StoreProductsPage"),
);
const StoreOrdersPage = lazy(
  () => import("@/features/stores/pages/StoreOrdersPage"),
);
const MyStoreOrdersPage = lazy(
  () => import("@/features/stores/pages/MyStoreOrdersPage"),
);
const StoreDirectoryPage = lazy(
  () => import("@/features/stores/pages/StoreDirectoryPage"),
);

// Admin panel
const AdminPage = lazy(() => import("@/features/admin/pages/AdminPage"));

// Módulo de adopciones
const AdoptionDirectoryPage = lazy(
  () => import("@/features/adoptions/pages/AdoptionDirectoryPage"),
);
const AdoptionDetailPage = lazy(
  () => import("@/features/adoptions/pages/AdoptionDetailPage"),
);
const AdoptionFairsPage = lazy(
  () => import("@/features/adoptions/pages/AdoptionFairsPage"),
);
const MyAdoptionApplicationsPage = lazy(
  () => import("@/features/adoptions/pages/MyAdoptionApplicationsPage"),
);
const ShelterDashboardPage = lazy(
  () => import("@/features/adoptions/pages/ShelterDashboardPage"),
);
const ShelterPublishPage = lazy(
  () => import("@/features/adoptions/pages/ShelterPublishPage"),
);
const ShelterApplicationsPage = lazy(
  () => import("@/features/adoptions/pages/ShelterApplicationsPage"),
);
const MunicipalityPortalPage = lazy(
  () => import("@/features/admin/pages/MunicipalityPortalPage"),
);
const MunicipalDashboardPage = lazy(
  () => import("@/features/admin/pages/MunicipalDashboardPage"),
);
const CertificateVerificationPage = lazy(
  () => import("@/features/clinics/pages/CertificateVerificationPage"),
);
const PassportVerificationPage = lazy(
  () => import("@/features/clinics/pages/PassportVerificationPage"),
);

// Family invitation (public — no auth wrapper needed; page handles redirect)
const AcceptFamilyInvitationPage = lazy(
  () => import("@/features/family/pages/AcceptFamilyInvitationPage"),
);

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
            path: "/encontre",
            element: (
              <S>
                <QuickFoundPetPage />
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
          {
            path: "/tienda/registro",
            element: (
              <S>
                <StoreRegistrationPage />
              </S>
            ),
          },
          {
            path: "/tienda/pendiente",
            element: (
              <S>
                <StorePendingPage />
              </S>
            ),
          },
          {
            path: "/tiendas",
            element: (
              <S>
                <StoreDirectoryPage />
              </S>
            ),
          },
          // ── Adopciones (público) ──────────────────────────────────────────
          {
            path: "/adopciones",
            element: (
              <S>
                <AdoptionDirectoryPage />
              </S>
            ),
          },
          {
            path: "/adopciones/ferias",
            element: (
              <S>
                <AdoptionFairsPage />
              </S>
            ),
          },
          {
            path: "/adopciones/:id",
            element: (
              <S>
                <AdoptionDetailPage />
              </S>
            ),
          },
          {
            path: "/familia/invitacion/:token",
            element: (
              <S>
                <AcceptFamilyInvitationPage />
              </S>
            ),
          },
        ],
      },

      // ── Estadísticas: solo Admin ──────────────────────────────────────────────
      {
        element: <RoleGuard roles={["Admin"]} />,
        children: [
          {
            element: <AuthenticatedLayout />,
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
          {
            // Direct thread access — used from notification deep-links
            path: "/chat/t/:threadId",
            element: (
              <S>
                <ChatThreadPage />
              </S>
            ),
          },
          {
            path: "/mis-pedidos",
            element: (
              <S>
                <MyStoreOrdersPage />
              </S>
            ),
          },
          // ── Adopciones (autenticado — Owner) ────────────────────────────────
          {
            path: "/mis-adopciones",
            element: (
              <S>
                <MyAdoptionApplicationsPage />
              </S>
            ),
          },

          // ── Store + Admin only ───────────────────────────────────────────────
          {
            element: <RoleGuard roles={["Store", "Admin"]} />,
            children: [
              {
                path: "/tienda/portal",
                element: (
                  <S>
                    <StoreDashboardPage />
                  </S>
                ),
              },
              {
                path: "/tienda/portal/productos",
                element: (
                  <S>
                    <StoreProductsPage />
                  </S>
                ),
              },
              {
                path: "/tienda/portal/ordenes",
                element: (
                  <S>
                    <StoreOrdersPage />
                  </S>
                ),
              },
            ],
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
              // ── Shelter adoption management ────────────────────────────────
              {
                path: "/shelter/dashboard",
                element: (
                  <S>
                    <ShelterDashboardPage />
                  </S>
                ),
              },
              {
                path: "/shelter/publicar",
                element: (
                  <S>
                    <ShelterPublishPage />
                  </S>
                ),
              },
              {
                path: "/shelter/animales/:id/aplicaciones",
                element: (
                  <S>
                    <ShelterApplicationsPage />
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
          // ── Public municipal portal (marketing) ──────────────────────────────
          {
            path: "/municipalidad",
            element: (
              <S>
                <MunicipalityPortalPage />
              </S>
            ),
          },
          // ── Municipal operational dashboard (authenticated) ──────────────────
          {
            element: <RoleGuard roles={["Municipality", "Admin"]} />,
            children: [
              {
                path: "/municipalidad/portal",
                element: (
                  <S>
                    <MunicipalDashboardPage />
                  </S>
                ),
              },
            ],
          },
        ],
      },

      // ── Catch-all 404 ─────────────────────────────────────────────────────
      // ── Public certificate verification (no auth, no layout shell) ─────
      {
        path: "/verificar/:code",
        element: (
          <S>
            <CertificateVerificationPage />
          </S>
        ),
      },
      {
        path: "/verificar/pasaporte/:code",
        element: (
          <S>
            <PassportVerificationPage />
          </S>
        ),
      },
      { path: "*", element: <NotFoundPage /> },
    ],
  },
]);
