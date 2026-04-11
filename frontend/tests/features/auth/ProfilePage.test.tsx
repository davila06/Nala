import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import ProfilePage from '@/features/auth/pages/ProfilePage'
import { server } from '../../mocks/server'
import { renderWithProviders } from '../../utils/renderWithProviders'
import { useAuthStore } from '@/features/auth/store/authStore'
import { act } from 'react'

const API = 'http://localhost:5000/api'

function seedAuthStore() {
  act(() => {
    useAuthStore.getState().setAuth(
      { id: 'user-1', name: 'Denis Ávila', email: 'denis@test.cr', role: 'Owner', isAdmin: false },
      'mock-token',
    )
  })
}

describe('ProfilePage — identity section', () => {
  it('shows user name and email from /auth/me', async () => {
    server.use(
      http.get(`${API}/auth/me`, () =>
        HttpResponse.json({
          id: 'user-1',
          name: 'Denis Ávila',
          email: 'denis@test.cr',
          isAdmin: false,
        }),
      ),
      // Foster profile returns 404 (user is not a volunteer)
      http.get(`${API}/fosters/me`, () => new HttpResponse(null, { status: 404 })),
    )

    seedAuthStore()
    renderWithProviders(<ProfilePage />)

    await waitFor(() =>
      expect(screen.getByText('Denis Ávila')).toBeInTheDocument(),
    )
    expect(screen.getByText('denis@test.cr')).toBeInTheDocument()
  })

  it('shows edit input when clicking "Editar"', async () => {
    server.use(
      http.get(`${API}/auth/me`, () =>
        HttpResponse.json({ id: 'user-1', name: 'Denis Ávila', email: 'denis@test.cr', isAdmin: false }),
      ),
      http.get(`${API}/fosters/me`, () => new HttpResponse(null, { status: 404 })),
    )

    seedAuthStore()
    const user = userEvent.setup()
    renderWithProviders(<ProfilePage />)

    await waitFor(() => expect(screen.getByText(/editar/i)).toBeInTheDocument())
    await user.click(screen.getByText(/editar/i))

    expect(screen.getByRole('textbox')).toHaveValue('Denis Ávila')
    // Use exact text to distinguish from 'Guardar perfil de custodio'
    expect(screen.getByRole('button', { name: 'Guardar' })).toBeInTheDocument()
  })

  it('saves name via PATCH /auth/me and shows confirmation', async () => {
    server.use(
      http.get(`${API}/auth/me`, () =>
        HttpResponse.json({ id: 'user-1', name: 'Denis Ávila', email: 'denis@test.cr', isAdmin: false }),
      ),
      http.get(`${API}/fosters/me`, () => new HttpResponse(null, { status: 404 })),
      http.patch(`${API}/auth/me`, () => new HttpResponse(null, { status: 204 })),
    )

    seedAuthStore()
    const user = userEvent.setup()
    renderWithProviders(<ProfilePage />)

    await waitFor(() => expect(screen.getByText(/editar/i)).toBeInTheDocument())
    await user.click(screen.getByText(/editar/i))

    const input = screen.getByRole('textbox')
    await user.clear(input)
    await user.type(input, 'Denis V2')
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    await waitFor(() =>
      expect(screen.getByText(/nombre actualizado/i)).toBeInTheDocument(),
    )
  })
})
