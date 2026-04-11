import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import LoginPage from '@/features/auth/pages/LoginPage'
import { server } from '../../mocks/server'
import { renderWithProviders } from '../../utils/renderWithProviders'

describe('LoginPage', () => {
  it('renders email, password fields and submit button', () => {
    renderWithProviders(<LoginPage />)

    expect(screen.getByLabelText(/correo/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/contraseña/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /ingresar/i })).toBeInTheDocument()
  })

  it('shows error alert when server returns 401', async () => {
    server.use(
      http.post('http://localhost:5000/api/auth/login', () =>
        HttpResponse.json({ detail: 'Invalid credentials' }, { status: 401 }),
      ),
    )

    const user = userEvent.setup()
    renderWithProviders(<LoginPage />)

    await user.type(screen.getByLabelText(/correo/i), 'wrong@test.cr')
    await user.type(screen.getByLabelText(/contraseña/i), 'wrongpass')
    await user.click(screen.getByRole('button', { name: /ingresar/i }))

    await waitFor(() =>
      expect(screen.getByRole('alert')).toHaveTextContent(/credenciales incorrectas/i),
    )
  })

  it('disables submit button while pending', async () => {
    server.use(
      http.post('http://localhost:5000/api/auth/login', async () => {
        await new Promise((r) => setTimeout(r, 200))
        return HttpResponse.json({
          user: { id: 'uid', name: 'Denis', email: 'denis@test.cr', isAdmin: false },
          accessToken: 'token',
          expiresIn: 900,
        })
      }),
    )

    const user = userEvent.setup()
    renderWithProviders(<LoginPage />)

    await user.type(screen.getByLabelText(/correo/i), 'denis@test.cr')
    await user.type(screen.getByLabelText(/contraseña/i), 'SecurePass1')
    await user.click(screen.getByRole('button', { name: /ingresar/i }))

    expect(screen.getByRole('button', { name: /ingresando/i })).toBeDisabled()
  })

  it('has link to registration page', () => {
    renderWithProviders(<LoginPage />)
    expect(screen.getByRole('link', { name: /regístrate/i })).toBeInTheDocument()
  })
})
