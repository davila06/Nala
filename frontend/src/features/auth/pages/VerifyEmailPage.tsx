import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'
import { authApi } from '../api/authApi'
import { Spinner } from '@/shared/ui/Spinner'

type Status = 'verifying' | 'success' | 'error' | 'missing'

const ENVELOPE_POSITIONS = [
  { x: -120, y: -60, size: '1.2rem', delay: '0s',   dur: '5s'  },
  { x:  130, y: -40, size: '0.9rem', delay: '0.8s', dur: '7s'  },
  { x: -100, y:  80, size: '1rem',   delay: '1.5s', dur: '6s'  },
  { x:  110, y:  70, size: '0.8rem', delay: '0.3s', dur: '8s'  },
]

export default function VerifyEmailPage() {
  const [searchParams] = useSearchParams()
  const [status, setStatus] = useState<Status>('verifying')

  useEffect(() => {
    const token = searchParams.get('token')
    if (!token) { setStatus('missing'); return }
    authApi.verifyEmail(token).then(() => setStatus('success')).catch(() => setStatus('error'))
  }, [searchParams])

  return (
    <div className="relative flex min-h-dvh flex-col items-center justify-center overflow-hidden px-6 py-16" style={{ background: 'linear-gradient(135deg, #f9f5ef 0%, #fff8f4 100%)' }}>

      {/* Ambient floating envelopes */}
      {status === 'success' && ENVELOPE_POSITIONS.map((p, i) => (
        <span
          key={i}
          aria-hidden="true"
          style={{
            position: 'absolute',
            left: `calc(50% + ${p.x}px)`,
            top: `calc(50% + ${p.y}px)`,
            fontSize: p.size,
            opacity: 0.15,
            animation: `float-bob ${p.dur} ease-in-out ${p.delay} infinite`,
            pointerEvents: 'none',
            userSelect: 'none',
          }}
        >
          ✉️
        </span>
      ))}

      <div className="relative z-10 w-full max-w-sm text-center">
        {/* Logo */}
        <div className="mb-10 flex items-center justify-center gap-2">
          <span className="flex h-9 w-9 items-center justify-center rounded-2xl bg-brand-500 text-lg text-white" aria-hidden="true">🐾</span>
          <span className="font-display text-xl font-semibold text-sand-900">PawTrack CR</span>
        </div>

        <AnimatePresence mode="wait">
          {/* Verifying */}
          {status === 'verifying' && (
            <motion.div key="verifying" initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0 }} className="flex flex-col items-center gap-4">
              <Spinner size="lg" label="Verificando tu cuenta…" />
              <p className="text-sm text-sand-500">Verificando tu correo, un momento…</p>
            </motion.div>
          )}

          {/* Success */}
          {status === 'success' && (
            <motion.div key="success" initial={{ opacity: 0, scale: 0.9 }} animate={{ opacity: 1, scale: 1 }} transition={{ type: 'spring', stiffness: 300, damping: 24 }} className="flex flex-col items-center gap-6">
              <motion.div
                initial={{ scale: 0 }}
                animate={{ scale: 1 }}
                transition={{ delay: 0.1, type: 'spring', stiffness: 400, damping: 20 }}
                className="flex h-24 w-24 items-center justify-center rounded-full bg-rescue-100 shadow-lg shadow-rescue-200"
              >
                <span className="text-5xl" aria-hidden="true" style={{ animation: 'float-bob 3s ease-in-out infinite' }}>✉️</span>
              </motion.div>
              <div>
                <h1 className="font-display text-2xl font-bold text-sand-900">¡Correo verificado!</h1>
                <p className="mt-2 text-sm text-sand-500 leading-relaxed">
                  Tu cuenta está activa. Ya puedes registrar a tu primera mascota y generar su placa QR.
                </p>
              </div>
              <div className="flex flex-col items-center gap-3 w-full">
                <Link to="/login" className="w-full rounded-xl bg-brand-500 py-3 text-center text-sm font-bold text-white hover:bg-brand-600 transition-all hover:-translate-y-0.5 shadow-md shadow-brand-200">
                  Ingresar ahora →
                </Link>
                <p className="text-xs text-sand-400">Empieza registrando a tu primera mascota</p>
              </div>
            </motion.div>
          )}

          {/* Error */}
          {(status === 'error' || status === 'missing') && (
            <motion.div key="error" initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col items-center gap-6">
              <div className="flex h-20 w-20 items-center justify-center rounded-full bg-danger-100 text-5xl">❌</div>
              <div>
                <h1 className="font-display text-2xl font-bold text-sand-900">
                  {status === 'missing' ? 'Enlace inválido' : 'No se pudo verificar'}
                </h1>
                <p className="mt-2 text-sm text-sand-500">
                  {status === 'missing'
                    ? 'El enlace de verificación está incompleto.'
                    : 'El enlace puede haber expirado. Solicita uno nuevo desde tu perfil.'}
                </p>
              </div>
              <Link to="/login" className="rounded-xl bg-brand-500 px-6 py-3 text-sm font-bold text-white hover:bg-brand-600">
                Volver al inicio
              </Link>
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </div>
  )
}