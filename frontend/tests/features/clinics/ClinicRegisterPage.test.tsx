import { screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { ReactNode } from 'react'
import ClinicRegisterPage from '@/features/clinics/pages/ClinicRegisterPage'
import { renderWithProviders } from '../../utils/renderWithProviders'

vi.mock('react-leaflet', () => ({
  MapContainer: ({ children }: { children: ReactNode }) => (
    <div data-testid="clinic-location-map-inner">{children}</div>
  ),
  TileLayer: () => null,
  Marker: () => null,
  useMapEvents: () => null,
  useMap: () => ({ setView: () => undefined, getZoom: () => 13 }),
}))

describe('ClinicRegisterPage', () => {
  it('shows a map to select clinic coordinates', () => {
    renderWithProviders(<ClinicRegisterPage />)

    expect(screen.getByText(/selecciona la ubicacion en el mapa/i)).toBeInTheDocument()
    expect(screen.getByTestId('clinic-location-map')).toBeInTheDocument()
  })
})
