/**
 * Unit tests for useEmergencyMode
 *
 * What is covered:
 *  - Initial state (all steps pending, not running, not finished)
 *  - Happy path: all 4 steps complete successfully
 *  - Step 2 (share) is skipped when navigator.share is unavailable
 *  - Step 2 (share) is skipped when the user cancels the share sheet (AbortError)
 *  - Step 2 (share) is marked error on non-abort share failures
 *  - Step 3 (link) is marked error when clipboard.writeText rejects
 *  - Re-entrant guard: calling run() while already running is a no-op
 *  - reset() restores initial state after a completed run
 *  - scrollIntoView is called on checklistRef.current during step 4
 */
import { act, renderHook, waitFor } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest'
import { useEmergencyMode } from '@/features/lost-pets/hooks/useEmergencyMode'
import type { UseEmergencyModeParams } from '@/features/lost-pets/hooks/useEmergencyMode'
import type { UseGenerateFlyerReturn } from '@/features/lost-pets/hooks/useGenerateFlyer'

// ── jsdom polyfill ────────────────────────────────────────────────────────────
// jsdom does not implement scrollIntoView; we add a no-op implementation to
// silence TypeError and let the scroll spy assertions work correctly.
if (!Element.prototype.scrollIntoView) {
  Element.prototype.scrollIntoView = vi.fn()
}

// ── Helpers ───────────────────────────────────────────────────────────────────

/** Builds a minimal flyerHook mock where every operation succeeds instantly. */
function makeFlyerHook(
  overrides: Partial<
    Pick<UseGenerateFlyerReturn, 'prepareAssets' | 'buildFlyerBlob' | 'assets' | 'state'>
  > = {},
) {
  const assets = { petPhotoDataUrl: null, qrCodeDataUrl: null, recentPhotoDataUrl: null }
  return {
    state: (overrides.state ?? 'ready') as UseGenerateFlyerReturn['state'],
    assets: overrides.assets !== undefined ? overrides.assets : assets,
    prepareAssets:
      overrides.prepareAssets ?? vi.fn().mockResolvedValue(undefined),
    buildFlyerBlob:
      overrides.buildFlyerBlob ??
      vi.fn().mockResolvedValue(new Blob(['png'], { type: 'image/png' })),
  }
}

/** Default hook params factory. */
function makeParams(
  flyerHookOverrides: Parameters<typeof makeFlyerHook>[0] = {},
): UseEmergencyModeParams {
  return {
    petId: 'pet-123',
    petName: 'Luna',
    flyerHook: makeFlyerHook(flyerHookOverrides),
    flyerRef: { current: document.createElement('div') },
    checklistRef: { current: document.createElement('section') },
  }
}

/** Awaits the full emergency-mode sequence, flushing all React state updates. */
async function runAndFinish(
  result: { current: ReturnType<typeof useEmergencyMode> },
) {
  await act(() => result.current.run())
  await waitFor(() => expect(result.current.isFinished).toBe(true), {
    timeout: 5000,
  })
}

// ── Browser API mocks ─────────────────────────────────────────────────────────

const mockShare     = vi.fn()
const mockWriteText = vi.fn()

beforeEach(() => {
  // Attach scrollIntoView spy to every newly created element
  vi.spyOn(Element.prototype, 'scrollIntoView').mockImplementation(() => undefined)

  Object.defineProperty(navigator, 'share', {
    value: mockShare,
    writable: true,
    configurable: true,
  })
  Object.defineProperty(navigator, 'clipboard', {
    value: { writeText: mockWriteText },
    writable: true,
    configurable: true,
  })

  // rAF: call callback synchronously so share-step timing resolves immediately
  vi.spyOn(window, 'requestAnimationFrame').mockImplementation((cb) => {
    cb(performance.now())
    return 0
  })

  mockShare.mockResolvedValue(undefined)
  mockWriteText.mockResolvedValue(undefined)
})

