import { type HTMLAttributes, type ReactNode } from 'react'

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode
  padding?: 'sm' | 'md' | 'lg' | 'none'
  shadow?: 'none' | 'sm' | 'md'
  border?: boolean
}

const paddingMap = {
  none: '',
  sm:   'p-4',
  md:   'p-5',
  lg:   'p-6 sm:p-8',
}

const shadowMap = {
  none: '',
  sm:   'shadow-xs',
  md:   'shadow-md',
}

export function Card({
  children,
  padding = 'md',
  shadow = 'sm',
  border = true,
  className = '',
  ...props
}: CardProps) {
  return (
    <div
      className={[
        'rounded-2xl bg-white',
        paddingMap[padding],
        shadowMap[shadow],
        border ? 'border border-sand-200' : '',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
      {...props}
    >
      {children}
    </div>
  )
}

// ── EmptyState ────────────────────────────────────────────────────────────────

interface EmptyStateProps {
  icon?: ReactNode
  title: string
  description?: string
  action?: ReactNode
  className?: string
}

export function EmptyState({ icon, title, description, action, className = '' }: EmptyStateProps) {
  return (
    <div
      className={[
        'flex flex-col items-center gap-4 rounded-2xl border-2 border-dashed border-sand-200',
        'py-16 px-6 text-center',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
    >
      {icon && (
        <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-sand-100 text-brand-500">
          {icon}
        </div>
      )}
      <div>
        <p className="text-lg font-semibold text-sand-800">{title}</p>
        {description && (
          <p className="mt-1 text-sm text-sand-500">{description}</p>
        )}
      </div>
      {action && <div className="mt-2">{action}</div>}
    </div>
  )
}

// ── Divider ──────────────────────────────────────────────────────────────────

interface DividerProps {
  label?: string
  className?: string
}

export function Divider({ label, className = '' }: DividerProps) {
  if (!label) {
    return <hr className={`border-sand-200 ${className}`} />
  }
  return (
    <div className={`flex items-center gap-3 ${className}`}>
      <hr className="flex-1 border-sand-200" />
      <span className="text-xs font-medium text-sand-400 uppercase tracking-wider">
        {label}
      </span>
      <hr className="flex-1 border-sand-200" />
    </div>
  )
}
