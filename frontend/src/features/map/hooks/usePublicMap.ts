import { useQuery } from '@tanstack/react-query'
import { useMemo, useCallback, useRef } from 'react'
import { publicMapApi, type MapBBox } from '../api/publicMapApi'

const MAX_BBOX_SPAN_DEGREES = 5

function clamp(value: number, min: number, max: number) {
  return Math.min(max, Math.max(min, value))
}

export function sanitizeMapBBox(bbox: MapBBox): MapBBox {
  let north = clamp(bbox.north, -90, 90)
  let south = clamp(bbox.south, -90, 90)
  let east = clamp(bbox.east, -180, 180)
  let west = clamp(bbox.west, -180, 180)

  if (north < south) [north, south] = [south, north]
  if (east < west) [east, west] = [west, east]

  const latSpan = north - south
  if (latSpan > MAX_BBOX_SPAN_DEGREES) {
    const center = (north + south) / 2
    north = clamp(center + MAX_BBOX_SPAN_DEGREES / 2, -90, 90)
    south = clamp(center - MAX_BBOX_SPAN_DEGREES / 2, -90, 90)
  }

  const lngSpan = east - west
  if (lngSpan > MAX_BBOX_SPAN_DEGREES) {
    const center = (east + west) / 2
    east = clamp(center + MAX_BBOX_SPAN_DEGREES / 2, -180, 180)
    west = clamp(center - MAX_BBOX_SPAN_DEGREES / 2, -180, 180)
  }

  return { north, south, east, west }
}

export function usePublicMapEvents(bbox: MapBBox | null) {
  const safeBBox = bbox ? sanitizeMapBBox(bbox) : null

  return useQuery({
    queryKey: ['public-map', safeBBox],
    queryFn: () => publicMapApi.getMapEvents(safeBBox!),
    enabled: safeBBox !== null,
    staleTime: 30_000,
    refetchOnWindowFocus: false,
  })
}

/**
 * Returns a debounced bbox setter (500 ms) so the map query only fires
 * after the user stops panning/zooming — avoids hammering the API on every
 * drag event, per vercel-react-best-practices `client-passive-event-listeners`.
 */
export function useDebouncedBBox(delay = 500) {
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  const debounce = useCallback(
    (setter: (bbox: MapBBox) => void, bbox: MapBBox) => {
      if (timerRef.current) clearTimeout(timerRef.current)
      timerRef.current = setTimeout(() => setter(bbox), delay)
    },
    [delay],
  )

  return useMemo(() => ({ debounce }), [debounce])
}
