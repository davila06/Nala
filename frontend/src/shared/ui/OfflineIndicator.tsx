import { useState, useEffect, useCallback } from 'react'
import { motion, AnimatePresence } from 'framer-motion'

/**
 * OfflineIndicator — slim banner at the very top of the screen.
 * Appears when navigator.onLine becomes false, auto-dismisses 3s after reconnecting.
 * Render once in App.tsx or AuthenticatedLayout.
 */
export function OfflineIndicator() {
  const [isOnline, setIsOnline] = useState(() => navigator.onLine)
  const [showReconnected, setShowReconnected] = useState(false)
  const [visible, setVisible] = useState(false)

  const handleOnline = useCallback(() => {
    setIsOnline(true)
    setShowReconnected(true)
    setVisible(true)
    setTimeout(() => setVisible(false), 3000)
  }, [])

  const handleOffline = useCallback(() => {
    setIsOnline(false)
    setShowReconnected(false)
    setVisible(true)
  }, [])

  useEffect(() => {
    window.addEventListener('online',  handleOnline)
    window.addEventListener('offline', handleOffline)
    return () => {
      window.removeEventListener('online',  handleOnline)
      window.removeEventListener('offline', handleOffline)
    }
  }, [handleOnline, handleOffline])

  return (
    <AnimatePresence>
      {visible && (
        <motion.div
          role="status"
          aria-live="polite"
          aria-atomic="true"
          initial={{ y: -40, opacity: 0 }}
          animate={{ y: 0,   opacity: 1 }}
          exit={{    y: -40, opacity: 0 }}
          transition={{ type: 'spring', stiffness: 400, damping: 35 }}
          className={[
            'fixed inset-x-0 top-0 z-[9999] flex items-center justify-center gap-2 py-2 text-xs font-semibold text-white shadow-md',
            isOnline && showReconnected
              ? 'bg-rescue-600'
              : 'bg-zinc-900',
          ].join(' ')}
        >
          {isOnline && showReconnected ? (
            <>
              <span className="relative flex h-2 w-2">
                <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-rescue-300 opacity-75" />
                <span className="relative inline-flex h-2 w-2 rounded-full bg-white" />
              </span>
              Conexión restaurada
            </>
          ) : (
            <>
              <svg viewBox="0 0 16 16" fill="none" className="h-3.5 w-3.5" aria-hidden="true">
                <path d="M2 2l12 12M6.5 6.5A4 4 0 0 0 4 10m7.5-3.5A4 4 0 0 1 12 10M8 13h.01" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
              </svg>
              Sin conexión — algunos datos pueden estar desactualizados
            </>
          )}
        </motion.div>
      )}
    </AnimatePresence>
  )
}
