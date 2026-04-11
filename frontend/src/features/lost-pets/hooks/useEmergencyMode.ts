import { useCallback, useRef, useState } from 'react'
import type { UseGenerateFlyerReturn } from './useGenerateFlyer'

// ── Types ──────────────────────────────────────────────────────────────────────

export type EmergencyStepId = 'flyer' | 'share' | 'link' | 'checklist'

export type EmergencyStepStatus = 'pending' | 'running' | 'done' | 'skipped' | 'error'

export interface EmergencyStep {
  readonly id: EmergencyStepId
  readonly label: string
  readonly description: string
  readonly status: EmergencyStepStatus
}

export interface UseEmergencyModeReturn {
  readonly steps: readonly EmergencyStep[]
  readonly isRunning: boolean
  readonly isFinished: boolean
  run: () => Promise<void>
  reset: () => void
}

export interface UseEmergencyModeParams {
  petId: string
  petName: string
  /**
   * Subset of the useGenerateFlyer return value required by the emergency
   * mode orchestrator.  Passing the full hook return value is also valid.
   */
  flyerHook: Pick<
    UseGenerateFlyerReturn,
    'prepareAssets' | 'buildFlyerBlob' | 'assets' | 'state'
  >
  flyerRef: React.RefObject<HTMLDivElement | null>
  checklistRef: React.RefObject<HTMLElement | null>
}

// ── Constants ─────────────────────────────────────────────────────────────────

/** How long to poll for flyer assets before giving up (ms). */
const ASSET_WAIT_TIMEOUT_MS = 10_000

/** Polling interval while waiting for concurrent asset fetch (ms). */
const ASSET_POLL_INTERVAL_MS = 100

/** Delay between step 3 and the checklist scroll so the user sees step 3 complete. */
const PRE_SCROLL_DELAY_MS = 300

// ── Step definitions ──────────────────────────────────────────────────────────

interface StepDef {
  readonly id: EmergencyStepId
  readonly label: string
  readonly description: string
}

const STEP_DEFS: readonly StepDef[] = [
  {
    id: 'flyer',
    label: 'Flyer de búsqueda',
    description: 'Generando imagen lista para imprimir y compartir',
  },
  {
    id: 'share',
    label: 'Compartir por app',
    description: 'Enviando flyer por WhatsApp u otra aplicación',
  },
  {
    id: 'link',
    label: 'Copiar enlace del caso',
    description: 'Copiando el link del caso al portapapeles',
  },
  {
    id: 'checklist',
    label: 'Guía de primeros pasos',
    description: 'Desplazando a las acciones urgentes del caso',
  },
] as const

function makeInitialSteps(): EmergencyStep[] {
  return STEP_DEFS.map((def) => ({ ...def, status: 'pending' as const }))
}

// ── Hook ──────────────────────────────────────────────────────────────────────

/**
 * Orchestrates the "Modo Emergencia" 4-step sequence on the lost-pet
 * confirmation page:
 *
 *   1. **flyer**     — pre-fetches pet photo + QR as data URLs (prepareAssets).
 *   2. **share**     — invokes navigator.share with the rendered flyer PNG.
 *                       Skipped on desktop / older browsers that lack the API.
 *   3. **link**      — writes the public case URL to the clipboard.
 *   4. **checklist** — smoothly scrolls the checklist element into view.
 *
 * Each step transitions through: pending → running → done | skipped | error.
 *
 * ## Stale-closure safety
 * The hook stores `flyerHook` in a mutable ref that is updated on every render.
 * This lets the async `run()` callback always read the latest `assets` and
 * `state` values without listing them as dependencies (which would require
 * re-creating the callback on every asset-related render).
 */
