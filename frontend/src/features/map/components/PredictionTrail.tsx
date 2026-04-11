import { Circle, Polyline } from 'react-leaflet'
import type { MovementPrediction } from '../api/publicMapApi'

interface PredictionTrailProps {
  prediction: MovementPrediction
}

/**
 * Resolves a CSS color string based on the confidence percentage.
 * - ≥ 65 % → green  (high confidence / recent sightings)
 * - 40–64 % → yellow (medium)
 * - < 40 %  → red    (low confidence / stale sightings)
 */
function confidenceColor(confidencePercent: number | null): string {
  if (confidencePercent === null) return '#9c8a78'  // sand-500
  if (confidencePercent >= 65) return '#17a26d'      // rescue-500
  if (confidencePercent >= 40) return '#d4851a'      // warn-600
  return '#d42020'                                   // danger-500
}

/**
 * Renders a pet's movement trail and projected position on the Leaflet map.
 *
 * - Blue dashed **polyline** connecting consecutive sighting points (oldest → newest).
 * - Colour-coded **circle** centred on the projected position, radius = uncertainty zone.
 *
 * Returns null when the prediction has insufficient data or is missing coords.
 */
export function PredictionTrail({ prediction }: PredictionTrailProps) {
  if (!prediction.hasEnoughData) return null
  if (
    prediction.projectedLat === null ||
    prediction.projectedLng === null ||
    prediction.radiusMeters === null
  ) {
    return null
  }

  const trailPositions = prediction.trailPoints.map(
    (p) => [p.lat, p.lng] as [number, number],
  )

  const color = confidenceColor(prediction.confidencePercent)

  return (
    <>
      {/* Movement trail polyline */}
      {trailPositions.length >= 2 && (
        <Polyline
          positions={trailPositions}
          pathOptions={{
            color: '#3056c2',
            weight: 3,
            dashArray: '8 5',
            opacity: 0.85,
          }}
        />
      )}

      {/* Projected uncertainty zone */}
      <Circle
        center={[prediction.projectedLat, prediction.projectedLng]}
        radius={prediction.radiusMeters}
        pathOptions={{
          color,
          fillColor: color,
          fillOpacity: 0.12,
          weight: 2,
          opacity: 0.75,
        }}
      />
    </>
  )
}
