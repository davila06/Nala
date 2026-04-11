import { type HTMLAttributes } from 'react'

type SpinnerSize = 'xs' | 'sm' | 'md' | 'lg'

interface SpinnerProps {
  size?: SpinnerSize
  label?: string
  className?: string
}

const sizes: Record<SpinnerSize, string> = {
  xs: 'h-3 w-3 border-[1.5px]',
  sm: 'h-4 w-4 border-2',
  md: 'h-6 w-6 border-2',
  lg: 'h-8 w-8 border-[3px]',
}

/** Accessible spinner using CSS border trick (no SVG import needed). */
export function Spinner({ size = 'md', label = 'Cargando…', className = '' }: SpinnerProps) {
  return (
    <span
      role="status"
      aria-label={label}
      className={[
        'inline-block rounded-full border-sand-300 border-t-brand-500 animate-spin',
        sizes[size],
        className,
      ]
        .filter(Boolean)
        .join(' ')}
    />
  )
}

// ── Skeleton ─────────────────────────────────────────────────────────────────

interface SkeletonProps extends HTMLAttributes<HTMLDivElement> {
  /** Optional fixed height (e.g. "h-32"). Defaults to full height of container. */
  height?: string
}

export function Skeleton({ height = 'h-5', className = '', ...props }: SkeletonProps) {
  return (
    <div
      aria-hidden="true"
      className={[
        'animate-pulse-soft rounded-xl bg-sand-200',
        height,
        className,
      ]
        .filter(Boolean)
        .join(' ')}
      {...props}
    />
  )
}

/** Full-page centered loading indicator. */
export function PageSpinner() {
  return (
    <div className="flex min-h-[60vh] items-center justify-center">
      <Spinner size="lg" />
    </div>
  )
}
