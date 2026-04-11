import { useState, useRef, useEffect } from 'react'
import { Outlet, Navigate, NavLink, Link, useNavigate } from 'react-router-dom'
import { NotificationBell } from '@/features/notifications/components/NotificationBell'
import { OfflineQueueBanner } from '@/features/lost-pets/components/OfflineQueueBanner'
import { useAuthStore } from '@/features/auth/store/authStore'
import { useLogout } from '@/features/auth/hooks/useAuth'
import { BottomNav } from './BottomNav'

// ── Nav items by role ────────────────────────────────────────────────────────
const NAV_MAIN = [
  { to: '/dashboard',  label: 'Inicio' },
  { to: '/map',        label: 'Mapa' },
]

const NAV_EXTRA_ALLY  = { to: '/allies/panel',  label: 'Panel Aliado' }
const NAV_EXTRA_CLINIC = { to: '/clinica/portal', label: 'Panel Clínica' }
const NAV_EXTRA_ADMIN  = { to: '/admin',          label: 'Administración' }

const activeCls =
  'text-brand-600 bg-brand-50 font-semibold'
const inactiveCls =
  'text-sand-600 hover:bg-sand-200 hover:text-sand-900'

const navLinkCls = ({ isActive }: { isActive: boolean }) =>
  [
    'rounded-lg px-3 py-1.5 text-sm transition-base',
    isActive ? activeCls : inactiveCls,
  ].join(' ')

