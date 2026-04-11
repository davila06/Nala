import { screen, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { describe, expect, it, vi } from 'vitest'
import { NotificationCenter } from '@/features/notifications/components/NotificationCenter'
import { server } from '../../mocks/server'
import { renderWithProviders } from '../../utils/renderWithProviders'

const API = 'http://localhost:5000/api'

const emptyNotificationsResponse = {
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 20,
}

// Mock the push subscription hook to control browser environment in tests
vi.mock('@/features/notifications/hooks/usePushSubscription', () => ({
  usePushSubscription: () => ({
    status: 'idle',
    subscribe: vi.fn(),
    unsubscribe: vi.fn(),
  }),
}))

describe('NotificationCenter — push opt-in banner', () => {
  it('renders the push opt-in banner when status is idle', async () => {
    server.use(
      http.get(`${API}/notifications`, () =>
        HttpResponse.json(emptyNotificationsResponse),
      ),
      http.get(`${API}/notifications/preferences`, () =>
        HttpResponse.json({ enablePreventiveAlerts: true }),
      ),
    )

    renderWithProviders(<NotificationCenter />)

    await waitFor(() =>
      expect(screen.getByText('Notificaciones push')).toBeInTheDocument(),
    )

    expect(
      screen.getByRole('button', { name: /activar notificaciones/i }),
    ).toBeInTheDocument()
  })
})

