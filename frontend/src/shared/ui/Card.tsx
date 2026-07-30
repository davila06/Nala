import { type ElementType, type HTMLAttributes, type ReactNode } from 'react'

// ── Card ──────────────────────────────────────────────────────────────────────

type CardVariant = 'default' | 'danger' | 'warn' | 'rescue' | 'trust'
type CardPadding = 'none' | 'sm' | 'md' | 'lg'

interface CardProps extends HTMLAttributes<HTMLElement> {
  children: ReactNode
  /** Semantic HTML tag. Defaults to 'div'. */
  as?: ElementType
  padding?: CardPadding
  shadow?: boolean
  border?: boolean
  variant?: CardVariant
}

const paddingMap: Record<CardPadding, string> = {
  none: '',
  sm:   'p-4',
  md:   'p-5',
  lg:   'p-6 sm:p-8',
}

const variantMap: Record<CardVariant, string> = {
  default: 'border-sand-200 bg-surface',
  danger:  'border-danger-200 bg-danger-50/40',
  warn:    'border-warn-200 bg-warn-50/40',
  rescue:  'border-rescue-200 bg-rescue-50/40',
  trust:   'border-trust-200 bg-trust-50/40',
}

export function Card({
  children,
  as: Tag = 'div',
  padding = 'md',
  shadow = false,
  border = true,
  variant = 'default',
  className = '',
  ...props
}: CardProps) {
  return (
    <Tag
      className={[
        'rounded-2xl',
        border ? `border ${variantMap[variant]}` : variantMap[variant].split(' ').slice(1).join(' '),
        paddingMap[padding],
        shadow ? 'shadow-sm' : '',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
      {...props}
    >
      {children}
    </Tag>
  )
}

// ── EmptyState ────────────────────────────────────────────────────────────────

interface EmptyStateProps {
  icon?: ReactNode
  title: string
  description?: string
  action?: ReactNode
  className?: string
  /** Subtle: smaller, less padding — for inline sections */
  subtle?: boolean
}

export function EmptyState({ icon, title, description, action, className = '', subtle = false }: EmptyStateProps) {
  return (
    <div
      className={[
        'flex flex-col items-center gap-4 rounded-2xl border-2 border-dashed border-sand-200 text-center',
        subtle ? 'py-8 px-4' : 'py-16 px-6',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
    >
      {icon && (
        <div
          className={[
            'flex items-center justify-center rounded-2xl bg-sand-100',
            subtle ? 'h-12 w-12' : 'h-16 w-16',
          ].join(' ')}
          style={{ animation: 'float-bob 4s ease-in-out infinite' }}
        >
          {icon}
        </div>
      )}
      <div>
        <p className={subtle ? 'text-base font-semibold text-sand-700' : 'text-lg font-semibold text-sand-800'}>
          {title}
        </p>
        {description && (
          <p className="mt-1 max-w-xs text-sm text-sand-500 leading-relaxed">{description}</p>
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
