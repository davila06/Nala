import { useCallback, useState } from 'react'
import { locationsApi } from '../api/locationsApi'
import { useAuthStore } from '@/features/auth/store/authStore'
import type { QuietHoursValue } from '../components/QuietHoursForm'

const PREFS_KEY_ALERTS = 'pawtrack:prefs:receiveNearbyAlerts'
const PREFS_KEY_QUIET  = 'pawtrack:prefs:quietHours'

function readStoredAlerts(): boolean {
  try { return localStorage.getItem(PREFS_KEY_ALERTS) === 'true' } catch { return false }
}

function writeStoredAlerts(value: boolean) {
  try { localStorage.setItem(PREFS_KEY_ALERTS, String(value)) } catch { /* ignore */ }
}

function readStoredQuiet(): QuietHoursValue | null {
  try {
    const raw = localStorage.getItem(PREFS_KEY_QUIET)
    return raw ? (JSON.parse(raw) as QuietHoursValue) : null
  } catch { return null }
}

function writeStoredQuiet(value: QuietHoursValue | null) {
  try {
    if (value) {
      localStorage.setItem(PREFS_KEY_QUIET, JSON.stringify(value))
    } else {
      localStorage.removeItem(PREFS_KEY_QUIET)
    }
  } catch { /* ignore */ }
}

/**
 * Manages the user's geofenced-alert opt-in preference and quiet-hours window.
 * Both are persisted to localStorage so the choice survives page refreshes.
 */
export function useAlertPreference() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const [receiveNearbyAlerts, setReceiveNearbyAlerts] = useState(readStoredAlerts)
  const [quietHours, setQuietHoursState] = useState<QuietHoursValue | null>(readStoredQuiet)
  const [isSaving, setIsSaving] = useState(false)

  // ── Internal helper ─────────────────────────────────────────────────────────

  const pushToApi = useCallback(
    (lat: number, lng: number, enabled: boolean, qh: QuietHoursValue | null) => {
      setIsSaving(true)
      void locationsApi
        .upsertLocation({
          lat,
          lng,
          receiveNearbyAlerts: enabled,
          quietHoursStart: qh?.start ?? null,
          quietHoursEnd:   qh?.end   ?? null,
        })
        .finally(() => setIsSaving(false))
    },
    [],
  )

  // ── Toggle opt-in ───────────────────────────────────────────────────────────

  const toggle = useCallback(async () => {
    if (!isAuthenticated) return

    const next = !receiveNearbyAlerts

    if (next && 'geolocation' in navigator) {
      navigator.geolocation.getCurrentPosition(
        (pos) => pushToApi(pos.coords.latitude, pos.coords.longitude, true, quietHours),
        ()    => pushToApi(0, 0, true, quietHours),
        { enableHighAccuracy: true, timeout: 5_000, maximumAge: 30_000 },
      )
    } else {
      pushToApi(0, 0, false, quietHours)
    }

    setReceiveNearbyAlerts(next)
    writeStoredAlerts(next)
  }, [isAuthenticated, receiveNearbyAlerts, quietHours, pushToApi])

  // ── Update quiet hours ──────────────────────────────────────────────────────

  const setQuietHours = useCallback(
    (next: QuietHoursValue | null) => {
      setQuietHoursState(next)
      writeStoredQuiet(next)
      // Only push to API if alerts are currently enabled
      if (receiveNearbyAlerts && isAuthenticated) {
        pushToApi(0, 0, true, next)
      }
    },
    [receiveNearbyAlerts, isAuthenticated, pushToApi],
  )

  return { receiveNearbyAlerts, toggle, isSaving, quietHours, setQuietHours }
}
