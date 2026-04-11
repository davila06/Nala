// ── Types ─────────────────────────────────────────────────────────────────────

export interface QuietHoursValue {
  start: string // "HH:mm"
  end: string   // "HH:mm"
}

interface QuietHoursFormProps {
  value: QuietHoursValue | null
  onChange: (next: QuietHoursValue | null) => void
  disabled?: boolean
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * Inline form that lets users configure a daily quiet-hours window.
 * Times are expressed in Costa Rica local time (UTC-6).
 *
 * When the user clears both inputs the form emits `null` (no window).
 */
export function QuietHoursForm({ value, onChange, disabled = false }: QuietHoursFormProps) {
  const enabled = value !== null

  const handleToggle = () => {
    onChange(
      enabled
        ? null
        : { start: '22:00', end: '07:00' },
    )
  }

  const handleChange = (field: keyof QuietHoursValue, raw: string) => {
    if (!value) return
    onChange({ ...value, [field]: raw })
  }

  const timeInputCls = [
    'w-[110px] rounded-xl border px-2.5 py-1.5 text-xs transition-base outline-none',
    'focus:ring-2 focus:ring-brand-400 focus:border-brand-400',
    disabled
      ? 'border-sand-200 bg-sand-100 text-sand-400 cursor-not-allowed'
      : 'border-sand-300 bg-white text-sand-900 cursor-auto',
  ].join(' ')

  return (
    <div className="mt-3 rounded-2xl border border-sand-200 bg-sand-50 p-3">
      {/* Header row */}
      <div className="flex items-center justify-between gap-2">
        <div>
          <p className="text-[0.8rem] font-semibold text-sand-900">
            🌙 Horario de silencio
          </p>
          <p className="mt-0.5 text-[0.72rem] text-sand-500">
            No recibirás alertas durante este rango horario (hora de Costa Rica).
          </p>
        </div>

        {/* Enable/disable toggle */}
        <button
          type="button"
          role="switch"
          aria-checked={enabled}
          aria-label="Activar horario de silencio"
          disabled={disabled}
          onClick={handleToggle}
          className={[
            'relative h-[22px] w-10 flex-shrink-0 rounded-full border-0 transition-colors duration-200',
            enabled ? 'bg-warn-400' : 'bg-sand-300',
            disabled ? 'cursor-not-allowed opacity-50' : 'cursor-pointer',
          ].join(' ')}
        >
          <span
            aria-hidden
            className={[
              'absolute top-0.5 h-[18px] w-[18px] rounded-full bg-white shadow transition-[left] duration-200',
              enabled ? 'left-[20px]' : 'left-0.5',
            ].join(' ')}
          />
        </button>
      </div>

      {/* Time inputs — only shown when enabled */}
      {enabled && value && (
        <div className="mt-2.5 flex flex-wrap items-center gap-2">
          <label className="whitespace-nowrap text-xs text-sand-500">
            Desde
          </label>
          <input
            type="time"
            value={value.start}
            disabled={disabled}
            onChange={(e) => handleChange('start', e.target.value)}
            className={timeInputCls}
            aria-label="Inicio del horario de silencio"
          />
          <label className="whitespace-nowrap text-xs text-sand-500">
            hasta
          </label>
          <input
            type="time"
            value={value.end}
            disabled={disabled}
            onChange={(e) => handleChange('end', e.target.value)}
            className={timeInputCls}
            aria-label="Fin del horario de silencio"
          />
        </div>
      )}
    </div>
  )
}
