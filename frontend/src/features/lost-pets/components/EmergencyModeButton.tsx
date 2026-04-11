import type {
  EmergencyStep,
  EmergencyStepId,
  EmergencyStepStatus,
  UseEmergencyModeReturn,
} from '../hooks/useEmergencyMode'

// ── Props ──────────────────────────────────────────────────────────────────────

export interface EmergencyModeButtonProps {
  /** Return value of useEmergencyMode(). */
  emergencyMode: UseEmergencyModeReturn
  /** Extra CSS classes applied to the outer wrapper div. */
  className?: string
}

// ── Step icon ──────────────────────────────────────────────────────────────────

function StepSpinner() {
  return (
    <span
      className="inline-block size-3.5 shrink-0 animate-spin rounded-full border-2 border-current border-t-transparent"
      aria-hidden="true"
    />
  )
}

const STEP_ICON: Record<EmergencyStepStatus, React.ReactNode> = {
  pending: <span aria-hidden="true" className="inline-block size-3.5 shrink-0 text-center leading-none opacity-30">·</span>,
  running: <StepSpinner />,
  done: <span aria-hidden="true" className="inline-block size-3.5 shrink-0 text-center font-bold leading-none">✓</span>,
  skipped: <span aria-hidden="true" className="inline-block size-3.5 shrink-0 text-center leading-none">—</span>,
  error: <span aria-hidden="true" className="inline-block size-3.5 shrink-0 text-center font-bold leading-none">✗</span>,
}

// ── Step label mapping ─────────────────────────────────────────────────────────

/** Inside the active (red) button — all text is white-based. */
const BUTTON_STEP_CLASS: Record<EmergencyStepStatus, string> = {
  pending:  'text-white/40',
  running:  'text-white font-semibold',
  done:     'text-white/70',
  skipped:  'text-white/30',
  error:    'text-red-200',
}

/** Inside the success card — colour-coded by outcome. */
const CARD_STEP_CLASS: Record<EmergencyStepStatus, string> = {
  pending:  'text-sand-400',
  running:  'text-brand-700 font-semibold',
  done:     'text-rescue-700',
  skipped:  'text-sand-400',
  error:    'text-red-500',
}

// ── Sub-components ────────────────────────────────────────────────────────────

interface StepRowProps {
  step: EmergencyStep
  variant: 'button' | 'card'
}

function StepRow({ step, variant }: StepRowProps) {
  const colorClass =
    variant === 'button'
      ? BUTTON_STEP_CLASS[step.status]
      : CARD_STEP_CLASS[step.status]

  return (
    <li
      className={`flex items-center gap-2 text-sm transition-colors duration-300 ${colorClass}`}
    >
      {STEP_ICON[step.status]}
      <span>{step.label}</span>
    </li>
  )
}

interface StepListProps {
  steps: readonly EmergencyStep[]
  variant: 'button' | 'card'
}

function StepList({ steps, variant }: StepListProps) {
  return (
    <ul
      className="space-y-1.5"
      aria-label="Pasos del modo emergencia"
      aria-live="polite"
      aria-atomic="false"
    >
      {steps.map((step) => (
        <StepRow key={step.id} step={step} variant={variant} />
      ))}
    </ul>
  )
}

// ── Connector line between steps ───────────────────────────────────────────────

const STEP_ORDER: readonly EmergencyStepId[] = [
  'flyer',
  'share',
  'link',
  'checklist',
]

/** Calculates overall progress (0–4) for accessible status text. */
function countDoneOrSkipped(steps: readonly EmergencyStep[]): number {
  return steps.filter(
    (s) => s.status === 'done' || s.status === 'skipped',
  ).length
}

// ── Main component ────────────────────────────────────────────────────────────

/**
 * Prominent call-to-action that triggers the emergency-mode sequence and
 * renders an animated mini-timeline while it runs.
 *
 * ### States
 * | State      | Render                                                    |
 * |------------|-----------------------------------------------------------|
 * | idle       | Pulsing red/rose gradient button with descriptive copy.   |
 * | running    | Same button — steps list animates in below the header.    |
 * | finished   | Green success card showing the outcome of each step.      |
 *
 * ### Accessibility
 * - `aria-busy` on the button reflects the running state.
 * - The step list uses `aria-live="polite"` so screen-readers announce updates.
 * - An `aria-label` on the overall status describes total progress.
 */
export function EmergencyModeButton({
  emergencyMode,
  className = '',
}: EmergencyModeButtonProps) {
  const { steps, isRunning, isFinished, run } = emergencyMode

  const doneCount = countDoneOrSkipped(steps)
  const totalCount = STEP_ORDER.length

  // ── Success card ──────────────────────────────────────────────────────────

  if (isFinished) {
    return (
      <div
        className={`rounded-2xl border border-rescue-200 bg-rescue-50 px-5 py-4 ${className}`}
        role="region"
        aria-label="Modo emergencia completado"
      >
        <p className="mb-3 font-bold text-rescue-800">
          ✅ Modo emergencia ejecutado
        </p>
        <StepList steps={steps} variant="card" />
      </div>
    )
  }

  // ── Active button (idle + running) ────────────────────────────────────────

  return (
    <div className={className}>
      <button
        type="button"
        onClick={() => void run()}
        disabled={isRunning}
        aria-busy={isRunning}
        aria-label={
          isRunning
            ? `Ejecutando modo emergencia: ${doneCount} de ${totalCount} pasos completos`
            : 'Activar modo emergencia: genera flyer, comparte y copia enlace en un toque'
        }
        className={[
          'relative w-full overflow-hidden rounded-2xl px-5 py-4 text-left',
          'transition-all duration-300',
          'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-rose-400',
          isRunning
            ? 'cursor-wait bg-red-600'
            : [
                'bg-gradient-to-br from-red-500 to-rose-600',
                'shadow-lg shadow-red-200',
                'hover:shadow-xl hover:from-red-600 hover:to-rose-700',
                'active:scale-[0.99]',
                'motion-safe:animate-pulse',
              ].join(' '),
        ]
          .filter(Boolean)
          .join(' ')}
      >
        {/* ── Header ─────────────────────────────────────────────────────── */}
        <div className="flex items-center gap-3">
          <span className="shrink-0 text-2xl" aria-hidden="true">
            🆘
          </span>
          <div className="min-w-0">
            <p className="text-base font-bold leading-tight text-white">
              Modo Emergencia
            </p>
            <p className="mt-0.5 text-xs leading-snug text-red-100">
              {isRunning
                ? 'Ejecutando pasos…'
                : 'Flyer · compartir · copiar enlace · checklist — en un toque'}
            </p>
          </div>
        </div>

        {/* ── Animated step list — only while running ─────────────────────── */}
        {isRunning && (
          <div className="mt-4 border-t border-red-400/30 pt-3">
            <StepList steps={steps} variant="button" />
          </div>
        )}
      </button>
    </div>
  )
}

