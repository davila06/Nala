import { useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useResetPassword } from '../hooks/useAuth'
import { Button } from '@/shared/ui/Button'
import { Input } from '@/shared/ui/Input'
import { Alert } from '@/shared/ui/Alert'

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const tokenFromUrl = useMemo(() => searchParams.get('token') ?? '', [searchParams])

  const { mutate: resetPassword, isPending, error } = useResetPassword()

  const [token, setToken]               = useState(tokenFromUrl)
  const [newPassword, setNewPassword]   = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [validationError, setValidationError] = useState<string | null>(null)
  const [completed, setCompleted]       = useState(false)

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault()

    if (newPassword !== confirmPassword) {
      setValidationError('Las contraseñas no coinciden.')
      return
    }
    if (newPassword.length < 8) {
      setValidationError('La contraseña debe tener al menos 8 caracteres.')
      return
    }

    setValidationError(null)
    resetPassword({ token, newPassword }, { onSuccess: () => setCompleted(true) })
  }

  return (
    <div className="flex min-h-dvh flex-col items-center justify-center px-6 py-16 bg-surface">
      <div className="w-full max-w-sm animate-fade-in-up">
        {/* Logo */}
        <div className="mb-8 flex items-center gap-2">
          <span className="flex h-9 w-9 items-center justify-center rounded-2xl bg-brand-500 text-lg text-white" aria-hidden="true">
            🐾
          </span>
          <span className="font-display text-xl font-semibold text-sand-900">PawTrack CR</span>
        </div>

        {completed ? (
          <div className="animate-fade-in">
            <Alert variant="success" title="Contraseña actualizada">
              Tu contraseña fue actualizada correctamente. Ya puedes ingresar.
            </Alert>
            <Link
              to="/login"
              className="mt-6 inline-block text-sm font-semibold text-brand-600 hover:underline"
            >
              Ir a ingresar →
            </Link>
          </div>
        ) : (
          <>
            <div className="mb-8">
              <h1 className="font-display text-3xl font-semibold text-sand-900">
                Nueva contraseña
              </h1>
              <p className="mt-2 text-sm text-sand-500">
                Elige una contraseña segura de al menos 8 caracteres.
              </p>
            </div>

            {(validationError || error) && (
              <Alert variant="error" className="mb-6">
                {validationError || 'El enlace es inválido o ha expirado. Solicita uno nuevo.'}
              </Alert>
            )}

            <form onSubmit={handleSubmit} noValidate className="space-y-5">
              {/* Show token field only when not pre-filled from URL */}
              {!tokenFromUrl && (
                <Input
                  label="Token de recuperación"
                  type="text"
                  id="token"
                  required
                  placeholder="Pega el código del correo"
                  value={token}
                  onChange={(e) => setToken(e.target.value)}
                  hint="Cópialo desde el enlace del correo que recibiste."
                />
              )}

              <Input
                label="Nueva contraseña"
                type="password"
                id="newPassword"
                autoComplete="new-password"
                required
                minLength={8}
                placeholder="Mínimo 8 caracteres"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
              />

              <Input
                label="Confirmar contraseña"
                type="password"
                id="confirmPassword"
                autoComplete="new-password"
                required
                placeholder="Repite tu contraseña"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
              />

              <Button type="submit" loading={isPending} fullWidth size="lg">
                Actualizar contraseña
              </Button>
            </form>

            <p className="mt-6 text-center text-sm text-sand-500">
              <Link
                to="/login"
                className="font-semibold text-brand-600 hover:underline"
              >
                Volver a ingresar
              </Link>
            </p>
          </>
        )}
      </div>
    </div>
  )
}
