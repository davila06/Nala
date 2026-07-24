import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { useRegister } from '../hooks/useAuth'
import PasswordStrengthIndicator from '../components/PasswordStrengthIndicator'
import { Button } from '@/shared/ui/Button'
import { Input } from '@/shared/ui/Input'
import { Alert } from '@/shared/ui/Alert'

// ── Ambient floating paw prints ───────────────────────────────────────────────
const PAWS = [
  { left: '7%',  dur: '6s',  size: '1.3rem', delay: '0s',   opacity: 0.10 },
  { left: '25%', dur: '9s',  size: '0.9rem', delay: '1.4s', opacity: 0.07 },
  { left: '55%', dur: '8s',  size: '1.6rem', delay: '0.6s', opacity: 0.09 },
  { left: '75%', dur: '7s',  size: '1.1rem', delay: '2.2s', opacity: 0.08 },
  { left: '90%', dur: '10s', size: '1.4rem', delay: '3.1s', opacity: 0.06 },
]

// ── Count-up hook ─────────────────────────────────────────────────────────────
function useCountUp(target: number, duration = 1400, start = false) {
  const [value, setValue] = useState(0)
  useEffect(() => {
    if (!start) return
    const t0 = performance.now()
    const tick = (now: number) => {
      const p = Math.min((now - t0) / duration, 1)
      const e = 1 - Math.pow(1 - p, 3)
      setValue(Math.round(e * target))
      if (p < 1) requestAnimationFrame(tick)
    }
    requestAnimationFrame(tick)
  }, [target, duration, start])
  return value
}

function StatItem({ end, suffix, label, started }: { end: number; suffix: string; label: string; started: boolean }) {
  const count = useCountUp(end, 1400, started)
  return (
    <div>
      <p className="font-display text-2xl font-semibold text-rescue-400">{count.toLocaleString('es-CR')}{suffix}</p>
      <p className="text-xs text-trust-300 mt-0.5">{label}</p>
    </div>
  )
}