export default function AuthenticatedLayout() {
  const { isAuthenticated, user } = useAuthStore()
  const [menuOpen, setMenuOpen] = useState(false)
  const [dropdownOpen, setDropdownOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)
  const { mutate: logout } = useLogout()
  const navigate = useNavigate()

  // Close desktop dropdown on outside click
  useEffect(() => {
    if (!dropdownOpen) return
    function handleOutside(e: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setDropdownOpen(false)
      }
    }
    document.addEventListener('mousedown', handleOutside)
    return () => document.removeEventListener('mousedown', handleOutside)
  }, [dropdownOpen])

  function handleLogout() {
    setDropdownOpen(false)
    setMenuOpen(false)
    logout(undefined, { onSuccess: () => navigate('/login', { replace: true }) })
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  const extraNav =
    user?.role === 'Ally'   ? NAV_EXTRA_ALLY  :
    user?.role === 'Clinic' ? NAV_EXTRA_CLINIC :
    user?.role === 'Admin'  ? NAV_EXTRA_ADMIN  :
    null

  return (
    <div className="min-h-dvh bg-sand-100">
      {/* ── Top bar ─────────────────────────────────────────────────────── */}
      <header className="sticky top-0 z-40 border-b border-sand-200 bg-white/95 backdrop-blur-sm">
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
                {item.label}
              </NavLink>
            ))}
            {extraNav && (
              <NavLink to={extraNav.to} className={navLinkCls}>
                {extraNav.label}
              </NavLink>
            )}
          </nav>

          {/* Spacer */}
          <div className="flex-1" />

          {/* Actions */}
          <div className="flex items-center gap-2">
            <NotificationBell />

            {/* ── User avatar: desktop dropdown + mobile drawer toggle ─── */}
            <div className="relative" ref={dropdownRef}>
              <button
                type="button"
                onClick={() => {
                  setDropdownOpen((v) => !v)
                  setMenuOpen(false)
                }}
                aria-label="Menú de usuario"
                aria-expanded={dropdownOpen}
                aria-haspopup="menu"
                className="flex h-8 w-8 items-center justify-center rounded-full bg-brand-100 text-sm font-semibold text-brand-700 hover:bg-brand-200 transition-base focus-visible:ring-2 focus-visible:ring-brand-400 md:flex hidden"
              >
                {user?.name?.[0]?.toUpperCase() ?? '?'}
              </button>

              {/* Desktop dropdown */}
              {dropdownOpen && (
                <div
                  role="menu"
                  className="absolute right-0 top-10 z-50 hidden md:block w-56 rounded-2xl border border-sand-200 bg-white shadow-xl animate-fade-in overflow-hidden"
                >
                  {/* User info */}
                  <div className="px-4 py-3 border-b border-sand-100">
                    <p className="text-sm font-semibold text-sand-900 truncate">{user?.name}</p>
                    <p className="text-xs text-sand-400 truncate">{user?.email}</p>
                  </div>

                  {/* Account links */}
                  <div className="py-1">
                    <Link
                      to="/perfil"
                      role="menuitem"
                      onClick={() => setDropdownOpen(false)}
                      className="flex items-center gap-2.5 px-4 py-2.5 text-sm text-sand-700 hover:bg-sand-50 hover:text-sand-900 transition-base"
                    >
                      <svg className="h-4 w-4 text-sand-400" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                        <path d="M10 10a4 4 0 1 0 0-8 4 4 0 0 0 0 8zm-7 8a7 7 0 0 1 14 0H3z" />
                      </svg>
                      Mi perfil
                    </Link>
                  </div>

                  {/* Sobre la app */}
                  <div className="border-t border-sand-100 py-1">
                    <p className="px-4 pt-2 pb-1 text-xs font-semibold uppercase tracking-wider text-sand-400">
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
                      <svg className="h-4 w-4 text-sand-400" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                        <path fillRule="evenodd" d="M4 4a2 2 0 0 1 2-2h4.586A2 2 0 0 1 12 2.586L15.414 6A2 2 0 0 1 16 7.414V16a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V4zm2 6a1 1 0 0 1 1-1h6a1 1 0 1 1 0 2H7a1 1 0 0 1-1-1zm1 3a1 1 0 1 0 0 2h6a1 1 0 1 0 0-2H7z" clipRule="evenodd" />
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
                      <svg className="h-4 w-4 text-sand-400" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                        <path fillRule="evenodd" d="M2.166 4.999A11.954 11.954 0 0 0 10 1.944 11.954 11.954 0 0 0 17.834 5c.11.65.166 1.32.166 2.001 0 5.225-3.34 9.67-8 11.317C5.34 16.67 2 12.225 2 7c0-.682.057-1.35.166-2.001zm11.541 3.708a1 1 0 0 0-1.414-1.414L9 10.586 7.707 9.293a1 1 0 0 0-1.414 1.414l2 2a1 1 0 0 0 1.414 0l4-4z" clipRule="evenodd" />
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
                      <svg className="h-4 w-4" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                        <path fillRule="evenodd" d="M3 3a1 1 0 0 1 1-1h6a1 1 0 1 1 0 2H5v12h5a1 1 0 1 1 0 2H4a1 1 0 0 1-1-1V3zm13.707 5.293a1 1 0 0 1 0 1.414l-3 3a1 1 0 0 1-1.414-1.414L13.586 10l-1.293-1.293a1 1 0 1 1 1.414-1.414l3 3z" clipRule="evenodd" />
                        <path fillRule="evenodd" d="M8 10a1 1 0 0 1 1-1h7a1 1 0 1 1 0 2H9a1 1 0 0 1-1-1z" clipRule="evenodd" />
                      </svg>
                      Cerrar sesión
                    </button>
                  </div>
                </div>
              )}
            </div>

            {/* Mobile avatar toggle (visible only on mobile) */}
            <button
              type="button"
              onClick={() => {
                setMenuOpen((v) => !v)
                setDropdownOpen(false)
              }}
              aria-label="Menú de usuario"
              aria-expanded={menuOpen}
              className="flex h-8 w-8 items-center justify-center rounded-full bg-brand-100 text-sm font-semibold text-brand-700 hover:bg-brand-200 transition-base focus-visible:ring-2 focus-visible:ring-brand-400 md:hidden"
            >
              {user?.name?.[0]?.toUpperCase() ?? '?'}
            </button>
          </div>
        </div>

        {/* Mobile nav drawer */}
        {menuOpen && (
          <nav
            aria-label="Navegación móvil"
            className="border-t border-sand-200 bg-white px-4 py-3 flex flex-col gap-1 md:hidden animate-fade-in"
          >
            {NAV_MAIN.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                onClick={() => setMenuOpen(false)}
                className={navLinkCls}
              >
                {item.label}
              </NavLink>
            ))}
            {extraNav && (
              <NavLink
                to={extraNav.to}
                onClick={() => setMenuOpen(false)}
                className={navLinkCls}
              >
                {extraNav.label}
              </NavLink>
            )}
            <hr className="my-2 border-sand-200" />
            <NavLink to="/perfil" onClick={() => setMenuOpen(false)} className={navLinkCls}>
              Mi perfil
            </NavLink>

            {/* Sobre la app — móvil */}
            <hr className="my-2 border-sand-200" />
            <p className="px-3 pt-1 pb-0.5 text-xs font-semibold uppercase tracking-wider text-sand-400">
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
            <hr className="my-2 border-sand-200" />
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

      {/* ── Main content ───────────────────────────────────────────────── */}
      <main className="mx-auto max-w-6xl px-4 py-6 pb-24 md:pb-6">
        <Outlet />
      </main>

      {/* ── Mobile bottom navigation ────────────────────────────────── */}
      <BottomNav />
    </div>
  )
}

