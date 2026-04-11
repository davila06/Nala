import { screen } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import PetDetailPage from '@/features/pets/pages/PetDetailPage'
import { renderWithProviders } from '../../utils/renderWithProviders'

vi.mock('@/features/pets/hooks/usePets', () => ({
  usePetDetail: vi.fn(),
  usePetScanHistory: vi.fn(),
  usePublicPetProfile: vi.fn(),
  usePets: vi.fn(),
}))

vi.mock('@/features/lost-pets/hooks/useLostPets', () => ({
  useActiveLostReport: vi.fn(),
  useUpdateLostPetStatus: vi.fn(() => ({
    mutateAsync: vi.fn(),
    isPending: false,
  })),
}))

vi.mock('@/features/sightings/components/SightingList', () => ({
  SightingList: () => <div>Sightings list</div>,
}))

vi.mock('@/features/pets/api/petsApi', () => ({
  petsApi: {
    deletePet: vi.fn(),
    getWhatsAppAvatar: vi.fn(),
  },
}))

import { usePetDetail, usePetScanHistory } from '@/features/pets/hooks/usePets'
import { useActiveLostReport } from '@/features/lost-pets/hooks/useLostPets'

const mockUsePetDetail = vi.mocked(usePetDetail)
const mockUsePetScanHistory = vi.mocked(usePetScanHistory)
const mockUseActiveLostReport = vi.mocked(useActiveLostReport)

function renderPage() {
  return renderWithProviders(<PetDetailPage />, {
    initialEntries: ['/pets/pet-1'],
    routePath: '/pets/:id',
  })
}

describe('PetDetailPage lost-state actions', () => {
  beforeEach(() => {
    mockUseActiveLostReport.mockReturnValue({ data: { id: 'event-1' } } as ReturnType<typeof useActiveLostReport>)
    mockUsePetScanHistory.mockReturnValue({
      data: {
        scansToday: 1,
        events: [
          {
            scannedAt: '2026-04-05T13:15:00Z',
            cityName: 'San Pedro',
            countryCode: 'CR',
            deviceSummary: 'iPhone',
          },
        ],
      },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof usePetScanHistory>)

    mockUsePetDetail.mockReturnValue({
      data: {
        id: 'pet-1',
        ownerId: 'owner-1',
        name: 'Luna',
        species: 'Dog',
        breed: 'Labrador',
        birthDate: null,
        photoUrl: null,
        status: 'Lost',
        createdAt: '2026-04-05T10:00:00Z',
        updatedAt: '2026-04-05T12:00:00Z',
      },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof usePetDetail>)
  })

  it('renders owner-facing lost actions when pet is lost', () => {
    renderPage()

    expect(screen.getByText(/luna está reportado como perdido/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /avatar para whatsapp/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /marcar como reunido/i })).toBeInTheDocument()
    expect(screen.getByText(/1 escaneo hoy/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /historial de escaneos del qr/i })).toBeInTheDocument()
  })

  it('shows report lost CTA only when pet status is active', () => {
    mockUseActiveLostReport.mockReturnValue({ data: undefined } as ReturnType<typeof useActiveLostReport>)

    mockUsePetDetail.mockReturnValue({
      data: {
        id: 'pet-1',
        ownerId: 'owner-1',
        name: 'Luna',
        species: 'Dog',
        breed: 'Labrador',
        birthDate: null,
        photoUrl: null,
        status: 'Active',
        createdAt: '2026-04-05T10:00:00Z',
        updatedAt: '2026-04-05T12:00:00Z',
      },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof usePetDetail>)

    renderPage()

    expect(screen.getByRole('link', { name: /reportar como perdido/i })).toBeInTheDocument()
  })
})
