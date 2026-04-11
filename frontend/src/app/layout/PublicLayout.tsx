import { Outlet } from 'react-router-dom'

/**
 * PublicLayout — wrapper for all unauthenticated pages.
 * Provides the warm ivory background from the design system.
 * Individual pages control their own layout (auth pages use a split card,
 * public map uses full-screen, etc.).
 */
export default function PublicLayout() {
  return (
    <div className="min-h-dvh bg-sand-100">
      <Outlet />
    </div>
  )
}
