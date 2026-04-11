import type { PetSpecies } from '@/features/pets/api/petsApi'

// ── Dog breed classification ───────────────────────────────────────────────────
// Maps common breed names (ES + EN) to size/energy categories.

/** High-energy, large or medium breeds — widest search radius. */
const ACTIVE_DOG_KEYWORDS = [
  'labrador', 'golden retriever', 'golden', 'husky', 'siberian',
  'border collie', 'collie', 'australian shepherd', 'pastor australiano',
  'german shepherd', 'pastor alemán', 'pastor aleman', 'malinois',
  'doberman', 'dobermann', 'rottweiler', 'weimaraner', 'vizsla',
  'pointer', 'setter', 'dalmatian', 'dálmata', 'dalmata',
  'boxer', 'pit bull', 'pitbull', 'american staffordshire', 'stafford',
  'greyhound', 'galgo', 'whippet', 'beagle', 'jack russell',
  'springer spaniel', 'cocker spaniel',
]

/** Small or low-energy toy breeds — narrowest search radius. */
const SMALL_DOG_KEYWORDS = [
  'chihuahua', 'yorkshire', 'yorkie', 'maltese', 'maltés', 'maltes',
  'pomeranian', 'pomerania', 'pomeranio',
  'toy poodle', 'poodle miniatura', 'poodle toy',
  'shih tzu', 'shih-tzu', 'shihtzu',
  'cavalier', 'bichon', 'frisé', 'frise',
  'papillon', 'affenpinscher', 'brussels griffon',
  'miniature dachshund', 'salchicha miniatura',
  'chipin', 'pomchi', 'maltipoo',
]

type DogCategory = 'active' | 'small' | 'medium'

/**
 * Classifies a dog breed into energy/size categories using keyword matching
 * against a normalised (lowercase) breed string. Falls back to 'medium' when
 * the breed is unknown or not provided.
 */
function classifyDog(breed: string | null): DogCategory {
  if (!breed) return 'medium'
  const normalized = breed.toLowerCase().trim()

  for (const keyword of ACTIVE_DOG_KEYWORDS) {
    if (normalized.includes(keyword)) return 'active'
  }
  for (const keyword of SMALL_DOG_KEYWORDS) {
    if (normalized.includes(keyword)) return 'small'
  }
  return 'medium'
}

// ── Radius matrix ──────────────────────────────────────────────────────────────
//
// Each entry is [0–2 h, 2–6 h, 6–24 h, 24 h+] in metres.
// The first ring preserves the current short-radius baseline and later rings
// expand progressively according to the mobility profile of the species/breed.

type RadiusQuadruple = [number, number, number, number]

const RADIUS_MATRIX: Record<string, RadiusQuadruple> = {
  'dog-active': [500, 750, 1000, 1500],
  'dog-medium': [350, 525, 700, 875],
  'dog-small': [200, 250, 320, 400],
  cat: [300, 375, 480, 600],
  rabbit: [50, 60, 75, 100],
  bird: [500, 750, 1000, 1500],
  other: [300, 375, 450, 600],
}

type TimesBracket = 0 | 1 | 2 | 3

function getTimeBracket(hoursElapsed: number): TimesBracket {
  if (hoursElapsed < 2) return 0
  if (hoursElapsed < 6) return 1
  if (hoursElapsed < 24) return 2
  return 3
}

// ── Public API ─────────────────────────────────────────────────────────────────

/**
 * Estimates the search radius in **metres** for a lost pet based on species,
 * breed, and time elapsed since the last sighting.
 *
 * The function is pure and runs in O(n) where n is the keyword list size (~50).
 * Safe to call on every render without memoisation.
 *
 * @param species - Pet species ('Dog' | 'Cat' | 'Rabbit' | 'Bird' | 'Other').
 * @param breed   - Optional breed string (may be null/undefined). Case-insensitive.
 * @param hoursElapsed - Hours since the last known sighting. Must be >= 0.
 * @returns Radius in metres.
 */
export function estimateSearchRadius(
  species: PetSpecies,
  breed: string | null,
  hoursElapsed: number,
): number {
  const bracket = getTimeBracket(Math.max(0, hoursElapsed))

  let key: string
  switch (species) {
    case 'Dog':    key = `dog-${classifyDog(breed)}`; break
    case 'Cat':    key = 'cat';                        break
    case 'Rabbit': key = 'rabbit';                     break
    case 'Bird':   key = 'bird';                       break
    default:       key = 'other'
  }

  return RADIUS_MATRIX[key][bracket]
}

/**
 * Replaces the heuristic radius with local empirical p90 when available.
 * Falls back to the heuristic value when stats are missing or invalid.
 */
export function resolveSearchRadiusWithLocalStats(
  fallbackRadiusMetres: number,
  p90DistanceMeters: number | null | undefined,
): number {
  if (p90DistanceMeters == null || Number.isNaN(p90DistanceMeters) || p90DistanceMeters <= 0) {
    return fallbackRadiusMetres
  }

  return Math.round(p90DistanceMeters)
}

/**
 * Formats a radius in metres as a human-readable string.
 *
 * @example
 * formatRadius(200)  // → "200 m"
 * formatRadius(2000) // → "2 km"
 * formatRadius(1500) // → "1.5 km"
 */
export function formatRadius(metres: number): string {
  if (metres < 1000) return `${metres} m`
  const km = metres / 1000
  return `${km % 1 === 0 ? km.toFixed(0) : km.toFixed(1)} km`
}

/**
 * Computes the hours elapsed between a given ISO timestamp and now.
 * Returns 0 if the timestamp is in the future.
 */
export function hoursElapsedSince(isoTimestamp: string): number {
  const elapsed = (Date.now() - new Date(isoTimestamp).getTime()) / 3_600_000
  return Math.max(0, elapsed)
}
