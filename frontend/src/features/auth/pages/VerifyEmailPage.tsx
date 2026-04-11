import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { authApi } from '../api/authApi'
import { Spinner } from '@/shared/ui/Spinner'
import { Alert } from '@/shared/ui/Alert'

type Status = 'verifying' | 'success' | 'error' | 'missing'

export default function VerifyEmailPage() {
  const [searchParams] = useSearchParams()
  const [status, setStatus] = useState<Status>('verifying')

  useEffect(() => {
    const token = searchParams.get('token')
    if (!token) {
      setStatus('missing')
      return
    }

    authApi
      .verifyEmail(token)
      .then(() => setStatus('success'))
      .catch(() => setStatus('error'))
  }, [searchParams])

  return (
    <div className="flex min-h-dvh flex-col items-center justify-center px-6 py-16 bg-surface">
      <div className="w-full max-w-sm text-center animate-fade-in-up">
        {/* Logo */}
        <div className="mb-10 flex items-center justify-center gap-2">
          <span className="flex h-9 w-9 items-center justify-center rounded-2xl bg-brand-500 text-lg text-white" aria-hidden="true">
            🐾
          </span>
          <span className="font-display text-xl font-semibold text-sand-900">PawTrack CR</span>
        </div>

        {status === 'verifying' && (
          <div className="flex flex-col items-center gap-4">
            <Spinner size="lg" label="Verificando tu cuenta…" />
            <p className="text-sm text-sand-500">Verificando tu correo…</p>
          </div>
        )}

        {status === 'success' && (
          <div className="flex flex-col items-center gap-6 animate-fade-in">
            <div className="flex h-20 w-20 items-center justify-center rounded-full bg-rescue-100">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5"
                className="h-10 w-10 text-rescue-600" aria-hidden="true">
                <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0z" />
              </svg>
            </div>
            <div>
              <h1 className="font-display text-2xl font-semibold text-sand-900">
                ¡Correo verificado!
              </h1>
              <p className="mt-2 text-sm text-sand-500">
                Tu cuenta está activa. Ya puedes ingresar y registrar a tu mascota.
              </p>
            </div>
            <Link
              to="/login"
              className="inline-flex items-center justify-center gap-2 rounded-xl bg-brand-500 px-6 py-3 text-sm font-semibold text-white shadow-sm hover:bg-brand-600 transition-base"
            >
              Ingresar ahora
            </Link>
          </div>
        )}

        {(status === 'error' || status === 'missing') && (
          <div className="flex flex-col items-center gap-6 animate-fade-in">
            <div className="flex h-20 w-20 items-center justify-center rounded-full bg-danger-100">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5"
                className="h-10 w-10 text-danger-600" aria-hidden="true">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
              </svg>
            </div>
            <div>
              <h1 className="font-display text-2xl font-semibold text-sand-900">
                Enlace inválido
              </h1>
              <Alert variant="error" className="mt-3 text-left">
                {status === 'missing'
                  ? 'El enlace de verificación es inválido.'
                  : 'El enlace expiró o ya fue utilizado. Solicita uno nuevo.'}
              </Alert>
            </div>
            <Link
              to="/login"
              className="text-sm font-semibold text-brand-600 hover:underline"
            >
              Volver a ingresar
            </Link>
          </div>
        )}
      </div>
    </div>
  )
}

