/**
 * useHaptic — thin wrapper around navigator.vibrate for tactile feedback.
 * Safe on browsers that don't support it (iOS Safari, desktop).
 *
 * Usage:
 *   const { tap, warning, success } = useHaptic()
 *   <button onClick={() => { tap(); doAction() }}>
 */
export function useHaptic() {
  const canVibrate = typeof navigator !== 'undefined' && 'vibrate' in navigator

  const vibrate = (pattern: number | number[]) => {
    if (canVibrate) {
      try { navigator.vibrate(pattern) } catch { /* ignore */ }
    }
  }

  return {
    /** Short single tap — general confirmation */
    tap:     () => vibrate(40),
    /** Double tap — toggle, selection */
    doubleTap: () => vibrate([30, 60, 30]),
    /** Warning pulse — destructive / critical action */
    warning: () => vibrate([80, 40, 80]),
    /** Success burst — celebration */
    success: () => vibrate([60, 30, 60, 30, 120]),
    /** Error shake */
    error:   () => vibrate([120, 60, 120]),
  }
}