export default function RegisterPage() {
  const { mutate: register, isPending, error } = useRegister()
  const [form, setForm] = useState({ name: '', email: '', password: '', confirm: '' })
  const [validationError, setValidationError] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const panelRef = useRef<HTMLDivElement>(null)
  const [statsStarted, setStatsStarted] = useState(false)

  useEffect(() => {
    const el = panelRef.current
    if (!el) return
    const observer = new IntersectionObserver(([entry]) => { if (entry.isIntersecting) setStatsStarted(true) }, { threshold: 0.3 })
    observer.observe(el)
    return () => observer.disconnect()
  }, [])

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setValidationError('')
    if (form.password !== form.confirm) { setValidationError('Las contraseñas no coinciden.'); return }
    if (form.password.length < 8) { setValidationError('La contraseña debe tener al menos 8 caracteres.'); return }
    register({ name: form.name, email: form.email, password: form.password })
  }

  return (
    <div className="min-h-dvh lg:grid lg:grid-cols-[1fr_560px]">
      {/* Animated Brand panel */}
      <div
        ref={panelRef}
        className="hidden lg:flex flex-col justify-between bg-trust-900 bg-topo px-12 py-14 text-white overflow-hidden"
        aria-hidden="true"
        style={{ position: 'relative' }}
      >
        {/* Ambient paws */}
        {PAWS.map((p, i) => (
          <span
            key={i}
            style={{
              position: 'absolute', left: p.left, bottom: '-2rem',
              fontSize: p.size, opacity: p.opacity,
              animation: `float-bob ${p.dur} ease-in-out ${p.delay} infinite`,
              userSelect: 'none', pointerEvents: 'none',
            }}
          >🐾</span>
        ))}

        {/* Logo */}
        <div className="flex items-center gap-3 relative z-10">
          <span className="flex h-10 w-10 items-center justify-center rounded-2xl bg-brand-500 text-xl">🐾</span>
          <span className="font-display text-2xl font-semibold tracking-tight">PawTrack CR</span>
        </div>

        {/* Hero copy */}
        <div className="space-y-6 relative z-10">
          <p className="font-display text-4xl leading-snug font-medium text-balance">
            Protege a tu mascota<br />
            <em className="not-italic text-rescue-400">desde hoy.</em>
          </p>
          <ul className="space-y-3 text-trust-200 text-sm leading-relaxed">
            {[
              'Perfil digital con código QR único',
              'Alertas en tiempo real si se pierde',
              'Red de aliados y rescatistas verificados',
              'Historial de avistamientos con geolocalización',
            ].map((feat) => (
              <li key={feat} className="flex items-start gap-2.5">
                <span className="mt-0.5 text-rescue-400 text-base">✓</span>
                {feat}
              </li>
            ))}
          </ul>
        </div>

        {/* Animated stats */}
        <div className="flex gap-8 relative z-10">
          <StatItem end={12000} suffix="+" label="mascotas registradas" started={statsStarted} />
          <StatItem end={94}    suffix=" %" label="tasa de recuperación"  started={statsStarted} />
          <StatItem end={480}   suffix="+"  label="aliados verificados"   started={statsStarted} />
        </div>
      </div>

      {/* Form panel */}
      <div className="flex min-h-dvh flex-col items-center justify-center px-6 py-12 lg:px-12 bg-surface">
        {/* Mobile logo */}
        <div className="mb-8 flex items-center gap-2.5 lg:hidden">
          <span className="flex h-9 w-9 items-center justify-center rounded-2xl bg-brand-500 text-lg text-white" aria-hidden="true">
            🐾
          </span>
          <span className="font-display text-xl font-semibold text-sand-900">PawTrack CR</span>
        </div>

        <div className="w-full max-w-sm">
          <div className="mb-8">
            <h1 className="font-display text-3xl font-semibold text-sand-900">
              Crear cuenta
            </h1>
            <p className="mt-2 text-sm text-sand-500">
              Es gratis. Sin tarjeta de crédito.
            </p>
          </div>

          {(validationError || error) && (
            <Alert variant="error" className="mb-6">
              {validationError || 'Error al registrar. Intenta nuevamente.'}
            </Alert>
          )}

          <form onSubmit={handleSubmit} noValidate className="space-y-4">
            <Input
              label="Nombre completo"
              type="text"
              id="name"
              autoComplete="name"
              required
              placeholder="Tu nombre"
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
            />

            <Input
              label="Correo electrónico"
              type="email"
              id="email"
              autoComplete="email"
              inputMode="email"
              required
              placeholder="tu@correo.com"
              value={form.email}
              onChange={(e) => setForm({ ...form, email: e.target.value })}
            />

            <div className="space-y-1.5">
              <Input
                label="Contraseña"
                type={showPassword ? 'text' : 'password'}
                id="password"
                autoComplete="new-password"
                required
                minLength={8}
                placeholder="Mínimo 8 caracteres"
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
              />
              <PasswordStrengthIndicator password={form.password} />
              <button
                type="button"
                onClick={() => setShowPassword((v) => !v)}
                className="rounded px-1 py-2 text-xs text-sand-500 hover:text-sand-700 transition-base focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
              >
                {showPassword ? 'Ocultar' : 'Mostrar'} contraseña
              </button>
            </div>

            <Input
              label="Confirmar contraseña"
              type={showPassword ? 'text' : 'password'}
              id="confirm"
              autoComplete="new-password"
              required
              placeholder="Repite tu contraseña"
              value={form.confirm}
              onChange={(e) => setForm({ ...form, confirm: e.target.value })}
            />

            <div className="pt-1">
              <Button type="submit" loading={isPending} fullWidth size="lg">
                {isPending ? 'Registrando…' : 'Crear cuenta'}
              </Button>
            </div>
          </form>

          <p className="mt-8 text-center text-sm text-sand-500">
            ¿Ya tienes cuenta?{' '}
            <Link
              to="/login"
              className="rounded font-semibold text-brand-600 hover:text-brand-700 hover:underline transition-base focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
            >
              Iniciar sesión
            </Link>
          </p>

          <p className="mt-4 text-center text-sm text-sand-500">
            ¿Eres veterinaria?{' '}
            <Link
              to="/clinica/registro"
              className="rounded font-semibold text-brand-600 hover:text-brand-700 hover:underline transition-base focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
            >
              Registra tu clínica
            </Link>
          </p>

          <p className="mt-4 text-center text-xs text-sand-400">
            Al registrarte aceptas los{' '}
            <a
              href="/legal/terminos-de-uso.html"
              target="_blank"
              rel="noopener noreferrer"
              className="underline hover:text-sand-600"
            >
              Términos de uso
            </a>{' '}
            y la{' '}
            <a
              href="/legal/politica-de-privacidad.html"
              target="_blank"
              rel="noopener noreferrer"
              className="underline hover:text-sand-600"
            >
              Política de privacidad
            </a>
            .
          </p>
        </div>
      </div>
    </div>
  )
}
