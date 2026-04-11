import { screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { SightingList } from '@/features/sightings/components/SightingList'
import { renderWithProviders } from '../../utils/renderWithProviders'

vi.mock('@/features/sightings/hooks/useSightings', () => ({
  useSightingsByPet: vi.fn(),
}))

import { useSightingsByPet } from '@/features/sightings/hooks/useSightings'

const mockUseSightingsByPet = vi.mocked(useSightingsByPet)

describe('SightingList', () => {
  it('renders the priority badge, score and recommended action for each sighting', () => {
    mockUseSightingsByPet.mockReturnValue({
      data: [
        {
          id: 'sighting-1',
          petId: 'pet-1',
          lostPetEventId: 'lost-1',
          lat: 9.9383,
          lng: -84.1001,
          photoUrl: 'https://cdn.example.com/luna.jpg',
          note: 'Seen near the park entrance',
          sightedAt: '2026-04-04T17:40:00Z',
          reportedAt: '2026-04-04T17:45:00Z',
          priorityScore: 91,
          priorityBadge: 'Urgent',
          recommendedAction: 'Contacta al posible testigo y revisa la zona en los próximos 15 minutos.',
        },
      ],
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useSightingsByPet>)

    renderWithProviders(<SightingList petId="pet-1" />)

    expect(screen.getByText(/urgente/i)).toBeInTheDocument()
    expect(screen.getByText(/91\/100/i)).toBeInTheDocument()
    expect(screen.getByText(/revisa la zona en los próximos 15 minutos/i)).toBeInTheDocument()
  })
})