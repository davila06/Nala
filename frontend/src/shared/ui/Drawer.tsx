import { useEffect, useRef, type ReactNode } from 'react'
import { AnimatePresence, motion, type TargetAndTransition } from 'framer-motion'
import { createPortal } from 'react-dom'

// ── Backdrop ──────────────────────────────────────────────────────────────────

function Backdrop({ onClick }: { onClick: () => void }) {
  return (
    <motion.div
      className="fixed inset-0 z-40 bg-zinc-900/50 backdrop-blur-sm"
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.2 }}
      onClick={onClick}
      aria-hidden="true"
    />
  )
}

// ── Drawer ────────────────────────────────────────────────────────────────────

type DrawerSide = 'bottom' | 'right' | 'left'

interface DrawerVariant {
  initial: TargetAndTransition
  animate: TargetAndTransition
  exit:    TargetAndTransition
  className: string
}

interface DrawerProps {
  isOpen: boolean
  onClose: () => void
  title?: string
  description?: string
  children: ReactNode
  side?: DrawerSide
  /** Max width for side drawers (default 440px) */
  maxWidth?: number
}

const VARIANTS: Record<DrawerSide, DrawerVariant> = {
  bottom: {
    initial: { y: '100%' },
    animate: { y: 0 },
    exit:    { y: '100%' },
    className: 'fixed bottom-0 inset-x-0 z-50 rounded-t-3xl bg-white max-h-[92dvh] flex flex-col',
  },
  right: {
    initial: { x: '100%' },
    animate: { x: 0 },
    exit:    { x: '100%' },
    className: 'fixed right-0 top-0 bottom-0 z-50 bg-white flex flex-col shadow-2xl',
  },
  left: {
    initial: { x: '-100%' },
    animate: { x: 0 },
    exit:    { x: '-100%' },
    className: 'fixed left-0 top-0 bottom-0 z-50 bg-white flex flex-col shadow-2xl',
  },
}

export function Drawer({
  isOpen,
  onClose,
  title,
  description,
  children,
  side = 'bottom',
  maxWidth = 440,
}: DrawerProps) {
  const variant = VARIANTS[side]
  const contentRef = useRef<HTMLDivElement>(null)

  // Trap focus within drawer
  useEffect(() => {
    if (!isOpen) return
    const prevFocus = document.activeElement as HTMLElement
    contentRef.current?.focus()
    return () => {
      prevFocus?.focus()
    }
  }, [isOpen])

  // Close on Escape
  useEffect(() => {
    if (!isOpen) return
    const handleKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', handleKey)
    return () => document.removeEventListener('keydown', handleKey)
  }, [isOpen, onClose])

  // Prevent body scroll when open
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden'
    } else {
      document.body.style.overflow = ''
    }
    return () => { document.body.style.overflow = '' }
  }, [isOpen])

  return createPortal(
    <AnimatePresence>
      {isOpen && (
        <>
          <Backdrop onClick={onClose} />
          <motion.div
            ref={contentRef}
            role="dialog"
            aria-modal="true"
            aria-label={title}
            tabIndex={-1}
            className={variant.className}
            style={side !== 'bottom' ? { width: '100%', maxWidth } : undefined}
            initial={variant.initial}
            animate={variant.animate}
            exit={variant.exit}
            transition={{ type: 'spring', stiffness: 380, damping: 40, mass: 0.8 }}
          >
            {/* Handle (bottom drawer) */}
            {side === 'bottom' && (
              <div className="mx-auto mt-3 h-1 w-10 flex-shrink-0 rounded-full bg-sand-300" aria-hidden="true" />
            )}

            {/* Header */}
            {(title || side !== 'bottom') && (
              <div className="flex items-start justify-between gap-4 px-5 py-4 border-b border-sand-100">
                <div>
                  {title && (
                    <h2 className="font-display text-lg font-semibold text-sand-900">{title}</h2>
                  )}
                  {description && (
                    <p className="mt-0.5 text-sm text-sand-500">{description}</p>
                  )}
                </div>
                <button
                  type="button"
                  onClick={onClose}
                  aria-label="Cerrar"
                  className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full text-sand-400 hover:bg-sand-100 hover:text-sand-600 transition-base focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:outline-none"
                >
                  <svg viewBox="0 0 16 16" fill="none" className="h-4 w-4" aria-hidden="true">
                    <path d="M3 3l10 10M13 3L3 13" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
                  </svg>
                </button>
              </div>
            )}

            {/* Content */}
            <div className="flex-1 overflow-y-auto overscroll-contain px-5 py-4">
              {children}
            </div>
          </motion.div>
        </>
      )}
    </AnimatePresence>,
    document.body,
  )
}

// ── Modal (centered dialog) ───────────────────────────────────────────────────

interface ModalProps {
  isOpen: boolean
  onClose: () => void
  title?: string
  description?: string
  children: ReactNode
  /** Max width in px (default 480) */
  maxWidth?: number
}

export function Modal({ isOpen, onClose, title, description, children, maxWidth = 480 }: ModalProps) {
  const contentRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!isOpen) return
    const prevFocus = document.activeElement as HTMLElement
    contentRef.current?.focus()
    return () => { prevFocus?.focus() }
  }, [isOpen])

  useEffect(() => {
    if (!isOpen) return
    const handleKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose() }
    document.addEventListener('keydown', handleKey)
    return () => document.removeEventListener('keydown', handleKey)
  }, [isOpen, onClose])

  useEffect(() => {
    document.body.style.overflow = isOpen ? 'hidden' : ''
    return () => { document.body.style.overflow = '' }
  }, [isOpen])

  return createPortal(
    <AnimatePresence>
      {isOpen && (
        <>
          <Backdrop onClick={onClose} />
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <motion.div
              ref={contentRef}
              role="dialog"
              aria-modal="true"
              aria-label={title}
              tabIndex={-1}
              className="w-full rounded-3xl bg-white shadow-2xl flex flex-col max-h-[90dvh] overflow-hidden"
              style={{ maxWidth }}
              initial={{ opacity: 0, scale: 0.94, y: 12 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.96, y: 8 }}
              transition={{ type: 'spring', stiffness: 400, damping: 38 }}
            >
              {/* Header */}
              <div className="flex items-start justify-between gap-4 px-6 py-5 border-b border-sand-100">
                <div>
                  {title && (
                    <h2 className="font-display text-xl font-semibold text-sand-900">{title}</h2>
                  )}
                  {description && (
                    <p className="mt-1 text-sm text-sand-500">{description}</p>
                  )}
                </div>
                <button
                  type="button"
                  onClick={onClose}
                  aria-label="Cerrar"
                  className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full text-sand-400 hover:bg-sand-100 hover:text-sand-600 transition-base focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:outline-none"
                >
                  <svg viewBox="0 0 16 16" fill="none" className="h-4 w-4" aria-hidden="true">
                    <path d="M3 3l10 10M13 3L3 13" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
                  </svg>
                </button>
              </div>

              {/* Content */}
              <div className="flex-1 overflow-y-auto overscroll-contain px-6 py-5">
                {children}
              </div>
            </motion.div>
          </div>
        </>
      )}
    </AnimatePresence>,
    document.body,
  )
}
