import { useState } from 'react'
import {
  CHECKLIST_ITEMS,
  useSearchChecklist,
  type ChecklistPhase,
  type ChecklistItem,
} from '../hooks/useSearchChecklist'

// ── Props ──────────────────────────────────────────────────────────────────────

export interface SearchChecklistProps {
  /** ID of the active lost-pet event — used as the localStorage persistence key */
  lostEventId: string
  /** Pet name for contextual copy */
  petName: string
  /** Extra CSS classes for the outer wrapper */
  className?: string
}

// ── Phase metadata ─────────────────────────────────────────────────────────────

interface PhaseMeta {
  label: string
  urgency: string
  emoji: string
  /** Tailwind classes for the section border when not done */
  borderClass: string
  /** Tailwind classes for the section background when not done */
  bgClass: string
  /** Tailwind classes for the header text */
  headingClass: string
  /** Tailwind classes for the urgency badge */
  badgeClass: string
}

const PHASE_META: Readonly<Record<ChecklistPhase, PhaseMeta>> = {
  '2h': {
    label: 'Primeras 2 horas',
    urgency: '¡Crítico!',
    emoji: '⚡',
    borderClass: 'border-red-200',
    bgClass: 'bg-red-50',
    headingClass: 'text-red-900',
    badgeClass: 'bg-red-100 text-red-700',
  },
  '24h': {
    label: 'Primeras 24 horas',
    urgency: 'Amplía la búsqueda',
    emoji: '🔍',
    borderClass: 'border-brand-200',
    bgClass: 'bg-brand-50',
    headingClass: 'text-brand-900',
    badgeClass: 'bg-brand-100 text-brand-700',
  },
  '3d': {
    label: 'Primeros 3 días',
    urgency: 'No te rindas',
    emoji: '💪',
    borderClass: 'border-sand-200',
    bgClass: 'bg-sand-50',
    headingClass: 'text-sand-800',
    badgeClass: 'bg-sand-200 text-sand-600',
  },
}

const PHASES: readonly ChecklistPhase[] = ['2h', '24h', '3d']

// ── Progress bar ───────────────────────────────────────────────────────────────

interface ProgressBarProps {
  completed: number
  total: number
}

function ProgressBar({ completed, total }: ProgressBarProps) {
  const pct = total === 0 ? 0 : Math.round((completed / total) * 100)
  return (
    <div
      role="progressbar"
      aria-valuenow={pct}
      aria-valuemin={0}
      aria-valuemax={100}
      aria-label={`${completed} de ${total} pasos completados`}
      className="h-1.5 w-full overflow-hidden rounded-full bg-sand-200"
    >
      <div
        className="h-full rounded-full bg-rescue-500 transition-[width] duration-500 ease-out"
        style={{ width: `${pct}%` }}
      />
    </div>
  )
}

// ── Phase section ──────────────────────────────────────────────────────────────

interface PhaseSectionProps {
  phase: ChecklistPhase
  items: readonly ChecklistItem[]
  checkedIds: ReadonlySet<string>
  onToggle: (id: string) => void
  isOpen: boolean
  onToggleOpen: () => void
  completed: number
  total: number
  done: boolean
}

