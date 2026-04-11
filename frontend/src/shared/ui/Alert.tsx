import { type ReactNode } from 'react'

type AlertVariant = 'info' | 'success' | 'warning' | 'error'

interface AlertProps {
  variant?: AlertVariant
  title?: string
  children: ReactNode
  className?: string
  /** Content for an inline icon. Defaults to a sensible SVG per variant. */
  icon?: ReactNode | false
}

const variantStyles: Record<
  AlertVariant,
  { container: string; icon: string; title: string }
> = {
  info: {
    container: 'bg-trust-50 border border-trust-200 text-trust-800',
    icon:      'text-trust-500',
    title:     'text-trust-800',
  },
  success: {
    container: 'bg-rescue-50 border border-rescue-200 text-rescue-800',
    icon:      'text-rescue-500',
    title:     'text-rescue-800',
  },
  warning: {
    container: 'bg-warn-50 border border-warn-200 text-warn-800',
    icon:      'text-warn-500',
    title:     'text-warn-800',
  },
  error: {
    container: 'bg-danger-50 border border-danger-200 text-danger-800',
    icon:      'text-danger-500',
    title:     'text-danger-800',
  },
}

function DefaultIcon({ variant }: { variant: AlertVariant }) {
  if (variant === 'success') {
    return (
      <svg viewBox="0 0 20 20" fill="currentColor" className="h-5 w-5 shrink-0" aria-hidden="true">
        <path fillRule="evenodd" d="M10 18a8 8 0 1 0 0-16 8 8 0 0 0 0 16zm3.857-9.809a.75.75 0 0 0-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 1 0-1.06 1.061l2.5 2.5a.75.75 0 0 0 1.137-.089l4-5.5z" clipRule="evenodd" />
      </svg>
    )
  }
  if (variant === 'error') {
    return (
      <svg viewBox="0 0 20 20" fill="currentColor" className="h-5 w-5 shrink-0" aria-hidden="true">
        <path fillRule="evenodd" d="M10 18a8 8 0 1 0 0-16 8 8 0 0 0 0 16zM8.28 7.22a.75.75 0 0 0-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 1 0 1.06 1.06L10 11.06l1.72 1.72a.75.75 0 1 0 1.06-1.06L11.06 10l1.72-1.72a.75.75 0 0 0-1.06-1.06L10 8.94 8.28 7.22z" clipRule="evenodd" />
      </svg>
    )
  }
  if (variant === 'warning') {
    return (
      <svg viewBox="0 0 20 20" fill="currentColor" className="h-5 w-5 shrink-0" aria-hidden="true">
        <path fillRule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 0 1 .75.75v3.5a.75.75 0 0 1-1.5 0v-3.5A.75.75 0 0 1 10 5zm0 9a1 1 0 1 1 0-2 1 1 0 0 1 0 2z" clipRule="evenodd" />
      </svg>
    )
  }
  return (
    <svg viewBox="0 0 20 20" fill="currentColor" className="h-5 w-5 shrink-0" aria-hidden="true">
      <path fillRule="evenodd" d="M18 10a8 8 0 1 1-16 0 8 8 0 0 1 16 0zm-7-4a1 1 0 1 1-2 0 1 1 0 0 1 2 0zM9 9a.75.75 0 0 0 0 1.5h.253a.25.25 0 0 1 .244.304l-.459 2.066A1.75 1.75 0 0 0 10.747 15H11a.75.75 0 0 0 0-1.5h-.253a.25.25 0 0 1-.244-.304l.459-2.066A1.75 1.75 0 0 0 9.253 9H9z" clipRule="evenodd" />
    </svg>
  )
}

export function Alert({ variant = 'info', title, children, className = '', icon }: AlertProps) {
  const styles = variantStyles[variant]
  const roleAttr = variant === 'error' || variant === 'warning' ? 'alert' : 'status'

  return (
    <div
      role={roleAttr}
      className={['flex gap-3 rounded-xl p-4 text-sm', styles.container, className].join(' ')}
    >
      {icon !== false && (
        <div className={styles.icon}>
          {icon ?? <DefaultIcon variant={variant} />}
        </div>
      )}
      <div className="flex flex-col gap-0.5">
        {title && <p className={`font-semibold ${styles.title}`}>{title}</p>}
        <div className="leading-relaxed">{children}</div>
      </div>
    </div>
  )
}
