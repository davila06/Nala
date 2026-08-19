import { useState, useRef, useEffect } from "react";
import {
  Outlet,
  Navigate,
  NavLink,
  Link,
  useNavigate,
  useLocation,
} from "react-router-dom";
import { AnimatePresence, motion } from "framer-motion";
import { useScrollToTop } from "@/shared/hooks/useScrollToTop";
import { NotificationBell } from "@/features/notifications/components/NotificationBell";
import { OfflineQueueBanner } from "@/features/lost-pets/components/OfflineQueueBanner";
import { useAuthStore } from "@/features/auth/store/authStore";
import { useLogout } from "@/features/auth/hooks/useAuth";
import { BottomNav } from "./BottomNav";

// ── Page context map ─────────────────────────────────────────────────────────
// Maps route prefixes to { label, description } for the sub-header breadcrumb.
const PAGE_CONTEXT: Record<string, { label: string; icon: string }> = {
  "/dashboard": { label: "Mis mascotas", icon: "🐾" },
  "/pets/new": { label: "Registrar mascota", icon: "➕" },
  "/pets": { label: "Detalle de mascota", icon: "🐾" },
  "/perfil": { label: "Mi perfil", icon: "👤" },
  "/notifications": { label: "Notificaciones", icon: "🔔" },
  "/map": { label: "Mapa público", icon: "🗺️" },
  "/estadisticas": { label: "Estadísticas", icon: "📊" },
  "/lost": { label: "Caso de búsqueda", icon: "🚨" },
  "/chat": { label: "Chat seguro", icon: "💬" },
  "/allies/panel": { label: "Panel de Aliado", icon: "🤝" },
  "/clinica/portal": { label: "Portal Clínica", icon: "🏥" },
  "/tienda/portal":  { label: "Portal Tienda",  icon: "🛍️" },
  "/admin":          { label: "Administración", icon: "⚙️" },
};

function resolvePageContext(pathname: string) {
  // Longest-prefix match
  const match = Object.keys(PAGE_CONTEXT)
    .filter(
      (prefix) =>
        pathname === prefix ||
        pathname.startsWith(prefix + "/") ||
        pathname.startsWith(prefix),
    )
    .sort((a, b) => b.length - a.length)[0];
  return match ? PAGE_CONTEXT[match] : null;
}

// Routes that are top-level nav destinations — no back button needed
const TOP_LEVEL_ROUTES = new Set([
  "/dashboard",
  "/map",
  "/notifications",
  "/perfil",
  "/allies/panel",
  "/clinica/portal",
  "/municipalidad/portal",
  "/admin",
  "/estadisticas",
]);
const NAV_MAIN = [
  {
    to: "/dashboard",
    label: "Inicio",
    icon: (active: boolean) => (
      <svg
        viewBox="0 0 20 20"
        fill={active ? "currentColor" : "none"}
        stroke="currentColor"
        strokeWidth="1.6"
        className="h-4 w-4"
        aria-hidden="true"
      >
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          d="M3 9.5 10 4l7 5.5V17a.5.5 0 0 1-.5.5H13V13H7v4.5H3.5A.5.5 0 0 1 3 17V9.5Z"
        />
      </svg>
    ),
  },
  {
    to: "/map",
    label: "Mapa",
    icon: (active: boolean) => (
      <svg
        viewBox="0 0 20 20"
        fill={active ? "currentColor" : "none"}
        stroke="currentColor"
        strokeWidth="1.6"
        className="h-4 w-4"
        aria-hidden="true"
      >
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          d="M10 2a5 5 0 0 1 5 5c0 3.5-5 11-5 11S5 10.5 5 7a5 5 0 0 1 5-5Z"
        />
        <circle cx="10" cy="7" r="1.5" strokeLinecap="round" />
      </svg>
    ),
  },
];

const NAV_EXTRA_ALLY = {
  to: "/allies/panel",
  label: "Panel Aliado",
  icon: null,
};
const NAV_EXTRA_CLINIC = {
  to: "/clinica/portal",
  label: "Panel Clínica",
  icon: null,
};
const NAV_EXTRA_STORE = {
  to: "/tienda/portal",
  label: "Portal Tienda",
  icon: null,
};
const NAV_EXTRA_MUNICIPALITY = {
  to: "/municipalidad/portal",
  label: "Portal Municipal",
  icon: null,
};
const NAV_EXTRA_ADMIN = { to: "/admin", label: "Administración", icon: null };
const NAV_EXTRA_ADMIN_STATS = {
  to: "/estadisticas",
  label: "Estadísticas",
  icon: null,
};

