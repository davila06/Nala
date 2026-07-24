import { motion, AnimatePresence } from 'framer-motion'
import type { PetStatus } from '../api/petsApi'

const config: Record<PetStatus, { label: string; bg: string; text: string; dot: string; pulse?: boolean }> = {
  Active: {
    label: 'Activa',
    bg: 'bg-rescue-50',
    text: 'text-rescue-700',
    dot: 'bg-rescue-500',
  },
  Lost: {
    label: 'Perdida',
    bg: 'bg-danger-50',
    text: 'text-danger-700',
    dot: 'bg-danger-500',
    pulse: true,
  },
  Reunited: {
    label: 'Reunida',
    bg: 'bg-trust-50',
    text: 'text-trust-700',
    dot: 'bg-trust-500',
  },
}

interface PetStatusBadgeProps {
  status: PetStatus
  className?: string
}

export const PetStatusBadge = ({ status, className = '' }: PetStatusBadgeProps) => {
  const { label, bg, text, dot, pulse } = config[status]

  return (
    <AnimatePresence mode="wait">
      <motion.span
        key={status}
        initial={{ scale: 0.85, opacity: 0 }}
        animate={{ scale: 1,    opacity: 1 }}
        exit={{    scale: 0.85, opacity: 0 }}
        transition={{ type: 'spring', stiffness: 400, damping: 28 }}
        className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium ${bg} ${text} ${className}`}
      >
        {/* Pulsing dot for Lost status */}
        <span className="relative flex h-1.5 w-1.5 flex-shrink-0" aria-hidden="true">
          {pulse && (
            <span className={`absolute inline-flex h-full w-full animate-ping rounded-full opacity-75 ${dot}`} />
          )}
          <span className={`relative inline-flex h-1.5 w-1.5 rounded-full ${dot}`} />
        </span>
        {label}
      </motion.span>
    </AnimatePresence>
  )
}

