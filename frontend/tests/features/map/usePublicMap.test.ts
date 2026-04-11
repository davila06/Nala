import { describe, expect, it } from 'vitest'
import { sanitizeMapBBox } from '@/features/map/hooks/usePublicMap'

describe('sanitizeMapBBox', () => {
  it('caps bbox span to 5 degrees on both axes', () => {
    const safe = sanitizeMapBBox({
      north: 11.630715737981498,
      south: 7.852498637813029,
      east: -79.55749511718751,
      west: -87.95104980468751,
    })

    expect(safe.north - safe.south).toBeLessThanOrEqual(5)
    expect(safe.east - safe.west).toBeLessThanOrEqual(5)
    expect(safe.north).toBeGreaterThanOrEqual(safe.south)
    expect(safe.east).toBeGreaterThanOrEqual(safe.west)
  })

  it('clamps lat/lng to valid WGS-84 ranges', () => {
    const safe = sanitizeMapBBox({
      north: 120,
      south: -120,
      east: 240,
      west: -240,
    })

    expect(safe.north).toBeLessThanOrEqual(90)
    expect(safe.south).toBeGreaterThanOrEqual(-90)
    expect(safe.east).toBeLessThanOrEqual(180)
    expect(safe.west).toBeGreaterThanOrEqual(-180)
  })
})
