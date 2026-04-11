import { describe, expect, it } from 'vitest'
import { estimateSearchRadius } from '@/features/lost-pets/utils/searchRadius'

describe('estimateSearchRadius', () => {
  it('keeps the short ring during the first two hours', () => {
    expect(estimateSearchRadius('Dog', 'Labrador', 1.9)).toBe(500)
    expect(estimateSearchRadius('Cat', null, 0.5)).toBe(300)
  })

  it('expands to the medium ring between two and six hours', () => {
    expect(estimateSearchRadius('Dog', 'Labrador', 3)).toBe(750)
    expect(estimateSearchRadius('Cat', null, 4)).toBe(375)
  })

  it('expands to the extended ring after twenty-four hours', () => {
    expect(estimateSearchRadius('Dog', 'Labrador', 25)).toBe(1500)
    expect(estimateSearchRadius('Rabbit', null, 30)).toBe(100)
  })
})