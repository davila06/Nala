import { useQuery } from "@tanstack/react-query";

// GADM 4.1 — downloaded locally to avoid LFS/CORS issues with remote sources
const GEOJSON_URL = "/geojson/cantons-cr.geojson";

export interface CantonStat {
  canton: string;
  totalReports: number;
  recoveredCount: number;
  recoveryRate: number;
}

// Normalize a canton name for fuzzy matching:
// lower-case, remove accents, collapse spaces.
function normalize(name: string): string {
  return name
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/\s+/g, " ")
    .trim();
}

export interface CantonFeatureProperties {
  shapeName: string;
  /** Injected after join — undefined when no data in stats */
  stat?: CantonStat;
}

async function fetchCantonGeoJson(): Promise<
  GeoJSON.FeatureCollection<GeoJSON.Geometry, CantonFeatureProperties>
> {
  const res = await fetch(GEOJSON_URL);
  if (!res.ok) throw new Error(`GeoJSON fetch failed: HTTP ${res.status}`);
  const raw = (await res.json()) as {
    type: string;
    features: Array<{
      type: string;
      geometry: GeoJSON.Geometry;
      properties: Record<string, unknown>;
    }>;
  };
  // Normalise GADM property NAME_2 → shapeName expected by the map component
  return {
    type: "FeatureCollection",
    features: raw.features.map((f) => ({
      ...f,
      properties: { shapeName: f.properties["NAME_2"] as string },
    })),
  };
}

/**
 * Fetches and caches the CR canton boundary GeoJSON, then joins it with the
 * provided `cantonStats` array by normalized name matching.
 * The returned FeatureCollection has `properties.stat` populated on matched features.
 */
export function useCantonChoropleth(cantonStats: CantonStat[] | undefined) {
  const {
    data: geoJson,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["canton-geojson"],
    queryFn: fetchCantonGeoJson,
    staleTime: Infinity,
    gcTime: Infinity,
    retry: 1,
  });

  if (!geoJson || !cantonStats)
    return { geoJson: null, isLoading, isError: false };

  // Build normalized lookup map from stats array
  const statsByName = new Map<string, CantonStat>();
  for (const stat of cantonStats) {
    statsByName.set(normalize(stat.canton), stat);
  }

  // Enrich each feature with the matched stat (mutate properties in place on
  // a shallow-cloned collection so we don't mutate the cached original)
  const enriched: GeoJSON.FeatureCollection<
    GeoJSON.Geometry,
    CantonFeatureProperties
  > = {
    ...geoJson,
    features: geoJson.features.map((f) => {
      const key = normalize(f.properties?.shapeName ?? "");
      const stat = statsByName.get(key);
      return { ...f, properties: { ...f.properties, stat } };
    }),
  };

  return { geoJson: enriched, isLoading, isError };
}
