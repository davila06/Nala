import { useState } from 'react'
import { Link } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'
import { useForgotPassword } from '../hooks/useAuth'
import { Button } from '@/shared/ui/Button'
import { Input } from '@/shared/ui/Input'

export default function ForgotPasswordPage() {
  const { mutate: forgotPassword, isPending } = useForgotPassword()
  const [email, setEmail] = useState('')
  const [submitted, setSubmitted] = useState(false)

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    forgotPassword({ email }, { onSuccess: () => setSubmitted(true) })
  }

  return (
    <div className="flex min-h-dvh flex-col items-center justify-center px-6 py-16" style={{ background: 'linear-gradient(135deg, #f9f5ef 0%, #fff8f4 100%)' }}>
      <div className="w-full max-w-sm">
        {/* Back */}
        <Link to="/login" className="mb-8 inline-flex items-center gap-1.5 rounded-lg text-sm text-sand-500 hover:text-sand-800 transition-base">
          ← Volver a ingresar
        </Link>

        {/* Logo */}
        <div className="mb-8 flex items-center gap-2">
          <span className="flex h-9 w-9 items-center justify-center rounded-2xl bg-brand-500 text-lg text-white">🐾</span>
          <span className="font-display text-xl font-semibold text-sand-900">PawTrack CR</span>
        </div>

        <AnimatePresence mode="wait">
          {/* Success state */}
          {submitted ? (
            <motion.div
              key="success"
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              className="flex flex-col items-center text-center gap-6"
            >
              <motion.div
                initial={{ scale: 0 }}
                animate={{ scale: 1 }}
                transition={{ delay: 0.1, type: 'spring', stiffness: 400, damping: 22 }}
                className="flex h-20 w-20 items-center justify-center rounded-full bg-rescue-100 shadow-lg shadow-rescue-200"
              >
                <span className="text-4xl" style={{ animation: 'float-bob 3s ease-in-out infinite' }}>📬</span>
              </motion.div>
              <div>
                <h1 className="font-display text-2xl font-bold text-sand-900">¡Revisa tu correo!</h1>
                <p className="mt-2 text-sm text-sand-500 leading-relaxed">
                  Si el correo está registrado, recibirás un enlace en unos minutos.
                  Revisa también tu carpeta de spam.
                </p>
              </div>
              <Link to="/login" className="w-full rounded-xl bg-brand-500 py-3 text-center text-sm font-bold text-white hover:bg-brand-600 transition-all hover:-translate-y-0.5">
                Volver a iniciar sesión →
              </Link>
            </motion.div>
          ) : (
            <motion.div key="form" initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }}>
              <div className="mb-8">
                <h1 className="font-display text-3xl font-semibold text-sand-900">Recuperar contraseña</h1>
                <p className="mt-2 text-sm text-sand-500">
                  Te enviaremos un enlace seguro a tu correo registrado.
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
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </div>
  )
}
