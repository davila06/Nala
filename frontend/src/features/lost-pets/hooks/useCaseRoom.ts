import { useQuery } from '@tanstack/react-query'
import { caseRoomApi } from '../api/caseRoomApi'

export const CASE_ROOM_KEY = (lostEventId: string) =>
  ['case-room', lostEventId] as const

/**
 * Fetches the Case Room aggregate for a given lost-pet event.
 *
 * @param lostEventId - The UUID of the active LostPetEvent.
 * Disabled when the ID is empty (page guard).
 */
export function useCaseRoom(lostEventId: string) {
  return useQuery({
    queryKey: CASE_ROOM_KEY(lostEventId),
    queryFn: () => caseRoomApi.getCaseRoom(lostEventId),
    enabled: lostEventId.length > 0,
    staleTime: 30_000,
    refetchInterval: 60_000, // auto-refresh every minute while page is open
  })
}
