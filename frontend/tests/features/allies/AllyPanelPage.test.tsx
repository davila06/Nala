import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AllyPanelPage from '@/features/allies/pages/AllyPanelPage'
import { useAuthStore } from '@/features/auth/store/authStore'
import { server } from '../../mocks/server'
import { renderWithProviders } from '../../utils/renderWithProviders'

describe('AllyPanelPage', () => {
  beforeEach(() => {
    useAuthStore.getState().setAuth(
      { id: 'ally-user', name: 'Vet Escazu', email: 'ally@test.cr', role: 'Owner', isAdmin: false },
      'access-token',
    )
  })

  it('shows the application form when the user has not applied yet', async () => {
    server.use(
      http.get('http://localhost:5000/api/allies/me', () => HttpResponse.json(null)),
    )

    renderWithProviders(<AllyPanelPage />)

    expect(await screen.findByRole('heading', { name: /red de aliados verificados/i })).toBeInTheDocument()
    expect(await screen.findByLabelText(/organización/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/zona de cobertura/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /enviar solicitud/i })).toBeInTheDocument()
  })

  it('shows the verified ally inbox and confirms an operational action', async () => {
    const confirmAction = vi.fn()

    server.use(
      http.get('http://localhost:5000/api/allies/me', () =>
        HttpResponse.json({
          userId: 'ally-user',
          organizationName: 'Vet Escazu',
          allyType: 'VeterinaryClinic',
          coverageLabel: 'Escazu centro',
          coverageLat: 9.9187,
          coverageLng: -84.1394,
          coverageRadiusMetres: 2000,
          verificationStatus: 'Verified',
          appliedAt: '2026-04-05T12:00:00Z',
          verifiedAt: '2026-04-05T13:00:00Z',
        }),
      ),
      http.get('http://localhost:5000/api/allies/me/alerts', () =>
        HttpResponse.json([
          {
            notificationId: 'alert-1',
            title: 'Alerta operativa: Nala necesita apoyo en tu zona',
            body: 'Dog Labrador reportado como perdido dentro de tu cobertura declarada.',
            relatedEntityId: 'lost-1',
            isRead: false,
            createdAt: '2026-04-05T13:10:00Z',
            actionConfirmedAt: null,
            actionSummary: null,
          },
        ]),
      ),
      http.put('http://localhost:5000/api/allies/me/alerts/:id/action', async () => {
        confirmAction()
        return new HttpResponse(null, { status: 204 })
      }),
    )

    const user = userEvent.setup()
    renderWithProviders(<AllyPanelPage />)

    expect(await screen.findByText(/alerta operativa: nala necesita apoyo en tu zona/i)).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /ya buscamos en nuestra área/i }))

    await waitFor(() => expect(confirmAction).toHaveBeenCalledTimes(1))
  })
})
