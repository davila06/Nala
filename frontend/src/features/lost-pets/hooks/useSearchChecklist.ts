import { useCallback, useMemo, useState } from 'react'

// ── Domain types ───────────────────────────────────────────────────────────────

export type ChecklistPhase = '2h' | '24h' | '3d'

export interface ChecklistItem {
  readonly id: string
  readonly phase: ChecklistPhase
  readonly label: string
}

export interface PhaseProgress {
  readonly total: number
  readonly completed: number
  readonly done: boolean
}

export interface SearchChecklistState {
  /** All checklist items in definition order */
  readonly items: readonly ChecklistItem[]
  /** Set of IDs that have been checked */
  readonly checkedIds: ReadonlySet<string>
  /** Toggle a single item on / off */
  toggleItem: (id: string) => void
  /** Progress keyed by phase */
  readonly phaseProgress: Readonly<Record<ChecklistPhase, PhaseProgress>>
  /** Aggregate progress across all phases */
  readonly totalProgress: PhaseProgress
  /** Clear all checked items and remove persisted data */
  reset: () => void
}

// ── Checklist definition ───────────────────────────────────────────────────────
// Based on evidence-based pet-recovery studies (within-radius search success rates).

export const CHECKLIST_ITEMS: readonly ChecklistItem[] = [
  // ── Primeras 2 horas ────────────────────────────────────────────────────────
  {
    id: '2h-1',
    phase: '2h',
    label: 'Revisa los alrededores inmediatos del punto de pérdida',
  },
  {
    id: '2h-2',
    phase: '2h',
    label: 'Deja ropa con tu olor cerca del lugar (los perros vuelven por el olfato)',
  },
  {
    id: '2h-3',
    phase: '2h',
    label: 'Avisa a vecinos del área (porteros, tiendas, etc.)',
  },
  {
    id: '2h-4',
    phase: '2h',
    label: 'Publica en grupos de WhatsApp del vecindario',
  },

  // ── Primeras 24 horas ────────────────────────────────────────────────────────
  {
    id: '24h-1',
    phase: '24h',
    label: 'Llama a 3 veterinarias cercanas',
  },
  {
    id: '24h-2',
    phase: '24h',
    label: 'Contacta el refugio / perrera municipal más cercano',
  },
  {
    id: '24h-3',
    phase: '24h',
    label: 'Publica en grupos de Facebook de tu cantón',
  },
  {
    id: '24h-4',
    phase: '24h',
    label: 'Coloca flyers impresos en postes y tiendas del área',
  },

  // ── Primeros 3 días ──────────────────────────────────────────────────────────
  {
    id: '3d-1',
    phase: '3d',
    label: 'Regresa al lugar de pérdida al amanecer (mayor actividad animal)',
  },
  {
    id: '3d-2',
    phase: '3d',
    label: 'Contacta tiendas de alimento para mascotas del área',
  },
  {
    id: '3d-3',
    phase: '3d',
    label: 'Amplía el radio de búsqueda a 1 km',
  },
]

const ALL_IDS = new Set(CHECKLIST_ITEMS.map((i) => i.id))

// ── Persistence helpers ────────────────────────────────────────────────────────

const storageKey = (lostEventId: string) => `pawtrack:checklist:${lostEventId}`

function readChecked(lostEventId: string): ReadonlySet<string> {
  try {
    const raw = localStorage.getItem(storageKey(lostEventId))
    if (!raw) return new Set()
    const parsed: unknown = JSON.parse(raw)
    if (Array.isArray(parsed)) {
      // Validate: only accept IDs that are actually in the definition
      const valid = (parsed as unknown[]).filter(
        (v): v is string => typeof v === 'string' && ALL_IDS.has(v),
      )
      return new Set(valid)
    }
  } catch {
    // Corrupt data — start fresh
  }
  return new Set()
}

function writeChecked(lostEventId: string, checked: ReadonlySet<string>): void {
  try {
    localStorage.setItem(storageKey(lostEventId), JSON.stringify([...checked]))
  } catch {
    // Storage full or restricted (private browsing) — silently ignore
  }
}

// ── Hook ───────────────────────────────────────────────────────────────────────

/**
 * Manages the state and localStorage persistence of the lost-pet action checklist.
 *
 * Storage key: `pawtrack:checklist:{lostEventId}`
 * Storage format: JSON array of checked item IDs.
 *
 * Design decisions:
 * - IDs are validated against the definition on read to guard against schema drift.
 * - Write failures (private browsing, storage quota) are silently ignored
 *   so the in-memory state still works.
 */
export function useSearchChecklist(lostEventId: string): SearchChecklistState {
  const [checkedIds, setCheckedIds] = useState<ReadonlySet<string>>(
    () => readChecked(lostEventId),
  )

  const toggleItem = useCallback(
    (id: string) => {
      setCheckedIds((prev) => {
        const next = new Set(prev)
        if (next.has(id)) {
          next.delete(id)
        } else {
          next.add(id)
        }
        writeChecked(lostEventId, next)
        return next
      })
    },
    [lostEventId],
  )

  const reset = useCallback(() => {
    setCheckedIds(new Set())
    try {
      localStorage.removeItem(storageKey(lostEventId))
    } catch {
      // ignore
    }
  }, [lostEventId])

  const phaseProgress = useMemo<Readonly<Record<ChecklistPhase, PhaseProgress>>>(() => {
    const phases: readonly ChecklistPhase[] = ['2h', '24h', '3d']
    const result = {} as Record<ChecklistPhase, PhaseProgress>
    for (const phase of phases) {
      const phaseItems = CHECKLIST_ITEMS.filter((i) => i.phase === phase)
      const completed = phaseItems.filter((i) => checkedIds.has(i.id)).length
      result[phase] = {
        total: phaseItems.length,
        completed,
        done: completed === phaseItems.length,
      }
    }
    return result
  }, [checkedIds])

  const totalProgress = useMemo<PhaseProgress>(() => {
    const total = CHECKLIST_ITEMS.length
    const completed = CHECKLIST_ITEMS.filter((i) => checkedIds.has(i.id)).length
    return { total, completed, done: completed === total }
  }, [checkedIds])

  return {
    items: CHECKLIST_ITEMS,
    checkedIds,
    toggleItem,
    phaseProgress,
    totalProgress,
    reset,
  }
}
