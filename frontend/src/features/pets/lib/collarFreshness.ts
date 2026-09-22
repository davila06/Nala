/**
 * A GPS fix is considered stale once its age (in seconds, from the device/provider
 * timestamp) exceeds the collar's own offline threshold — the same boundary the
 * backend uses to decide when to alert the owner that the collar stopped reporting.
 */
export function isPositionStale(positionAgeSeconds: number | null, offlineThresholdMinutes: number): boolean {
  if (positionAgeSeconds === null) return false;
  return positionAgeSeconds > offlineThresholdMinutes * 60;
}
