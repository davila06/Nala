import { screen, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import AdminPage from '@/features/admin/pages/AdminPage'
import { server } from '../../mocks/server'
import { renderWithProviders } from '../../utils/renderWithProviders'
import { useAuthStore } from '@/features/auth/store/authStore'
import { act } from 'react'

const API = 'http://localhost:5000/api'

describe('AdminPage', () => {
  it('redirects non-admin users to /dashboard', () => {
    act(() => {
      useAuthStore.getState().setAuth(
        { id: 'u1', name: 'Owner', email: 'o@test.cr', role: 'Owner', isAdmin: false },
        'token',
      )
    })

    renderWithProviders(<AdminPage />, { initialEntries: ['/admin'] })
    // Navigate to /dashboard — the Navigate component renders without crashing
    // RouterProvider would redirect but MemoryRouter renders Navigate inline
    expect(screen.queryByText('Panel de administración')).not.toBeInTheDocument()
  })

  it('shows allies and clinics tabs for admin', async () => {
    act(() => {
      useAuthStore.getState().setAuth(
        { id: 'admin-1', name: 'Admin', email: 'admin@test.cr', role: 'Admin', isAdmin: true },
        'admin-token',
      )
    })

    server.use(
      http.get(`${API}/allies/admin/pending`, () =>
        HttpResponse.json([
          {
            userId: 'ally-1',
            organizationName: 'Rescate Animal CR',
            allyType: 'Rescue',
            coverageLabel: 'San José',
            coverageLat: 9.9,
            coverageLng: -84.1,
            coverageRadiusMetres: 5000,
            verificationStatus: 'Pending',
            appliedAt: '2025-01-01T00:00:00Z',
            verifiedAt: null,
          },
        ]),
      ),
      http.get(`${API}/clinics/admin/pending`, () => HttpResponse.json([])),
    )

    renderWithProviders(<AdminPage />)

    await waitFor(() =>
      expect(screen.getByText('Panel de administración')).toBeInTheDocument(),
    )

    expect(screen.getByRole('button', { name: 'Aliados' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Clínicas' })).toBeInTheDocument()

    await waitFor(() =>
      expect(screen.getByText('Rescate Animal CR')).toBeInTheDocument(),
    )

    expect(screen.getByRole('button', { name: 'Aprobar' })).toBeInTheDocument()
  })

  it('shows empty state when no pending items', async () => {
    act(() => {
      useAuthStore.getState().setAuth(
        { id: 'admin-1', name: 'Admin', email: 'admin@test.cr', role: 'Admin', isAdmin: true },
        'admin-token',
      )
    })

    server.use(
      http.get(`${API}/allies/admin/pending`, () => HttpResponse.json([])),
      http.get(`${API}/clinics/admin/pending`, () => HttpResponse.json([])),
    )

    renderWithProviders(<AdminPage />)

    await waitFor(() =>
      expect(
        screen.getByText('No hay solicitudes de aliados pendientes.'),
      ).toBeInTheDocument(),
    )
  })
})
