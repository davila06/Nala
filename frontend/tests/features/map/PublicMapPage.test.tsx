import { screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { act } from 'react'
import PublicMapPage from '@/features/map/pages/PublicMapPage'
import { renderWithProviders } from '../../utils/renderWithProviders'
import { useAuthStore } from '@/features/auth/store/authStore'

vi.mock('@/features/map/hooks/usePublicMap', () => ({
  useDebouncedBBox: () => ({ debounce: (_set: unknown, _bbox: unknown) => {} }),
  usePublicMapEvents: () => ({ data: [], isFetching: false }),
}))

vi.mock('@/features/map/hooks/useMovementPrediction', () => ({
  useMovementPredictions: () => ({}),
}))

vi.mock('@/features/map/components/MapContainer', () => ({
  MapContainer: () => <div data-testid="map-container" />, 
}))

describe('PublicMapPage', () => {
  it('shows dashboard button when user is authenticated', () => {
    act(() => {
      useAuthStore.getState().setAuth(
        {
          id: 'u-1',
          name: 'Owner',
          email: 'owner@test.cr',
          role: 'Owner',
          isAdmin: false,
        },
        'token',
      )
    })

    renderWithProviders(<PublicMapPage />)

    const dashboardLink = screen.getByRole('link', { name: /volver al dashboard/i })
    expect(dashboardLink).toBeInTheDocument()
    expect(dashboardLink).toHaveAttribute('href', '/dashboard')
  })

  it('hides dashboard button when user is not authenticated', () => {
    act(() => {
      useAuthStore.getState().clearAuth()
    })

    renderWithProviders(<PublicMapPage />)

    expect(screen.queryByRole('link', { name: /volver al dashboard/i })).not.toBeInTheDocument()
  })
})
