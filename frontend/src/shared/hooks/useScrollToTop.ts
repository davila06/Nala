import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'

/**
 * useScrollToTop — scrolls window to top on every route change.
 * Call once inside AuthenticatedLayout and PublicLayout.
 */
export function useScrollToTop() {
  const { pathname } = useLocation()

  useEffect(() => {
    // Use instant scroll so it doesn't fight with Framer Motion page transitions
    window.scrollTo({ top: 0, behavior: 'instant' as ScrollBehavior })
  }, [pathname])
}
