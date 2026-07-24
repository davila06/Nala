import { useEffect, useRef } from 'react'
import { Link } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'
import { useUnreadCount } from '../hooks/useNotifications'

export function NotificationBell() {
  const { data: unreadCount = 0 } = useUnreadCount()
  const prevCount = useRef(unreadCount)
  const didIncrease = unreadCount > prevCount.current

  useEffect(() => {
    prevCount.current = unreadCount
  }, [unreadCount])

  return (
    <Link
      to="/notifications"
      aria-label={`Notificaciones${unreadCount > 0 ? ` — ${unreadCount} sin leer` : ''}`}
      className="relative flex h-11 w-11 items-center justify-center rounded-xl text-sand-600 hover:bg-sand-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
    >
      {/* Bell icon — shakes when new notification arrives */}
      <motion.svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        className="h-5 w-5"
        aria-hidden="true"
        animate={didIncrease ? { rotate: [0, -15, 15, -10, 10, 0] } : {}}
        transition={{ duration: 0.5, ease: 'easeInOut' }}
      >
        <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
        <path d="M13.73 21a2 2 0 0 1-3.46 0" />
      </motion.svg>

      {/* Animated count badge */}
      <AnimatePresence>
        {unreadCount > 0 && (
          <motion.span
            key={unreadCount}
            aria-hidden="true"
            initial={{ scale: 0, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            exit={{    scale: 0, opacity: 0 }}
            transition={{ type: 'spring', stiffness: 500, damping: 28 }}
            className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-brand-500 px-1 text-[10px] font-bold text-white"
          >
            {unreadCount > 99 ? '99+' : unreadCount}
          </motion.span>
        )}
      </AnimatePresence>
    </Link>
  )
}

