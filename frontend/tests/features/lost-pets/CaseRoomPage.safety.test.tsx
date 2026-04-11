import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import CaseRoomPage from '@/features/lost-pets/pages/CaseRoomPage'
import { useAuthStore } from '@/features/auth/store/authStore'
import { renderWithProviders } from '../../utils/renderWithProviders'

vi.mock('@/features/lost-pets/hooks/useCaseRoom', () => ({
  useCaseRoom: vi.fn(),
}))

vi.mock('@/features/lost-pets/components/CaseActionsPanel', () => ({
  CaseActionsPanel: () => <div>Case actions</div>,
}))

vi.mock('@/features/lost-pets/components/CaseTimeline', () => ({
  CaseTimeline: () => <div>Case timeline</div>,
}))

vi.mock('@/features/lost-pets/components/SightingHeatMap', () => ({
  SightingHeatMap: () => <div>Heat map</div>,
}))

import { useCaseRoom } from '@/features/lost-pets/hooks/useCaseRoom'

const mockedUseCaseRoom = vi.mocked(useCaseRoom)

const baseDto = {
  event: {
    id: 'event-1',
    petId: 'pet-1',
    ownerId: 'owner-1',
    status: 'Active' as const,
    description: 'Perdido en parque',
    publicMessage: null,
    lastSeenLat: 9.93,
    lastSeenLng: -84.08,
    lastSeenAt: '2026-04-01T12:00:00Z',
    reportedAt: '2026-04-01T13:00:00Z',
    resolvedAt: null,
    recentPhotoUrl: null,
    contactName: null,
    rewardAmount: null,
    rewardNote: null,
  },
  sightings: [],
  nearbyAlerts: [],
  totalNearbyAlertsDispatched: 0,
  generatedAt: '2026-04-01T13:10:00Z',
}

const renderPage = () =>
  renderWithProviders(<CaseRoomPage />, {
    initialEntries: ['/case/event-1'],
    routePath: '/case/:id',
  })

describe('CaseRoomPage safety flows', () => {
  beforeEach(() => {
    useAuthStore.setState({
      isAuthenticated: true,
      user: {
        id: 'rescuer-1',
        name: 'Rescatista',
        email: 'rescuer@pawtrack.cr',
        role: 'Ally',
        isAdmin: false,
      },
      accessToken: 'token',
    })

    mockedUseCaseRoom.mockReturnValue({
      data: baseDto,
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as unknown as ReturnType<typeof useCaseRoom>)
  })

  it('shows rescuer handover verification panel for non-owner users in Actions tab', async () => {
    renderPage()

    await userEvent.click(screen.getByRole('tab', { name: /acciones/i }))

    expect(screen.getByText(/Ingresa el código del dueño/i)).toBeInTheDocument()
  })
})