function PhaseSection({
  phase,
  items,
  checkedIds,
  onToggle,
  isOpen,
  onToggleOpen,
  completed,
  total,
  done,
}: PhaseSectionProps) {
  const meta = PHASE_META[phase]
  const sectionId = `sc-section-${phase}`
  const headerId = `sc-header-${phase}`

  return (
    <div
      className={`overflow-hidden rounded-xl border transition-colors ${
        done
          ? 'border-rescue-200 bg-rescue-50'
          : `${meta.borderClass} ${meta.bgClass}`
      }`}
    >
      {/* Accordion header */}
      <button
        type="button"
        id={headerId}
        aria-expanded={isOpen}
        aria-controls={sectionId}
        onClick={onToggleOpen}
        className="flex w-full items-center gap-3 px-4 py-3 text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400"
      >
        {/* Phase icon / done check */}
        <span className="shrink-0 text-lg" aria-hidden="true">
          {done ? '✅' : meta.emoji}
        </span>

        {/* Title + urgency */}
        <span className="flex-1 min-w-0">
          <span
            className={`block text-sm font-bold ${done ? 'text-rescue-800' : meta.headingClass}`}
          >
            {meta.label}
          </span>
          {!done && (
            <span
              className={`mt-0.5 inline-block rounded-full px-2 py-0.5 text-[11px] font-semibold ${meta.badgeClass}`}
            >
              {meta.urgency}
            </span>
          )}
          {done && (
            <span className="mt-0.5 inline-block rounded-full bg-rescue-100 px-2 py-0.5 text-[11px] font-semibold text-rescue-700">
              ¡Completado!
            </span>
          )}
        </span>

        {/* Progress count */}
        <span
          className={`shrink-0 text-xs font-medium tabular-nums ${
            done ? 'text-rescue-700' : 'text-sand-500'
          }`}
          aria-hidden="true"
        >
          {completed}/{total}
        </span>

        {/* Caret */}
        <span
          className={`shrink-0 text-sand-400 transition-transform duration-200 ${
            isOpen ? 'rotate-180' : ''
          }`}
          aria-hidden="true"
        >
          <svg viewBox="0 0 20 20" fill="currentColor" className="size-4">
            <path
              fillRule="evenodd"
              d="M5.23 7.21a.75.75 0 0 1 1.06.02L10 11.168l3.71-3.938a.75.75 0 1 1 1.08 1.04l-4.25 4.5a.75.75 0 0 1-1.08 0l-4.25-4.5a.75.75 0 0 1 .02-1.06z"
              clipRule="evenodd"
            />
          </svg>
        </span>
      </button>

      {/* Accordion body */}
      {isOpen && (
        <ul id={sectionId} role="list" className="px-4 pb-3 pt-1 space-y-2">
          {items.map((item) => {
            const checked = checkedIds.has(item.id)
            return (
              <li key={item.id}>
                <label
                  className={`flex cursor-pointer select-none items-start gap-3 rounded-lg px-2 py-2 transition-colors hover:bg-black/5 ${
                    checked ? 'opacity-60' : ''
                  }`}
                >
                  <input
                    type="checkbox"
                    className="mt-0.5 size-4 shrink-0 cursor-pointer accent-rescue-600"
                    checked={checked}
                    onChange={() => onToggle(item.id)}
                    aria-label={item.label}
                  />
                  <span
                    className={`text-sm leading-snug ${
                      checked
                        ? 'line-through text-sand-400'
                        : done
                          ? 'text-rescue-800'
                          : 'text-sand-800'
                    }`}
                  >
                    {item.label}
                  </span>
                </label>
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}

// ── Main component ─────────────────────────────────────────────────────────────

/**
 * Time-phased action checklist for lost-pet recovery.
 *
 * - Three collapsible accordion sections (2h / 24h / 3d).
 * - Checkbox state persisted to localStorage keyed by `lostEventId`.
 * - Overall progress bar at the top.
 * - When all items complete: celebration state replaces the list.
 * - Zero external dependencies.
 */
export function SearchChecklist({ lostEventId, petName, className = '' }: SearchChecklistProps) {
  const { checkedIds, toggleItem, phaseProgress, totalProgress } =
    useSearchChecklist(lostEventId)

  // First phase open by default, others closed until user expands them
  const [openPhases, setOpenPhases] = useState<ReadonlySet<ChecklistPhase>>(
    () => new Set<ChecklistPhase>(['2h']),
  )

  const togglePhase = (phase: ChecklistPhase) => {
    setOpenPhases((prev) => {
      const next = new Set(prev)
      if (next.has(phase)) {
        next.delete(phase)
      } else {
        next.add(phase)
      }
      return next
    })
  }

  // ── All done celebration ───────────────────────────────────────────────────

  if (totalProgress.done) {
    return (
      <div
        className={`rounded-2xl border border-rescue-200 bg-rescue-50 p-5 text-center ${className}`}
        role="status"
        aria-live="polite"
      >
        <div className="mb-2 text-3xl" aria-hidden="true">
          🎉
        </div>
        <p className="text-sm font-bold text-rescue-800">
          ¡Completaste todos los pasos!
        </p>
        <p className="mt-1 text-xs text-rescue-700">
          Hiciste todo lo que está en tus manos. Esperamos que {petName} vuelva pronto a casa.
        </p>
      </div>
    )
  }

  // ── Checklist ─────────────────────────────────────────────────────────────

  return (
    <div className={`rounded-2xl border border-sand-200 bg-white p-4 ${className}`}>
      {/* Header */}
      <div className="mb-3">
        <h2 className="text-sm font-bold text-sand-900">
          📋 Qué hacer ahora
        </h2>
        <p className="mt-0.5 text-xs text-sand-500">
          Pasos basados en estudios de recuperación de mascotas.{' '}
          <span className="font-medium tabular-nums text-sand-700">
            {totalProgress.completed}/{totalProgress.total} completados
          </span>
        </p>
      </div>

      {/* Overall progress bar */}
      <ProgressBar
        completed={totalProgress.completed}
        total={totalProgress.total}
      />

      {/* Phase sections */}
      <div className="mt-3 space-y-2">
        {PHASES.map((phase) => {
          const phaseItems = CHECKLIST_ITEMS.filter((i) => i.phase === phase)
          const progress = phaseProgress[phase]
          return (
            <PhaseSection
              key={phase}
              phase={phase}
              items={phaseItems}
              checkedIds={checkedIds}
              onToggle={toggleItem}
              isOpen={openPhases.has(phase)}
              onToggleOpen={() => togglePhase(phase)}
              completed={progress.completed}
              total={progress.total}
              done={progress.done}
            />
          )
        })}
      </div>
    </div>
  )
}

