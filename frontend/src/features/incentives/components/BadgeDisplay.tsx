import type { ContributorBadge } from '../api/incentivesApi'

interface BadgeDisplayProps {
  badge: ContributorBadge
  size?: 'sm' | 'md'
  className?: string
}

const BADGE_CONFIG: Record<ContributorBadge, { label: string; icon: string; classes: string }> = {
  None:     { label: 'Sin insignia', icon: '·',  classes: 'bg-sand-100 text-sand-500' },
  Helper:   { label: 'Ayudante',    icon: '🌱', classes: 'bg-rescue-100 text-rescue-700' },
  Rescuer:  { label: 'Rescatista',  icon: '⭐', classes: 'bg-trust-100 text-trust-700' },
  Guardian: { label: 'Guardián',    icon: '💎', classes: 'bg-trust-100 text-trust-800' },
  Legend:   { label: 'Leyenda',     icon: '🏆', classes: 'bg-warn-100 text-warn-700' },
}

export function BadgeDisplay({ badge, size = 'md', className = '' }: BadgeDisplayProps) {
  const cfg = BADGE_CONFIG[badge]
  if (badge === 'None') return null

  const sizeClasses = size === 'sm'
    ? 'rounded-full px-2 py-0.5 text-xs font-medium'
    : 'rounded-full px-3 py-1 text-sm font-semibold'

  return (
    <span className={`inline-flex items-center gap-1 ${sizeClasses} ${cfg.classes} ${className}`}>
      <span aria-hidden="true">{cfg.icon}</span>
      {cfg.label}
    </span>
  )
}
