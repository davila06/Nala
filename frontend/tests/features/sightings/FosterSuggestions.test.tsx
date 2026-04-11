import { screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { FosterSuggestions } from '@/features/sightings/components/FosterSuggestions'
import { renderWithProviders } from '../../utils/renderWithProviders'

vi.mock('@/features/sightings/hooks/useFosters', () => ({
  useFosterSuggestions: vi.fn(),
}))

import { useFosterSuggestions } from '@/features/sightings/hooks/useFosters'
const mockUseFosterSuggestions = vi.mocked(useFosterSuggestions)

describe('FosterSuggestions', () => {
  it('renders nearby volunteers list', () => {
    mockUseFosterSuggestions.mockReturnValue({
      data: [
        {
          userId: 'u1',
          volunteerName: 'Daniela',
          distanceMetres: 450,
          distanceLabel: '450 m',
          sizePreference: 'Medium',
          maxDays: 5,
          speciesMatch: true,
        },
      ],
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useFosterSuggestions>)

    renderWithProviders(<FosterSuggestions foundReportId="report-1" />)

    expect(screen.getByText(/custodios temporales sugeridos/i)).toBeInTheDocument()
    expect(screen.getByText(/daniela/i)).toBeInTheDocument()
    expect(screen.getByText(/450 m/i)).toBeInTheDocument()
  })
})
