import { type ReactNode } from 'react'

type BadgeVariant = 'default' | 'brand' | 'rescue' | 'danger' | 'warn' | 'trust' | 'neutral'
type BadgeSize    = 'sm' | 'md'

interface BadgeProps {
  variant?: BadgeVariant
  size?: BadgeSize
  children: ReactNode
  className?: string
  dot?: boolean
}

const base = 'inline-flex items-center gap-1.5 font-semibold rounded-full'

const variants: Record<BadgeVariant, string> = {
  default:  'bg-sand-100 text-sand-700',
  brand:    'bg-brand-100 text-brand-700',
  rescue:   'bg-rescue-100 text-rescue-700',
  danger:   'bg-danger-100 text-danger-700',
  warn:     'bg-warn-100 text-warn-700',
  trust:    'bg-trust-100 text-trust-700',
  neutral:  'bg-sand-100 text-sand-600',
}

const sizes: Record<BadgeSize, string> = {
  sm: 'text-xs px-2 py-0.5',
  md: 'text-xs px-2.5 py-1',
}

const dotVariants: Record<BadgeVariant, string> = {
  default: 'bg-sand-400',
  brand:   'bg-brand-500',
  rescue:  'bg-rescue-500',
  danger:  'bg-danger-500',
  warn:    'bg-warn-500',
  trust:   'bg-trust-500',
  neutral: 'bg-sand-400',
}

export function Badge({
  variant = 'default',
  size = 'md',
  children,
  className = '',
  dot = false,
}: BadgeProps) {
  return (
    <span className={[base, variants[variant], sizes[size], className].filter(Boolean).join(' ')}>
      {dot && (
        <span
          className={`h-1.5 w-1.5 rounded-full ${dotVariants[variant]}`}
          aria-hidden="true"
        />
      )}
      {children}
    </span>
  )
}

