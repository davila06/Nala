import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useLogin } from '../hooks/useAuth'
import { Button } from '@/shared/ui/Button'
import { Input } from '@/shared/ui/Input'
import { Alert } from '@/shared/ui/Alert'

// ── Brand panel (left column on desktop) ────────────────────────────────────

function BrandPanel() {
  return (
    <div
      className="hidden lg:flex flex-col justify-between bg-trust-900 bg-topo px-12 py-14 text-white"
      aria-hidden="true"
    >
      {/* Logo */}
      <div className="flex items-center gap-3">
        <span className="flex h-10 w-10 items-center justify-center rounded-2xl bg-brand-500 text-xl">
          🐾
        </span>
        <span className="font-display text-2xl font-semibold tracking-tight">PawTrack CR</span>
      </div>

      {/* Central copy */}
      <div className="space-y-6">
        <p className="font-display text-4xl leading-snug font-medium text-balance">
          Cada mascota merece volver<br />
          <em className="not-italic text-brand-400">a casa.</em>
        </p>
        <p className="text-trust-200 text-base leading-relaxed max-w-sm">
          Identidad digital, seguimiento en tiempo real y
          una red comunitaria de rescate para Costa Rica.
        </p>
      </div>

      {/* Bottom stat row */}
      <div className="flex gap-8">
        {[
          { number: '12 000+', label: 'mascotas registradas' },
          { number: '94 %',    label: 'tasa de recuperación' },
          { number: '480+',    label: 'aliados verificados' },
        ].map(({ number, label }) => (
          <div key={label}>
            <p className="font-display text-2xl font-semibold text-brand-400">{number}</p>
            <p className="text-xs text-trust-300 mt-0.5">{label}</p>
          </div>
        ))}
      </div>
    </div>
  )
}

// ── Login form ───────────────────────────────────────────────────────────────

export default function LoginPage() {
  const { mutate: login, isPending, error } = useLogin()
  const [searchParams] = useSearchParams()
  const justRegistered = searchParams.get('registered') === 'true'

  const [form, setForm] = useState({ email: '', password: '' })
  const [showPassword, setShowPassword] = useState(false)

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    login({ email: form.email, password: form.password })
  }

  return (
    <div className="min-h-dvh lg:grid lg:grid-cols-[1fr_520px]">
      <BrandPanel />

      {/* Form panel */}
      <div className="flex min-h-dvh flex-col items-center justify-center px-6 py-12 lg:px-12 bg-surface">
        {/* Mobile logo */}
        <div className="mb-10 flex items-center gap-2.5 lg:hidden">
          <span className="flex h-9 w-9 items-center justify-center rounded-2xl bg-brand-500 text-lg text-white" aria-hidden="true">
            🐾
          </span>
          <span className="font-display text-xl font-semibold text-sand-900">PawTrack CR</span>
        </div>

        <div className="w-full max-w-sm">
          <div className="mb-8">
            <h1 className="font-display text-3xl font-semibold text-sand-900 text-balance">
              Bienvenido de vuelta
            </h1>
            <p className="mt-2 text-sm text-sand-500">
              Ingresa para acceder al panel de tu mascota.
            </p>
          </div>

          {justRegistered && (
            <Alert variant="success" className="mb-6">
              Cuenta creada exitosamente. Revisa tu correo para verificarla.
            </Alert>
          )}

          {error && (
            <Alert variant="error" className="mb-6">
              Credenciales incorrectas. Verifica tu correo y contraseña.
            </Alert>
          )}

          <form onSubmit={handleSubmit} noValidate className="space-y-5">
            <Input
              label="Correo electrónico"
              type="email"
              id="email"
              autoComplete="email"
              inputMode="email"
              required
              autoFocus
              placeholder="tu@correo.com"
              value={form.email}
              onChange={(e) => setForm({ ...form, email: e.target.value })}
            />

            <div>
              <Input
                label="Contraseña"
                type={showPassword ? 'text' : 'password'}
                id="password"
                autoComplete="current-password"
                required
                placeholder="••••••••"
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
              />
              <button
                type="button"
                onClick={() => setShowPassword((v) => !v)}
                className="mt-1.5 rounded px-1 py-2 text-xs text-sand-500 hover:text-sand-700 transition-base focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
                aria-label="Alternar visibilidad"
                aria-pressed={showPassword}>
                {showPassword ? 'Ocultar' : 'Mostrar'}
              </button>
            </div>

            <div className="flex items-center justify-end">
              <Link
                to="/forgot-password"
                className="rounded text-xs text-brand-600 hover:text-brand-700 hover:underline transition-base focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
              >
                ¿Olvidaste tu contraseña?
              </Link>
            </div>

            <Button type="submit" loading={isPending} fullWidth size="lg">
              {isPending ? 'Ingresando…' : 'Ingresar'}
            </Button>
          </form>

          <p className="mt-8 text-center text-sm text-sand-500">
            ¿No tienes cuenta?{' '}
            <Link
              to="/register"
              className="rounded font-semibold text-brand-600 hover:text-brand-700 hover:underline transition-base focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
            >
              Regístrate gratis
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}

