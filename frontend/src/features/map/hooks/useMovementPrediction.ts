import { useQueries } from "@tanstack/react-query";
import { useMemo } from "react";
import { publicMapApi, type MovementPrediction } from "../api/publicMapApi";

const STALE_TIME = 5 * 60 * 1000; // 5 minutes
/** Max simultaneous movement-prediction queries. Prevents N×40 parallel requests when many pets are on screen. */
const MAX_PREDICTIONS_IN_FLIGHT = 10;

/**
 * Fetches movement predictions for every provided lost-pet event ID in
 * parallel.  Results are served from the React Query cache after the first
 * successful fetch (staleTime 5 min) so panning the map never re-triggers
 * the network calls within that window.
 *
 * @returns A stable `Record<lostPetEventId, MovementPrediction>` containing
 *          only the IDs whose queries have succeeded.
 */
export function useMovementPredictions(
  lostPetEventIds: string[],
): Record<string, MovementPrediction> {
  // Cap to the most-recent N pets visible on screen to avoid request storms.
  const cappedIds = lostPetEventIds.slice(0, MAX_PREDICTIONS_IN_FLIGHT);

  const results = useQueries({
    queries: cappedIds.map((id) => ({
      queryKey: ["movement-prediction", id] as const,
      queryFn: () => publicMapApi.getMovementPrediction(id),
      staleTime: STALE_TIME,
      refetchOnWindowFocus: false,
      // Silently ignore individual prediction failures — the map keeps working.
      throwOnError: false,
    })),
  });

  return useMemo(() => {
    const map: Record<string, MovementPrediction> = {};
    cappedIds.forEach((id, i) => {
      const data = results[i]?.data;
      if (data !== undefined) map[id] = data;
    });
    return map;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [results, cappedIds.join(",")]);
}
