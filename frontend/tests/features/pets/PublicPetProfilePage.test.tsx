import { screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import PublicPetProfilePage from '@/features/pets/pages/PublicPetProfilePage'
import { renderWithProviders } from '../../utils/renderWithProviders'

// Mock at the hook level — avoids network entirely
vi.mock('@/features/pets/hooks/usePets', () => ({
  usePublicPetProfile: vi.fn(),
  usePetDetail: vi.fn(),
  usePetScanHistory: vi.fn(),
  usePets: vi.fn(),
}))

import { usePublicPetProfile } from '@/features/pets/hooks/usePets'
const mockProfile = vi.mocked(usePublicPetProfile)

const LOST_PET = {
  id: 'pet-1',
  name: 'Luna',
  species: 'Dog' as const,
  breed: 'Labrador',
  photoUrl: null,
  status: 'Lost' as const,
}

const ACTIVE_PET = {
  id: 'pet-2',
  name: 'Michi',
  species: 'Cat' as const,
  breed: null,
  photoUrl: null,
  status: 'Active' as const,
}

const render = (initialEntry: string) =>
  renderWithProviders(<PublicPetProfilePage />, {
    initialEntries: [initialEntry],
    routePath: '/p/:id',
  })

describe('PublicPetProfilePage', () => {
  it('displays pet name and "Lost" banner when pet status is Lost', () => {
    mockProfile.mockReturnValue({ data: LOST_PET, isLoading: false, isError: false } as ReturnType<typeof usePublicPetProfile>)
    render('/p/pet-1')

    // Pet name in <h1>
    expect(screen.getByRole('heading', { name: /luna/i })).toBeInTheDocument()
    // LostPetBanner text
    expect(screen.getByText(/está reportado como perdido/i)).toBeInTheDocument()
  })

  it('shows report sighting link when status is Lost', () => {
    mockProfile.mockReturnValue({ data: LOST_PET, isLoading: false, isError: false } as ReturnType<typeof usePublicPetProfile>)
    render('/p/pet-1')

    const link = screen.getByRole('link', { name: /reportar avistamiento/i })
    expect(link).toBeInTheDocument()
    expect(link).toHaveAttribute('href', '/p/pet-1/report-sighting')
  })

  it('does not show lost banner for Active pet', () => {
    mockProfile.mockReturnValue({ data: ACTIVE_PET, isLoading: false, isError: false } as ReturnType<typeof usePublicPetProfile>)
    render('/p/pet-2')

    expect(screen.getByRole('heading', { name: /michi/i })).toBeInTheDocument()
    expect(screen.queryByText(/perdid/i)).not.toBeInTheDocument()
  })

  it('matches snapshot — Lost pet state', () => {
    mockProfile.mockReturnValue({ data: LOST_PET, isLoading: false, isError: false } as ReturnType<typeof usePublicPetProfile>)
    const { container } = render('/p/pet-1')

    expect(screen.getByRole('heading', { name: /luna/i })).toBeInTheDocument()
    expect(container).toMatchSnapshot()
  })

  it('shows loading skeleton while fetching', () => {
    mockProfile.mockReturnValue({ data: undefined, isLoading: true, isError: false } as ReturnType<typeof usePublicPetProfile>)
    const { container } = render('/p/pet-1')

    // Loading state renders animated skeleton divs
    expect(container.querySelector('.animate-pulse')).toBeInTheDocument()
  })

  it('shows not found message when API returns error', () => {
    mockProfile.mockReturnValue({ data: undefined, isLoading: false, isError: true } as ReturnType<typeof usePublicPetProfile>)
    render('/p/unknown')

    expect(screen.getByText(/perfil no encontrado/i)).toBeInTheDocument()
  })
})



