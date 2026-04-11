import { type ButtonHTMLAttributes, forwardRef } from 'react'

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'rescue'
type Size    = 'sm' | 'md' | 'lg'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  size?: Size
  loading?: boolean
  fullWidth?: boolean
}

const base =
  'inline-flex items-center justify-center gap-2 font-semibold rounded-xl transition-base ' +
  'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 ' +
  'disabled:pointer-events-none disabled:opacity-50 select-none cursor-pointer'

const variants: Record<Variant, string> = {
  primary:
    'bg-brand-500 text-white hover:bg-brand-600 active:bg-brand-700 ' +
    'focus-visible:ring-brand-400 shadow-sm',
  secondary:
    'bg-sand-100 text-sand-800 border border-sand-300 hover:bg-sand-200 ' +
    'active:bg-sand-300 focus-visible:ring-brand-400',
  ghost:
    'text-sand-700 hover:bg-sand-100 active:bg-sand-200 focus-visible:ring-brand-400',
  danger:
    'bg-danger-500 text-white hover:bg-danger-600 active:bg-danger-700 ' +
    'focus-visible:ring-danger-400 shadow-sm',
  rescue:
    'bg-rescue-500 text-white hover:bg-rescue-600 active:bg-rescue-700 ' +
    'focus-visible:ring-rescue-400 shadow-sm',
}

const sizes: Record<Size, string> = {
  sm: 'text-xs px-3 py-1.5 min-h-[44px]',
  md: 'text-sm px-4 py-2.5 h-11',
  lg: 'text-base px-6 py-3 h-12',
}

const Spinner = () => (
  <svg
    className="h-4 w-4 animate-spin"
    viewBox="0 0 24 24"
    fill="none"
    aria-hidden="true"
  >
    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="3" />
    <path
      className="opacity-75"
      fill="currentColor"
      d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
    />
  </svg>
)

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  (
    {
      variant = 'primary',
      size = 'md',
      loading = false,
      fullWidth = false,
      disabled,
      children,
      className = '',
      ...props
    },
    ref,
  ) => {
    return (
      <button
        ref={ref}
        disabled={disabled || loading}
        aria-busy={loading}
        className={[
          base,
          variants[variant],
          sizes[size],
          fullWidth ? 'w-full' : '',
          className,
        ]
          .filter(Boolean)
          .join(' ')}
        {...props}
      >
        {loading && <Spinner />}
        {children}
      </button>
    )
  },
)
Button.displayName = 'Button'
