import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForgotPassword } from '../hooks/useAuth'
import { Button } from '@/shared/ui/Button'
import { Input } from '@/shared/ui/Input'
import { Alert } from '@/shared/ui/Alert'

export default function ForgotPasswordPage() {
  const { mutate: forgotPassword, isPending } = useForgotPassword()
  const [email, setEmail] = useState('')
  const [submitted, setSubmitted] = useState(false)

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    forgotPassword(
      { email },
      { onSuccess: () => setSubmitted(true) },
    )
  }

  return (
    <div className="flex min-h-dvh flex-col items-center justify-center px-6 py-16 bg-surface">
      <div className="w-full max-w-sm animate-fade-in-up">
        {/* Back link */}
        <Link
          to="/login"
          className="mb-8 inline-flex items-center gap-1.5 rounded-lg text-sm text-sand-500 hover:text-sand-800 transition-base focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
        >
          <svg viewBox="0 0 20 20" fill="currentColor" className="h-4 w-4" aria-hidden="true">
            <path fillRule="evenodd" d="M17 10a.75.75 0 0 1-.75.75H5.612l4.158 3.96a.75.75 0 1 1-1.04 1.08l-5.5-5.25a.75.75 0 0 1 0-1.08l5.5-5.25a.75.75 0 1 1 1.04 1.08L5.612 9.25H16.25A.75.75 0 0 1 17 10z" clipRule="evenodd" />
          </svg>
          Volver a ingresar
        </Link>

        {/* Logo */}
        <div className="mb-8 flex items-center gap-2">
          <span className="flex h-9 w-9 items-center justify-center rounded-2xl bg-brand-500 text-lg text-white" aria-hidden="true">
            🐾
          </span>
          <span className="font-display text-xl font-semibold text-sand-900">PawTrack CR</span>
        </div>

        {submitted ? (
          <div className="animate-fade-in">
            <Alert variant="success" title="Enlace enviado">
              Si el correo está registrado, recibirás un enlace para restablecer tu contraseña.
              Revisa también la carpeta de spam.
            </Alert>
            <Link
              to="/login"
              className="mt-6 inline-block rounded text-sm font-semibold text-brand-600 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
            >
              Volver a iniciar sesión →
            </Link>
          </div>
        ) : (
          <>
            <div className="mb-8">
              <h1 className="font-display text-3xl font-semibold text-sand-900">
                Recuperar contraseña
              </h1>
              <p className="mt-2 text-sm text-sand-500">
                Te enviaremos un enlace de recuperación a tu correo registrado.
              </p>
            </div>

            <form onSubmit={handleSubmit} noValidate className="space-y-5">
              <Input
                label="Correo electrónico"
                type="email"
                id="email"
                autoComplete="email"
                inputMode="email"
                required
                placeholder="tu@correo.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />

              <Button type="submit" loading={isPending} fullWidth size="lg">
                Enviar enlace de recuperación
              </Button>
            </form>
          </>
        )}
      </div>
    </div>
  )
}
