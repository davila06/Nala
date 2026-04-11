import { useCallback, useEffect, useRef } from 'react'
import { locationsApi } from '../api/locationsApi'
import { useAuthStore } from '@/features/auth/store/authStore'

/**
 * When the user is authenticated and has opted in to nearby alerts,
 * this hook watches the device position and pushes updates to the backend
 * every `syncIntervalMs` milliseconds (default: 5 minutes).
 *
 * The hook is self-contained and safe to mount at the app root.
 */
export function useTrackLocation({
  receiveNearbyAlerts,
  syncIntervalMs = 5 * 60 * 1_000, // 5 minutes
}: {
  receiveNearbyAlerts: boolean
  syncIntervalMs?: number
}) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const watchIdRef = useRef<number | null>(null)
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)
  const pendingCoordsRef = useRef<{ lat: number; lng: number } | null>(null)

  const send = useCallback(
    (lat: number, lng: number) => {
      void locationsApi.upsertLocation({ lat, lng, receiveNearbyAlerts })
    },
    [receiveNearbyAlerts],
  )

  useEffect(() => {
    // Only track when authenticated AND opted in.
    if (!isAuthenticated || !receiveNearbyAlerts) return

    if (!('geolocation' in navigator)) return

    // Start watching position.
    watchIdRef.current = navigator.geolocation.watchPosition(
      (position) => {
        pendingCoordsRef.current = {
          lat: position.coords.latitude,
          lng: position.coords.longitude,
        }
      },
      () => {
        // Silently ignore geolocation errors — the app still works without tracking.
      },
      { enableHighAccuracy: true, maximumAge: 60_000 },
    )

    // Batch-send at most once per `syncIntervalMs` to avoid API flooding.
    timerRef.current = setInterval(() => {
      const coords = pendingCoordsRef.current
      if (coords) {
        send(coords.lat, coords.lng)
        pendingCoordsRef.current = null
      }
    }, syncIntervalMs)

    return () => {
      if (watchIdRef.current !== null) {
        navigator.geolocation.clearWatch(watchIdRef.current)
        watchIdRef.current = null
      }
      if (timerRef.current !== null) {
        clearInterval(timerRef.current)
        timerRef.current = null
      }
    }
  }, [isAuthenticated, receiveNearbyAlerts, send, syncIntervalMs])
}
