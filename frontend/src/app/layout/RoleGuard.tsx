import { Navigate, Outlet } from 'react-router-dom'
import { useAuthStore } from '@/features/auth/store/authStore'
import type { AuthUser } from '@/features/auth/store/authStore'

interface RoleGuardProps {
  /** Roles allowed to access this route */
  roles: AuthUser['role'][]
  /** Where to redirect unauthenticated users (default: /login) */
  unauthenticated?: string
  /** Where to redirect authenticated users without the required role (default: /dashboard) */
  unauthorized?: string
}

/**
 * Layout-level role guard. Use as the `element` on a wrapper route.
 * Redirects to /login if not authenticated, or /dashboard if wrong role.
 * Renders <Outlet /> for matched children.
 */
export function RoleGuard({
  roles,
  unauthenticated = '/login',
  unauthorized = '/dashboard',
}: RoleGuardProps) {
  const { isAuthenticated, user } = useAuthStore()

  if (!isAuthenticated || !user) {
    return <Navigate to={unauthenticated} replace />
  }

  if (!roles.includes(user.role)) {
    return <Navigate to={unauthorized} replace />
  }

  return <Outlet />
}
