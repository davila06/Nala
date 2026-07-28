import { useState, useEffect } from 'react'
import { motion, AnimatePresence } from 'framer-motion'

interface BeforeInstallPromptEvent extends Event {
  readonly platforms: string[]
  prompt(): Promise<void>
  readonly userChoice: Promise<{ outcome: 'accepted' | 'dismissed'; platform: string }>
}

const DISMISSED_KEY = 'pwa-install-dismissed'

/**
 * PWAInstallBanner — custom "Add to Home Screen" banner.
 * Intercepts the browser's beforeinstallprompt event and shows a branded
 * prompt instead of the generic browser UI.
 *
 * Render once near the root (e.g. in App.tsx or AuthenticatedLayout).
 */
export function PWAInstallBanner() {
  const [deferredPrompt, setDeferredPrompt] = useState<BeforeInstallPromptEvent | null>(null)
  const [visible, setVisible] = useState(false)

  useEffect(() => {
    // Don't show if user dismissed in this session
    if (sessionStorage.getItem(DISMISSED_KEY)) return

    const handler = (e: Event) => {
      e.preventDefault()
      setDeferredPrompt(e as BeforeInstallPromptEvent)
      // Small delay so it doesn't pop up immediately on first load
      setTimeout(() => setVisible(true), 3000)
    }

    window.addEventListener('beforeinstallprompt', handler)
    return () => window.removeEventListener('beforeinstallprompt', handler)
  }, [])

  const handleInstall = async () => {
    if (!deferredPrompt) return
    await deferredPrompt.prompt()
    const choice = await deferredPrompt.userChoice
    if (choice.outcome === 'accepted') {
      setDeferredPrompt(null)
      setVisible(false)
    }
  }

  const handleDismiss = () => {
    sessionStorage.setItem(DISMISSED_KEY, '1')
    setVisible(false)
  }

  return (
    <AnimatePresence>
      {visible && (
        <motion.div
          role="dialog"
          aria-label="Instalar PawTrack CR"
          aria-live="polite"
          className="fixed bottom-20 inset-x-3 z-50 md:bottom-6 md:left-auto md:right-6 md:w-80"
          initial={{ opacity: 0, y: 24, scale: 0.96 }}
          animate={{ opacity: 1, y: 0, scale: 1 }}
          exit={{ opacity: 0, y: 16, scale: 0.97 }}
          transition={{ type: 'spring', stiffness: 380, damping: 36 }}
        >
          <div className="flex items-start gap-3.5 rounded-2xl border border-sand-200 field-input p-4 shadow-xl shadow-sand-900/10">
            {/* App icon */}
            <div className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-2xl bg-brand-500 text-2xl shadow-md shadow-brand-500/30">
              🐾
            </div>

            {/* Text */}
            <div className="flex-1 min-w-0">
              <p className="font-display text-sm font-semibold text-sand-900">
                Instala PawTrack CR
              </p>
              <p className="mt-0.5 text-xs text-sand-500 leading-snug">
                Acceso rápido, notificaciones y funciona sin conexión.
              </p>

              {/* Actions */}
              <div className="mt-3 flex gap-2">
                <button
                  type="button"
                  onClick={() => void handleInstall()}
                  className="rounded-lg bg-brand-500 px-3.5 py-1.5 text-xs font-bold text-white hover:bg-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 transition-colors"
                >
                  Instalar
                </button>
                <button
                  type="button"
                  onClick={handleDismiss}
                  className="rounded-lg border border-sand-200 px-3 py-1.5 text-xs font-semibold text-sand-500 hover:bg-sand-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 transition-colors"
                >
                  Ahora no
                </button>
              </div>
            </div>

            {/* Close */}
            <button
              type="button"
              onClick={handleDismiss}
              aria-label="Cerrar"
              className="flex-shrink-0 text-sand-400 hover:text-sand-600 transition-colors mt-0.5"
            >
              <svg viewBox="0 0 16 16" fill="none" className="h-4 w-4" aria-hidden="true">
                <path d="M3 3l10 10M13 3L3 13" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
              </svg>
            </button>
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  )
}