afterEach(() => {
  vi.restoreAllMocks()
})

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('useEmergencyMode', () => {
  // ── initial state ──────────────────────────────────────────────────────────

  it('starts with all steps pending and not running', () => {
    const { result } = renderHook(() => useEmergencyMode(makeParams()))

    expect(result.current.isRunning).toBe(false)
    expect(result.current.isFinished).toBe(false)
    result.current.steps.forEach((s) => expect(s.status).toBe('pending'))
  })

  // ── happy path ─────────────────────────────────────────────────────────────

  it('completes all 4 steps as done when everything succeeds', async () => {
    const { result } = renderHook(() => useEmergencyMode(makeParams()))

    await runAndFinish(result)

    expect(result.current.isFinished).toBe(true)
    expect(result.current.isRunning).toBe(false)

    const byId = Object.fromEntries(
      result.current.steps.map((s) => [s.id, s.status]),
    )
    expect(byId.flyer).toBe('done')
    expect(byId.share).toBe('done')
    expect(byId.link).toBe('done')
    expect(byId.checklist).toBe('done')
  }, 10_000)

  // ── share step skipped when API unavailable ────────────────────────────────

  it('skips share step when navigator.share is not available', async () => {
    Object.defineProperty(navigator, 'share', {
      value: undefined,
      writable: true,
      configurable: true,
    })

    const { result } = renderHook(() => useEmergencyMode(makeParams()))

    await runAndFinish(result)

    const share = result.current.steps.find((s) => s.id === 'share')
    expect(share?.status).toBe('skipped')
    result.current.steps
      .filter((s) => s.id !== 'share')
      .forEach((s) => expect(s.status).toBe('done'))
  }, 10_000)

  // ── share step skipped on AbortError ──────────────────────────────────────

  it('marks share step as skipped when user dismisses the share sheet (AbortError)', async () => {
    mockShare.mockRejectedValueOnce(new DOMException('', 'AbortError'))

    const { result } = renderHook(() => useEmergencyMode(makeParams()))

    await runAndFinish(result)

    const share = result.current.steps.find((s) => s.id === 'share')
    expect(share?.status).toBe('skipped')
  }, 10_000)

  // ── share step error on other failures ────────────────────────────────────

  it('marks share step as error when navigator.share rejects with a non-abort error', async () => {
    mockShare.mockRejectedValueOnce(new Error('network failure'))

    const { result } = renderHook(() => useEmergencyMode(makeParams()))

    await runAndFinish(result)

    const share = result.current.steps.find((s) => s.id === 'share')
    expect(share?.status).toBe('error')
    // Sequence continues despite the error
    const link = result.current.steps.find((s) => s.id === 'link')
    expect(link?.status).toBe('done')
  }, 10_000)

  // ── clipboard error ────────────────────────────────────────────────────────

  it('marks link step as error when clipboard.writeText rejects', async () => {
    mockWriteText.mockRejectedValueOnce(new DOMException('denied'))

    const { result } = renderHook(() => useEmergencyMode(makeParams()))

    await runAndFinish(result)

    const link = result.current.steps.find((s) => s.id === 'link')
    expect(link?.status).toBe('error')
    // Checklist step should still complete
    const checklist = result.current.steps.find((s) => s.id === 'checklist')
    expect(checklist?.status).toBe('done')
  }, 10_000)

  // ── re-entrant guard ───────────────────────────────────────────────────────

  it('ignores a second run() call while already running', async () => {
    const prepareAssets = vi.fn().mockResolvedValue(undefined)
    const params = makeParams({ prepareAssets })
    const { result } = renderHook(() => useEmergencyMode(params))

    // Fire both calls without awaiting the first
    await act(async () => {
      void result.current.run()
      await result.current.run()  // second call should be swallowed
    })

    await waitFor(() => expect(result.current.isFinished).toBe(true), {
      timeout: 5000,
    })

    // prepareAssets should only have been called once
    expect(prepareAssets).toHaveBeenCalledTimes(1)
  }, 10_000)

  // ── reset ──────────────────────────────────────────────────────────────────

  it('reset() restores all steps to pending and clears isFinished', async () => {
    const { result } = renderHook(() => useEmergencyMode(makeParams()))

    await runAndFinish(result)
    expect(result.current.isFinished).toBe(true)

    act(() => result.current.reset())

    expect(result.current.isFinished).toBe(false)
    result.current.steps.forEach((s) => expect(s.status).toBe('pending'))
  }, 10_000)

  // ── scroll side-effect ─────────────────────────────────────────────────────

  it('calls scrollIntoView on checklistRef when checklist step runs', async () => {
    const checklistEl = document.createElement('section')
    const scrollSpy = vi
      .spyOn(checklistEl, 'scrollIntoView')
      .mockImplementation(() => undefined)

    const params: UseEmergencyModeParams = {
      ...makeParams(),
      checklistRef: { current: checklistEl },
    }
    const { result } = renderHook(() => useEmergencyMode(params))

    await runAndFinish(result)

    expect(scrollSpy).toHaveBeenCalledWith({ behavior: 'smooth', block: 'start' })
  }, 10_000)
})