const ROLE_BADGE: Record<string, { label: string; cls: string }> = {
  Owner: { label: "Propietario", cls: "bg-sand-100 text-sand-600" },
  Ally: { label: "Aliado", cls: "bg-brand-50 text-brand-700" },
  Clinic: { label: "Clínica", cls: "bg-blue-50 text-blue-700" },
  Municipality: { label: "Municipalidad", cls: "bg-trust-50 text-trust-700" },
  Admin: { label: "Admin", cls: "bg-red-50 text-red-600" },
};

const AVATAR_COLORS = [
  "bg-brand-500",
  "bg-blue-500",
  "bg-rescue-500",
  "bg-warn-500",
  "bg-sand-600",
  "bg-purple-500",
];

function getInitials(name = ""): string {
  const parts = name.trim().split(/\s+/);
  if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
  return (parts[0]?.[0] ?? "?").toUpperCase();
}

function avatarColor(name = ""): string {
  return AVATAR_COLORS[(name.charCodeAt(0) ?? 0) % AVATAR_COLORS.length];
}

const activeCls = "text-brand-600 bg-brand-50 font-semibold";
const inactiveCls = "text-sand-600 hover:bg-sand-50 hover:text-sand-900";

const navLinkCls = ({ isActive }: { isActive: boolean }) =>
  [
    "flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm transition-base",
    isActive ? activeCls : inactiveCls,
  ].join(" ");

const navLinkPlainCls = ({ isActive }: { isActive: boolean }) =>
  [
    "rounded-lg px-3 py-1.5 text-sm transition-base",
    isActive ? activeCls : inactiveCls,
  ].join(" ");

