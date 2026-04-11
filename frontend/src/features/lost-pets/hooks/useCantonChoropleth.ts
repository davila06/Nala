import { useQuery } from '@tanstack/react-query'

const GEOJSON_URL =
  'https://raw.githubusercontent.com/wmgeolab/geoBoundaries/9469f09/releaseData/gbOpen/CRI/ADM2/geoBoundaries-CRI-ADM2_simplified.geojson'

export interface CantonStat {
  canton: string
  totalReports: number
  recoveredCount: number
  recoveryRate: number
}

// Normalize a canton name for fuzzy matching:
// lower-case, remove accents, collapse spaces.
function normalize(name: string): string {
  return name
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/\s+/g, ' ')
    .trim()
}

export interface CantonFeatureProperties {
  shapeName: string
  /** Injected after join — undefined when no data in stats */
  stat?: CantonStat
}

async function fetchCantonGeoJson(): Promise<GeoJSON.FeatureCollection<GeoJSON.Geometry, CantonFeatureProperties>> {
  const res = await fetch(GEOJSON_URL)
  if (!res.ok) throw new Error(`HTTP ${res.status} fetching canton GeoJSON`)
  return res.json() as Promise<GeoJSON.FeatureCollection<GeoJSON.Geometry, CantonFeatureProperties>>
}

/**
 * Fetches and caches the CR canton boundary GeoJSON, then joins it with the
 * provided `cantonStats` array by normalized name matching.
 * The returned FeatureCollection has `properties.stat` populated on matched features.
 */
export function useCantonChoropleth(cantonStats: CantonStat[] | undefined) {
  const { data: geoJson, isLoading, isError } = useQuery({
    queryKey: ['canton-geojson'],
    queryFn: fetchCantonGeoJson,
    staleTime: 24 * 60 * 60 * 1000, // 24 h — static public dataset
    gcTime: 48 * 60 * 60 * 1000,
    retry: 2,
  })

  if (!geoJson || !cantonStats) return { geoJson: null, isLoading, isError }

  // Build normalized lookup map from stats array
  const statsByName = new Map<string, CantonStat>()
  for (const stat of cantonStats) {
    statsByName.set(normalize(stat.canton), stat)
  }

  // Enrich each feature with the matched stat (mutate properties in place on
  // a shallow-cloned collection so we don't mutate the cached original)
  const enriched: GeoJSON.FeatureCollection<GeoJSON.Geometry, CantonFeatureProperties> = {
    ...geoJson,
    features: geoJson.features.map((f) => {
      const key = normalize(f.properties?.shapeName ?? '')
      const stat = statsByName.get(key)
      return { ...f, properties: { ...f.properties, stat } }
    }),
  }

  return { geoJson: enriched, isLoading, isError }
}
