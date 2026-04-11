import 'leaflet/dist/leaflet.css'
import L from 'leaflet'
import { useEffect, useRef } from 'react'
import type { CantonStat } from '../hooks/useCantonChoropleth'
import { useCantonChoropleth } from '../hooks/useCantonChoropleth'

// ── Color scale ───────────────────────────────────────────────────────────────

function rateToFill(rate: number, maxRate: number): string {
  const intensity = maxRate > 0 ? rate / maxRate : 0
  const alpha = Math.max(0.12, Math.min(1, intensity))
  // Emerald-600 (#059669) with variable alpha
  return `rgba(5, 150, 105, ${alpha.toFixed(2)})`
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function toPercent(value: number): string {
  return `${(value * 100).toFixed(0)}%`
}

// ── Props ─────────────────────────────────────────────────────────────────────

interface CantonChoroplethMapProps {
  cantonRecovery: CantonStat[]
  maxRecoveryRate: number
}

// ── Component ─────────────────────────────────────────────────────────────────

export function CantonChoroplethMap({ cantonRecovery, maxRecoveryRate }: CantonChoroplethMapProps) {
  const { geoJson, isLoading, isError } = useCantonChoropleth(cantonRecovery)
  const containerRef = useRef<HTMLDivElement>(null)
  const mapRef = useRef<L.Map | null>(null)
  const layerRef = useRef<L.GeoJSON | null>(null)

  // Initialise the Leaflet map once the container is mounted.
  useEffect(() => {
    if (!containerRef.current || mapRef.current) return

    const map = L.map(containerRef.current, {
      center: [9.7489, -83.7534], // Geographic center of Costa Rica
      zoom: 7,
      zoomControl: true,
      attributionControl: true,
    })

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
      maxZoom: 18,
    }).addTo(map)

    mapRef.current = map

    return () => {
      map.remove()
      mapRef.current = null
    }
  }, [])

  // Add / refresh the GeoJSON layer whenever the data changes.
  useEffect(() => {
    const map = mapRef.current
    if (!map || !geoJson) return

    // Remove old layer
    if (layerRef.current) {
      layerRef.current.remove()
      layerRef.current = null
    }

    const layer = L.geoJSON(geoJson as GeoJSON.GeoJsonObject, {
      style: (feature) => {
        const stat = feature?.properties?.stat as CantonStat | undefined
        return {
          fillColor: stat ? rateToFill(stat.recoveryRate, maxRecoveryRate) : 'rgba(226,232,240,0.5)',
          fillOpacity: 1,
          color: '#475569',
          weight: 0.6,
          opacity: 0.6,
        }
      },
      onEachFeature: (feature, featureLayer) => {
        const name: string = feature.properties?.shapeName ?? 'Cantón'
        const stat = feature.properties?.stat as CantonStat | undefined

        const tooltipContent = stat
          ? `<strong>${name}</strong><br/>
             Tasa: <b>${toPercent(stat.recoveryRate)}</b><br/>
             Reunidos: ${stat.recoveredCount} / ${stat.totalReports}`
          : `<strong>${name}</strong><br/><em>Sin datos</em>`

        featureLayer.bindTooltip(tooltipContent, {
          direction: 'top',
          sticky: true,
          opacity: 0.95,
          className: 'pawtrack-canton-tooltip',
        })

        featureLayer.on({
          mouseover(e) {
            const target = e.target as L.Path
            target.setStyle({ weight: 2, color: '#0f172a', opacity: 0.85 })
            target.bringToFront()
          },
          mouseout(e) {
            layer.resetStyle(e.target as L.Layer)
          },
        })
      },
    })

    layer.addTo(map)
    layerRef.current = layer

    // Fit map to canton bounds for a tight, country-level view
    const bounds = layer.getBounds()
    if (bounds.isValid()) {
      map.fitBounds(bounds, { padding: [16, 16] })
    }
  }, [geoJson, maxRecoveryRate])

  if (isError) {
    return (
      <div
        role="alert"
        style={{
          height: 120,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          background: '#fef2f2',
          border: '1px solid #fecaca',
          borderRadius: 12,
          color: '#b91c1c',
          fontSize: '0.9rem',
        }}
      >
        No se pudo cargar el mapa de cantones. Intente recargando la página.
      </div>
    )
  }

  return (
    <div style={{ position: 'relative' }}>
      {isLoading && (
        <div
          aria-live="polite"
          aria-label="Cargando mapa de cantones"
          style={{
            position: 'absolute',
            inset: 0,
            zIndex: 1000,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            background: 'rgba(248,250,252,0.8)',
            borderRadius: 12,
            pointerEvents: 'none',
          }}
        >
          <span style={{ fontSize: '0.9rem', color: '#64748b' }}>Cargando límites cantonales…</span>
        </div>
      )}
      <div
        ref={containerRef}
        style={{ height: 500, borderRadius: 12, overflow: 'hidden' }}
        aria-label="Mapa coroplético de tasa de recuperación por cantón en Costa Rica"
        role="img"
      />
    </div>
  )
}
