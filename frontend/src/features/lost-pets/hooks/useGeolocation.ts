import { useCallback, useEffect, useRef, useState } from 'react'

export type GeolocationStatus = 'idle' | 'requesting' | 'granted' | 'denied' | 'unavailable'

export interface GeolocationState {
  status: GeolocationStatus
  coords: { lat: number; lng: number } | null
  error: string | null
}

const GEOLOCATION_OPTIONS: PositionOptions = {
  enableHighAccuracy: true,
  timeout: 8_000,
  maximumAge: 30_000,
}

/**
 * Hook that requests the browser geolocation once on demand.
 * Returns the current state and a `request()` function to trigger the prompt.
 */
export function useGeolocation(): GeolocationState & { request: () => void } {
  const [state, setState] = useState<GeolocationState>({
    status: typeof navigator !== 'undefined' && !navigator.geolocation
      ? 'unavailable'
      : 'idle',
    coords: null,
    error: null,
  })

  // Keep a ref to avoid re-creating effects on every render
  const watchIdRef = useRef<number | null>(null)

  const clearWatch = useCallback(() => {
    if (watchIdRef.current !== null) {
      navigator.geolocation.clearWatch(watchIdRef.current)
      watchIdRef.current = null
    }
  }, [])

  const request = useCallback(() => {
    if (!navigator.geolocation) {
      setState({ status: 'unavailable', coords: null, error: 'Geolocalización no disponible en este dispositivo.' })
      return
    }

    setState((prev) => ({ ...prev, status: 'requesting', error: null }))

    // Use watchPosition for mobile (keeps updating as user moves)
    watchIdRef.current = navigator.geolocation.watchPosition(
      (position) => {
        setState({
          status: 'granted',
          coords: {
            lat: position.coords.latitude,
            lng: position.coords.longitude,
          },
          error: null,
        })
      },
      (err) => {
        const message =
          err.code === err.PERMISSION_DENIED
            ? 'Permiso de ubicación denegado. Puedes continuar sin él.'
            : err.code === err.TIMEOUT
              ? 'Tiempo de espera agotado. Puedes continuar sin ubicación.'
              : 'No se pudo obtener tu ubicación.'

        setState({ status: 'denied', coords: null, error: message })
        clearWatch()
      },
      GEOLOCATION_OPTIONS,
    )
  }, [clearWatch])

  // Clean up the watch when the component unmounts
  useEffect(() => clearWatch, [clearWatch])

  return { ...state, request }
}
