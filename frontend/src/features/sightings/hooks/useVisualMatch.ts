import { useMutation, useQuery } from '@tanstack/react-query'
import { matchingApi, type VisualMatchPayload } from '../api/matchingApi'

/**
 * Mutation hook for the visual photo-matching endpoint.
 * Uploads a probe photo (+ optional lat/lng) and returns up-to-35 matching
 * lost-pet profiles ranked by visual similarity + geo proximity.
 */
export function useVisualMatch() {
  return useMutation({
    mutationFn: (payload: VisualMatchPayload) => matchingApi.visualMatch(payload),
  })
}

/**
 * Query hook for auto-matching a sighting whose photo is already stored.
 * Enabled only when `sightingId` is non-null.
 */
export function useVisualMatchBySighting(
  sightingId: string | null,
  lat?: number,
  lng?: number,
) {
  return useQuery({
    queryKey: ['visual-match-sighting', sightingId, lat, lng],
    queryFn: () => matchingApi.visualMatchBySightingId(sightingId!, lat, lng),
    enabled: sightingId != null,
    staleTime: 5 * 60 * 1_000,
    retry: 1,
  })
}