export function useEmergencyMode({
  petId,
  petName,
  flyerHook,
  flyerRef,
  checklistRef,
}: UseEmergencyModeParams): UseEmergencyModeReturn {
  const [steps, setSteps] = useState<EmergencyStep[]>(makeInitialSteps)
  const [isRunning, setIsRunning] = useState(false)
  const [isFinished, setIsFinished] = useState(false)

  // ── Stale-closure guard ───────────────────────────────────────────────────
  // Always reflects the latest flyerHook object inside the async `run` callback.
  const flyerHookRef = useRef(flyerHook)
  flyerHookRef.current = flyerHook

  // Prevents re-entrant calls while a sequence is already in flight.
  const runningRef = useRef(false)

  // ── Step state helper ─────────────────────────────────────────────────────

  const setStepStatus = useCallback(
    (id: EmergencyStepId, status: EmergencyStepStatus) => {
      setSteps((prev) =>
        prev.map((s): EmergencyStep => (s.id === id ? { ...s, status } : s)),
      )
    },
    [],
  )

  // ── Main sequence ─────────────────────────────────────────────────────────

  const run = useCallback(async () => {
    if (runningRef.current) return
    runningRef.current = true
    setIsRunning(true)
    setIsFinished(false)
    setSteps(makeInitialSteps())

    // ── Step 1: Prepare flyer assets ────────────────────────────────────────
    setStepStatus('flyer', 'running')
    try {
      // If there is already a concurrent fetch in progress, prepareAssets()
      // returns immediately.  We poll the shared `assets` ref until it resolves.
      await flyerHookRef.current.prepareAssets()

      if (flyerHookRef.current.assets === null) {
        const deadline = Date.now() + ASSET_WAIT_TIMEOUT_MS
        while (
          flyerHookRef.current.assets === null &&
          flyerHookRef.current.state === 'loading' &&
          Date.now() < deadline
        ) {
          await new Promise<void>((resolve) =>
            setTimeout(resolve, ASSET_POLL_INTERVAL_MS),
          )
        }
      }

      setStepStatus('flyer', flyerHookRef.current.assets !== null ? 'done' : 'error')
    } catch {
      setStepStatus('flyer', 'error')
    }

    // ── Step 2: Share flyer via Web Share API ───────────────────────────────
    const canNativeShare =
      typeof navigator !== 'undefined' &&
      typeof navigator.share === 'function' &&
      flyerRef.current !== null

    if (canNativeShare) {
      setStepStatus('share', 'running')
      try {
        // Allow two rAF cycles for the DOM to reflect updated asset data URLs
        // before html2canvas captures the flyer element.
        await new Promise<void>((resolve) => {
          requestAnimationFrame(() =>
            requestAnimationFrame(() => setTimeout(resolve, 150)),
          )
        })

        const blob = await flyerHookRef.current.buildFlyerBlob(flyerRef)
        const safeName = petName.toLowerCase().replace(/\s+/g, '-')
        const file = new File([blob], `flyer-${safeName}.png`, {
          type: 'image/png',
        })

        await navigator.share({
          title: `¡Ayuda a encontrar a ${petName}!`,
          text: `${petName} está perdido. Si lo ves, escanea su QR o contacta al dueño.`,
          url: `${window.location.origin}/p/${petId}`,
          files: [file],
        })

        setStepStatus('share', 'done')
      } catch (err) {
        // AbortError = user dismissed the share sheet; treat as skipped, not error.
        const isAbort = err instanceof DOMException && err.name === 'AbortError'
        setStepStatus('share', isAbort ? 'skipped' : 'error')
      }
    } else {
      // Native share not available (desktop, Firefox, etc.)
      setStepStatus('share', 'skipped')
    }

    // ── Step 3: Copy public case URL to clipboard ───────────────────────────
    setStepStatus('link', 'running')
    const caseUrl = `${window.location.origin}/p/${petId}`
    try {
      await navigator.clipboard.writeText(caseUrl)
      setStepStatus('link', 'done')
    } catch {
      // Clipboard API may fail in insecure contexts or when permission is denied.
      setStepStatus('link', 'error')
    }

    // ── Step 4: Scroll checklist into view ──────────────────────────────────
    setStepStatus('checklist', 'running')
    // Brief pause so the user sees step 3 complete before the page scrolls.
    await new Promise<void>((resolve) => setTimeout(resolve, PRE_SCROLL_DELAY_MS))
    checklistRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
    setStepStatus('checklist', 'done')

    runningRef.current = false
    setIsRunning(false)
    setIsFinished(true)
  }, [petId, petName, flyerRef, checklistRef, setStepStatus])

  // ── Reset ─────────────────────────────────────────────────────────────────

  const reset = useCallback(() => {
    if (runningRef.current) return
    setSteps(makeInitialSteps())
    setIsFinished(false)
  }, [])

  return { steps, isRunning, isFinished, run, reset }
}
