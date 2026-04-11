import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import RegisterPage from '@/features/auth/pages/RegisterPage'
import { server } from '../../mocks/server'
import { renderWithProviders } from '../../utils/renderWithProviders'

describe('RegisterPage', () => {
  it('renders all form fields', () => {
    renderWithProviders(<RegisterPage />)

    expect(screen.getByLabelText(/nombre/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/correo/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/^contraseña/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/confirmar/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /crear cuenta/i })).toBeInTheDocument()
  })

  it('shows error when passwords do not match', async () => {
    const user = userEvent.setup()
    renderWithProviders(<RegisterPage />)

    await user.type(screen.getByLabelText(/nombre/i), 'Denis')
    await user.type(screen.getByLabelText(/correo/i), 'denis@test.cr')
    await user.type(screen.getByLabelText(/^contraseña/i), 'SecurePass1')
    await user.type(screen.getByLabelText(/confirmar/i), 'different')
    await user.click(screen.getByRole('button', { name: /crear cuenta/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/contraseñas no coinciden/i)
  })

  it('shows error when password is too short', async () => {
    const user = userEvent.setup()
    renderWithProviders(<RegisterPage />)

    await user.type(screen.getByLabelText(/^contraseña/i), 'abc')
    await user.type(screen.getByLabelText(/confirmar/i), 'abc')
    await user.click(screen.getByRole('button', { name: /crear cuenta/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/8 caracteres/i)
  })

  it('disables submit button while pending', async () => {
    // Delay the response so we can inspect the loading state
    server.use(
      http.post('http://localhost:5000/api/auth/register', async () => {
        await new Promise((r) => setTimeout(r, 200))
        return HttpResponse.json({ userId: 'uid' }, { status: 201 })
      }),
    )

    const user = userEvent.setup()
    renderWithProviders(<RegisterPage />)

    await user.type(screen.getByLabelText(/nombre/i), 'Denis')
    await user.type(screen.getByLabelText(/correo/i), 'denis@test.cr')
    await user.type(screen.getByLabelText(/^contraseña/i), 'SecurePass1')
    await user.type(screen.getByLabelText(/confirmar/i), 'SecurePass1')
    await user.click(screen.getByRole('button', { name: /crear cuenta/i }))

    expect(screen.getByRole('button', { name: /registrando/i })).toBeDisabled()
  })

  it('shows API error message when server rejects registration', async () => {
    server.use(
      http.post('http://localhost:5000/api/auth/register', () =>
        HttpResponse.json({ detail: 'Email already exists' }, { status: 400 }),
      ),
    )

    const user = userEvent.setup()
    renderWithProviders(<RegisterPage />)

    await user.type(screen.getByLabelText(/nombre/i), 'Denis')
    await user.type(screen.getByLabelText(/correo/i), 'exists@test.cr')
    await user.type(screen.getByLabelText(/^contraseña/i), 'SecurePass1')
    await user.type(screen.getByLabelText(/confirmar/i), 'SecurePass1')
    await user.click(screen.getByRole('button', { name: /crear cuenta/i }))

    await waitFor(() =>
      expect(screen.getByRole('alert')).toHaveTextContent(/error al registrar/i),
    )
  })
})
