import type { PetStatus } from '../api/petsApi'

const config: Record<PetStatus, { label: string; bg: string; text: string; dot: string }> = {
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
  const { label, bg, text, dot } = config[status]

  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium ${bg} ${text} ${className}`}
    >
      <span className={`size-1.5 rounded-full ${dot}`} aria-hidden="true" />
      {label}
    </span>
  )
}
