import { Outlet, useLocation } from 'react-router-dom'
import { AnimatePresence, motion } from 'framer-motion'

/**
 * PublicLayout — wrapper for all unauthenticated pages.
 * Provides the warm ivory background from the design system.
 * Individual pages control their own layout (auth pages use a split card,
 * public map uses full-screen, etc.).
 */
export default function PublicLayout() {
  const location = useLocation()
  return (
    <div className="min-h-dvh bg-sand-100">
      <AnimatePresence mode="wait" initial={false}>
        <motion.div
          key={location.pathname}
          initial={{ opacity: 0, y: 8 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0 }}
          transition={{ duration: 0.2, ease: [0.4, 0, 0.2, 1] }}
        >
          <Outlet />
        </motion.div>
      </AnimatePresence>
    </div>
  )
}