export default function AuthenticatedLayout() {
  const { isAuthenticated, isInitializing, user } = useAuthStore();
  const [menuOpen, setMenuOpen] = useState(false);
  const [dropdownOpen, setDropdownOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const { mutate: logout } = useLogout();
  const navigate = useNavigate();
  const location = useLocation();
  useScrollToTop();

  // Close desktop dropdown on outside click
  useEffect(() => {
    if (!dropdownOpen) return;
    function handleOutside(e: MouseEvent) {
      if (
        dropdownRef.current &&
        !dropdownRef.current.contains(e.target as Node)
      ) {
        setDropdownOpen(false);
      }
    }
    document.addEventListener("mousedown", handleOutside);
    return () => document.removeEventListener("mousedown", handleOutside);
  }, [dropdownOpen]);

  function handleLogout() {
    setDropdownOpen(false);
    setMenuOpen(false);
    logout(undefined, {
      onSuccess: () => navigate("/login", { replace: true }),
    });
  }

  if (isInitializing) {
    // Wait for the silent refresh to resolve before deciding auth state.
    // Prevents redirect to /login during page refresh when cookie is valid.
    return (
      <div className="flex min-h-dvh items-center justify-center bg-bg">
        <span className="h-8 w-8 animate-spin rounded-full border-4 border-sand-200 border-t-brand-500" />
      </div>
    );
  }

  if (!isAuthenticated) {
    const returnTo = encodeURIComponent(location.pathname + location.search);
    return <Navigate to={`/login?return=${returnTo}`} replace />;
  }

  const extraNav =
    user?.role === "Ally"
      ? NAV_EXTRA_ALLY
      : user?.role === "Clinic"
        ? NAV_EXTRA_CLINIC
        : user?.role === "Store"
          ? NAV_EXTRA_STORE
          : user?.role === "Municipality"
            ? NAV_EXTRA_MUNICIPALITY
            : user?.role === "Admin"
              ? NAV_EXTRA_ADMIN
              : null;

  const adminStatsNav = user?.role === "Admin" ? NAV_EXTRA_ADMIN_STATS : null;

  const pageCtx = resolvePageContext(location.pathname);
  const isSubPage =
    pageCtx !== null &&
    !TOP_LEVEL_ROUTES.has(location.pathname) &&
    !Array.from(TOP_LEVEL_ROUTES).some((r) => location.pathname === r);

  return (
    <div className="min-h-dvh bg-sand-100">
      {/* ── Top bar ─────────────────────────────────────────────────────── */}
      <header className="sticky top-0 z-40 border-b border-sand-200 bg-surface/95 backdrop-blur-sm">
        <div className="mx-auto flex h-14 max-w-6xl items-center gap-4 px-4">
          {/* Logo */}
          <Link
            to="/dashboard"
            className="flex items-center gap-2 font-display text-lg font-semibold text-brand-600 tracking-tight shrink-0"
          >
            <span
              aria-hidden="true"
              className="flex h-8 w-8 items-center justify-center rounded-xl bg-brand-500 text-white text-base"
            >
              🐾
            </span>
            PawTrack
          </Link>

          {/* Desktop nav */}
          <nav
            aria-label="Navegación principal"
            className="hidden items-center gap-1 md:flex"
          >
            {NAV_MAIN.map((item) => (
              <NavLink key={item.to} to={item.to} className={navLinkCls}>
                {({ isActive }) => (
                  <>
                    {item.icon?.(isActive)}
                    {item.label}
                  </>
                )}
              </NavLink>
            ))}
            {extraNav && (
              <NavLink to={extraNav.to} className={navLinkPlainCls}>
                {extraNav.label}
              </NavLink>
            )}
            {adminStatsNav && (
              <NavLink to={adminStatsNav.to} className={navLinkPlainCls}>
                {adminStatsNav.label}
              </NavLink>
            )}
          </nav>

          {/* Spacer */}
          <div className="flex-1" />

          {/* ── Report lost CTA — shown for Owners only ──────────────────── */}
          {user?.role === "Owner" && (
            <Link
              to="/dashboard"
              className="hidden md:flex items-center gap-1.5 rounded-xl bg-danger-500 px-3.5 py-1.5 text-sm font-semibold text-white shadow-sm hover:bg-danger-600 transition-base"
            >
              <svg
                viewBox="0 0 20 20"
                fill="currentColor"
                className="h-3.5 w-3.5"
                aria-hidden="true"
              >
                <path
                  fillRule="evenodd"
                  d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495ZM10 5a.75.75 0 0 1 .75.75v3.5a.75.75 0 0 1-1.5 0v-3.5A.75.75 0 0 1 10 5Zm0 9a1 1 0 1 0 0-2 1 1 0 0 0 0 2Z"
                  clipRule="evenodd"
                />
              </svg>
              Mascota perdida
            </Link>
          )}

          {/* Actions */}
          <div className="flex items-center gap-2">
            <NotificationBell />

            {/* ── User avatar: desktop dropdown + mobile drawer toggle ─── */}
            <div className="relative" ref={dropdownRef}>
              <button
                type="button"
                onClick={() => {
                  setDropdownOpen((v) => !v);
                  setMenuOpen(false);
                }}
                aria-label="Menú de usuario"
                aria-expanded={dropdownOpen}
                aria-haspopup="menu"
                className={`hidden md:flex h-8 w-8 items-center justify-center rounded-full text-xs font-bold text-white transition-base focus-visible:ring-2 focus-visible:ring-brand-400 ${avatarColor(user?.name)}`}
              >
                {getInitials(user?.name)}
              </button>

              {/* Desktop dropdown */}
              <AnimatePresence>
                {dropdownOpen && (
                  <motion.div
                    role="menu"
                    initial={{ opacity: 0, scale: 0.95, y: -6 }}
                    animate={{ opacity: 1, scale: 1, y: 0 }}
                    exit={{ opacity: 0, scale: 0.95, y: -6 }}
                    transition={{ duration: 0.15, ease: [0.4, 0, 0.2, 1] }}
                    className="absolute right-0 top-10 z-50 hidden md:block w-60 rounded-2xl border border-sand-200 bg-surface shadow-xl overflow-hidden origin-top-right"
                  >
                    {/* User info header */}
                    <div className="flex items-center gap-3 px-4 py-3.5 border-b border-sand-100">
                      <span
                        className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-full text-sm font-bold text-white ${avatarColor(user?.name)}`}
                      >
                        {getInitials(user?.name)}
                      </span>
                      <div className="min-w-0">
                        <p className="text-sm font-semibold text-sand-900 truncate">
                          {user?.name}
                        </p>
                        <p className="text-xs text-sand-400 truncate">
                          {user?.email}
                        </p>
                        {user?.role && ROLE_BADGE[user.role] && (
                          <span
                            className={`mt-0.5 inline-block rounded-full px-2 py-0.5 text-[10px] font-semibold ${ROLE_BADGE[user.role].cls}`}
                          >
                            {ROLE_BADGE[user.role].label}
                          </span>
                        )}
                      </div>
                    </div>

                    {/* Account links */}
                    <div className="py-1">
                      <Link
                        to="/perfil"
                        role="menuitem"
                        onClick={() => setDropdownOpen(false)}
                        className="flex items-center gap-2.5 px-4 py-2.5 text-sm text-sand-700 hover:bg-sand-50 hover:text-sand-900 transition-base"
                      >
                        <svg
                          className="h-4 w-4 shrink-0 text-sand-400"
                          viewBox="0 0 20 20"
                          fill="currentColor"
                          aria-hidden="true"
                        >
                          <path d="M10 10a4 4 0 1 0 0-8 4 4 0 0 0 0 8zm-7 8a7 7 0 0 1 14 0H3z" />
                        </svg>
                        Mi perfil
                      </Link>
                      <Link
                        to="/dashboard"
                        role="menuitem"
                        onClick={() => setDropdownOpen(false)}
                        className="flex items-center gap-2.5 px-4 py-2.5 text-sm text-sand-700 hover:bg-sand-50 hover:text-sand-900 transition-base"
                      >
                        <svg
                          className="h-4 w-4 shrink-0 text-sand-400"
                          viewBox="0 0 20 20"
                          fill="currentColor"
                          aria-hidden="true"
                        >
                          <path d="M4.5 2a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3zM9 3.5a1.5 1.5 0 1 1 3 0 1.5 1.5 0 0 1-3 0zm6 0a1.5 1.5 0 1 1 3 0 1.5 1.5 0 0 1-3 0zM2 8.5a1.5 1.5 0 1 1 3 0 1.5 1.5 0 0 1-3 0zM10 7a5 5 0 0 0-4.546 2.916A2.5 2.5 0 0 0 7 14.5h6a2.5 2.5 0 0 0 1.546-4.584A5 5 0 0 0 10 7z" />
                        </svg>
                        Mis mascotas
                      </Link>
                    </div>

                    {/* Sobre la app */}
                    <div className="border-t border-sand-100 py-1">
                      <p className="px-4 pt-2 pb-1 text-[10px] font-semibold uppercase tracking-wider text-sand-400">
                        Sobre la app
                      </p>
                      <a
                        href="/legal/terminos-de-uso.html"
                        target="_blank"
                        rel="noopener noreferrer"
                        role="menuitem"
                        onClick={() => setDropdownOpen(false)}
                        className="flex items-center gap-2.5 px-4 py-2.5 text-sm text-sand-700 hover:bg-sand-50 hover:text-sand-900 transition-base"
                      >
                        <svg
                          className="h-4 w-4 shrink-0 text-sand-400"
                          viewBox="0 0 20 20"
                          fill="currentColor"
                          aria-hidden="true"
                        >
                          <path
                            fillRule="evenodd"
                            d="M4 4a2 2 0 0 1 2-2h4.586A2 2 0 0 1 12 2.586L15.414 6A2 2 0 0 1 16 7.414V16a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V4zm2 6a1 1 0 0 1 1-1h6a1 1 0 1 1 0 2H7a1 1 0 0 1-1-1zm1 3a1 1 0 1 0 0 2h6a1 1 0 1 0 0-2H7z"
                            clipRule="evenodd"
                          />
                        </svg>
                        Términos de uso
                      </a>
                      <a
                        href="/legal/politica-de-privacidad.html"
                        target="_blank"
                        rel="noopener noreferrer"
                        role="menuitem"
                        onClick={() => setDropdownOpen(false)}
                        className="flex items-center gap-2.5 px-4 py-2.5 text-sm text-sand-700 hover:bg-sand-50 hover:text-sand-900 transition-base"
                      >
                        <svg
                          className="h-4 w-4 shrink-0 text-sand-400"
                          viewBox="0 0 20 20"
                          fill="currentColor"
                          aria-hidden="true"
                        >
                          <path
                            fillRule="evenodd"
                            d="M2.166 4.999A11.954 11.954 0 0 0 10 1.944 11.954 11.954 0 0 0 17.834 5c.11.65.166 1.32.166 2.001 0 5.225-3.34 9.67-8 11.317C5.34 16.67 2 12.225 2 7c0-.682.057-1.35.166-2.001zm11.541 3.708a1 1 0 0 0-1.414-1.414L9 10.586 7.707 9.293a1 1 0 0 0-1.414 1.414l2 2a1 1 0 0 0 1.414 0l4-4z"
                            clipRule="evenodd"
                          />
                        </svg>
                        Política de privacidad
                      </a>
                    </div>

                    {/* Logout */}
                    <div className="border-t border-sand-100 py-1">
                      <button
                        type="button"
                        role="menuitem"
                        onClick={handleLogout}
                        className="flex w-full items-center gap-2.5 px-4 py-2.5 text-sm text-red-600 hover:bg-red-50 transition-base"
                      >
                        <svg
                          className="h-4 w-4 shrink-0"
                          viewBox="0 0 20 20"
                          fill="currentColor"
                          aria-hidden="true"
                        >
                          <path
                            fillRule="evenodd"
                            d="M3 3a1 1 0 0 1 1-1h6a1 1 0 1 1 0 2H5v12h5a1 1 0 1 1 0 2H4a1 1 0 0 1-1-1V3zm13.707 5.293a1 1 0 0 1 0 1.414l-3 3a1 1 0 0 1-1.414-1.414L13.586 10l-1.293-1.293a1 1 0 1 1 1.414-1.414l3 3z"
                            clipRule="evenodd"
                          />
                          <path
                            fillRule="evenodd"
                            d="M8 10a1 1 0 0 1 1-1h7a1 1 0 1 1 0 2H9a1 1 0 0 1-1-1z"
                            clipRule="evenodd"
                          />
                        </svg>
                        Cerrar sesión
                      </button>
                    </div>
                  </motion.div>
                )}
              </AnimatePresence>
            </div>

            {/* Mobile avatar toggle (visible only on mobile) */}
            <button
              type="button"
              onClick={() => {
                setMenuOpen((v) => !v);
                setDropdownOpen(false);
              }}
              aria-label="Menú de usuario"
              aria-expanded={menuOpen}
              className={`flex h-8 w-8 items-center justify-center rounded-full text-xs font-bold text-white transition-base focus-visible:ring-2 focus-visible:ring-brand-400 md:hidden ${avatarColor(user?.name)}`}
            >
              {getInitials(user?.name)}
            </button>
          </div>
        </div>

        {/* Mobile nav drawer */}
        {menuOpen && (
          <nav
            aria-label="Navegación móvil"
            className="border-t border-sand-200 bg-surface px-4 py-3 flex flex-col gap-1 md:hidden animate-fade-in"
          >
            {/* Mobile user header */}
            <div className="flex items-center gap-3 pb-3">
              <span
                className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-full text-sm font-bold text-white ${avatarColor(user?.name)}`}
              >
                {getInitials(user?.name)}
              </span>
              <div className="min-w-0">
                <p className="text-sm font-semibold text-sand-900 truncate">
                  {user?.name}
                </p>
                <p className="text-xs text-sand-400 truncate">{user?.email}</p>
              </div>
              {user?.role && ROLE_BADGE[user.role] && (
                <span
                  className={`ml-auto shrink-0 rounded-full px-2.5 py-0.5 text-xs font-semibold ${ROLE_BADGE[user.role].cls}`}
                >
                  {ROLE_BADGE[user.role].label}
                </span>
              )}
            </div>
            <hr className="border-sand-200" />
            {NAV_MAIN.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                onClick={() => setMenuOpen(false)}
                className={navLinkCls}
              >
                {({ isActive }) => (
                  <>
                    {item.icon?.(isActive)}
                    {item.label}
                  </>
                )}
              </NavLink>
            ))}
            {extraNav && (
              <NavLink
                to={extraNav.to}
                onClick={() => setMenuOpen(false)}
                className={navLinkPlainCls}
              >
                {extraNav.label}
              </NavLink>
            )}
            {adminStatsNav && (
              <NavLink
                to={adminStatsNav.to}
                onClick={() => setMenuOpen(false)}
                className={navLinkPlainCls}
              >
                {adminStatsNav.label}
              </NavLink>
            )}
            {user?.role === "Owner" && (
              <>
                <hr className="my-1 border-sand-200" />
                <Link
                  to="/dashboard"
                  onClick={() => setMenuOpen(false)}
                  className="flex items-center gap-2 rounded-xl bg-danger-500 px-3 py-2 text-sm font-semibold text-white"
                >
                  <svg
                    viewBox="0 0 20 20"
                    fill="currentColor"
                    className="h-4 w-4"
                    aria-hidden="true"
                  >
                    <path
                      fillRule="evenodd"
                      d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495ZM10 5a.75.75 0 0 1 .75.75v3.5a.75.75 0 0 1-1.5 0v-3.5A.75.75 0 0 1 10 5Zm0 9a1 1 0 1 0 0-2 1 1 0 0 0 0 2Z"
                      clipRule="evenodd"
                    />
                  </svg>
                  Reportar mascota perdida
                </Link>
              </>
            )}
            <hr className="my-1 border-sand-200" />
            <NavLink
              to="/perfil"
              onClick={() => setMenuOpen(false)}
              className={navLinkCls}
            >
              Mi perfil
            </NavLink>
            <Link
              to="/dashboard"
              onClick={() => setMenuOpen(false)}
              className="flex items-center gap-2.5 px-4 py-2.5 text-sm text-sand-700 hover:bg-sand-50 hover:text-sand-900 transition-base"
            >
              <svg
                className="h-4 w-4 shrink-0 text-sand-400"
                viewBox="0 0 20 20"
                fill="currentColor"
                aria-hidden="true"
              >
                <path d="M4.5 2a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3zM9 3.5a1.5 1.5 0 1 1 3 0 1.5 1.5 0 0 1-3 0zm6 0a1.5 1.5 0 1 1 3 0 1.5 1.5 0 0 1-3 0zM2 8.5a1.5 1.5 0 1 1 3 0 1.5 1.5 0 0 1-3 0zM10 7a5 5 0 0 0-4.546 2.916A2.5 2.5 0 0 0 7 14.5h6a2.5 2.5 0 0 0 1.546-4.584A5 5 0 0 0 10 7z" />
              </svg>
              Mis mascotas
            </Link>

            {/* Sobre la app — móvil */}
            <hr className="my-1 border-sand-200" />
            <p className="px-3 pt-1 pb-0.5 text-[10px] font-semibold uppercase tracking-wider text-sand-400">
              Sobre la app
            </p>
            <a
              href="/legal/terminos-de-uso.html"
              target="_blank"
              rel="noopener noreferrer"
              onClick={() => setMenuOpen(false)}
              className="rounded-lg px-3 py-2 text-sm text-sand-600 hover:bg-sand-200 hover:text-sand-900 transition-base"
            >
              Términos de uso
            </a>
            <a
              href="/legal/politica-de-privacidad.html"
              target="_blank"
              rel="noopener noreferrer"
              onClick={() => setMenuOpen(false)}
              className="rounded-lg px-3 py-2 text-sm text-sand-600 hover:bg-sand-200 hover:text-sand-900 transition-base"
            >
              Política de privacidad
            </a>
            <hr className="my-1 border-sand-200" />
            <button
              type="button"
              onClick={handleLogout}
              className="rounded-lg px-3 py-2 text-sm text-red-600 hover:bg-red-50 text-left transition-base"
            >
              Cerrar sesión
            </button>
          </nav>
        )}
      </header>

      <OfflineQueueBanner />

      {/* ── Page context breadcrumb ───────────────────────────────────── */}
      {pageCtx && (
        <div className="border-b border-sand-100 bg-surface-warm">
          <div className="mx-auto flex h-9 max-w-6xl items-center gap-2 px-4">
            {isSubPage && (
              <button
                type="button"
                onClick={() => navigate(-1)}
                aria-label="Volver atrás"
                className="mr-1 flex h-6 w-6 shrink-0 items-center justify-center rounded-md text-sand-500 hover:bg-sand-100 hover:text-sand-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 transition-base"
              >
                <svg
                  viewBox="0 0 16 16"
                  fill="currentColor"
                  className="h-4 w-4"
                  aria-hidden="true"
                >
                  <path
                    fillRule="evenodd"
                    d="M9.78 4.22a.75.75 0 0 1 0 1.06L7.06 8l2.72 2.72a.75.75 0 1 1-1.06 1.06L5.47 8.53a.75.75 0 0 1 0-1.06l3.25-3.25a.75.75 0 0 1 1.06 0Z"
                    clipRule="evenodd"
                  />
                </svg>
              </button>
            )}
            <span aria-hidden="true" className="text-sm">
              {pageCtx.icon}
            </span>
            <span className="text-xs font-semibold text-sand-700">
              {pageCtx.label}
            </span>
          </div>
        </div>
      )}

      {/* ── Main content ───────────────────────────────────────────────── */}
      <AnimatePresence mode="wait" initial={false}>
        <motion.main
          key={location.pathname}
          className="mx-auto max-w-6xl px-4 py-6 pb-24 md:pb-6"
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -6 }}
          transition={{ duration: 0.22, ease: [0.4, 0, 0.2, 1] }}
        >
          <Outlet />
        </motion.main>
      </AnimatePresence>

      {/* ── Mobile bottom navigation ────────────────────────────────── */}
      <BottomNav />
    </div>
  );
}
