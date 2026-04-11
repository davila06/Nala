import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { clinicsApi, type RegisterClinicRequest } from '../api/clinicsApi'
import { Alert } from '@/shared/ui/Alert'
import { Input } from '@/shared/ui/Input'
import { Button } from '@/shared/ui/Button'
import PasswordStrengthIndicator from '@/features/auth/components/PasswordStrengthIndicator'
import { ClinicLocationPicker } from '../components/ClinicLocationPicker'

export default function ClinicRegisterPage() {
  const navigate = useNavigate()
  const [form, setForm] = useState<RegisterClinicRequest>({
    name: '',
    licenseNumber: '',
    address: '',
    lat: 9.9281,
    lng: -84.0908,
    contactEmail: '',
    password: '',
  })
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [validationError, setValidationError] = useState<string | null>(null)

  const { mutate, isPending, error } = useMutation({
    mutationFn: (payload: RegisterClinicRequest) => clinicsApi.register(payload),
    onSuccess: () => navigate('/clinica/pendiente'),
  })

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const { name, value, type } = e.target
    setForm((prev) => ({
      ...prev,
      [name]: type === 'number' ? parseFloat(value) || 0 : value,
    }))
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setValidationError(null)

    if (form.password !== confirmPassword) {
      setValidationError('Las contraseñas no coinciden.')
      return
    }
    if (form.password.length < 8) {
      setValidationError('La contraseña debe tener al menos 8 caracteres.')
      return
    }

    mutate(form)
  }

  const serverError =
    error instanceof Error ? error.message : error ? 'Error al registrar la clínica.' : null

  return (
    <div className="min-h-dvh lg:grid lg:grid-cols-[1fr_560px]">
      {/* Brand panel */}
      <div
        className="hidden lg:flex flex-col justify-between bg-trust-900 bg-topo px-12 py-14 text-white"
        aria-hidden="true"
      >
        <div className="flex items-center gap-3">
          <span className="flex h-10 w-10 items-center justify-center rounded-2xl bg-brand-500 text-xl">
            🐾
          </span>
          <span className="font-display text-2xl font-semibold tracking-tight">PawTrack CR</span>
        </div>

        <div className="space-y-6">
          <p className="font-display text-4xl leading-snug font-medium text-balance">
            Únete a la red<br />
            <em className="not-italic text-rescue-400">veterinaria oficial.</em>
          </p>
          <ul className="space-y-3 text-trust-200 text-sm leading-relaxed">
            {[
              'Escanea QR y microchips de mascotas perdidas',
              'Notifica al dueño de forma instantánea',
              'Directorio público de clínicas afiliadas',
              'Revisión y activación en 1-2 días hábiles',
            ].map((feat) => (
              <li key={feat} className="flex items-start gap-2.5">
                <span className="mt-0.5 text-rescue-400 text-base" aria-hidden="true">✓</span>
                {feat}
              </li>
            ))}
          </ul>
        </div>

        <p className="text-xs text-trust-400">
          Solo para clínicas veterinarias registradas en SENASA · Costa Rica
        </p>
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
              Registrar clínica
            </h1>
            <p className="mt-2 text-sm text-sand-500">
              Afilia tu veterinaria a la red PawTrack CR.
            </p>
          </div>

          {(validationError ?? serverError) && (
            <Alert variant="error" className="mb-6">
              {validationError ?? serverError}
            </Alert>
          )}

          <form onSubmit={handleSubmit} noValidate className="space-y-4">

            {/* ── Datos de la clínica ── */}
            <p className="text-xs font-semibold uppercase tracking-widest text-sand-400">
              Datos de la clínica
            </p>

            <Input
              label="Nombre de la clínica"
              type="text"
              name="name"
              id="name"
              autoComplete="organization"
              required
              placeholder="Ej. Clínica Veterinaria Los Yoses"
              value={form.name}
              onChange={handleChange}
            />

            <Input
              label="Número de licencia SENASA"
              type="text"
              name="licenseNumber"
              id="licenseNumber"
              required
              placeholder="Ej. VET-2024-0123"
              value={form.licenseNumber}
              onChange={handleChange}
              className="font-mono"
            />

            <Input
              label="Dirección"
              type="text"
              name="address"
              id="address"
              autoComplete="street-address"
              required
              placeholder="Ej. 300m norte del parque central, San José"
              value={form.address}
              onChange={handleChange}
            />

            <ClinicLocationPicker
              lat={form.lat}
              lng={form.lng}
              onChange={(lat, lng) => setForm((prev) => ({ ...prev, lat, lng }))}
            />

            {/* Coordinates */}
            <div className="grid grid-cols-2 gap-3">
              <Input
                label="Latitud"
                type="number"
                name="lat"
                id="lat"
                step="0.000001"
                inputMode="decimal"
                value={form.lat}
                onChange={handleChange}
                readOnly
              />
              <Input
                label="Longitud"
                type="number"
                name="lng"
                id="lng"
                step="0.000001"
                inputMode="decimal"
                value={form.lng}
                onChange={handleChange}
                readOnly
              />
            </div>

            {/* ── Credenciales de acceso ── */}
            <p className="pt-2 text-xs font-semibold uppercase tracking-widest text-sand-400">
              Credenciales de acceso
            </p>

            <Input
              label="Correo electrónico de contacto"
              type="email"
              name="contactEmail"
              id="contactEmail"
              autoComplete="email"
              inputMode="email"
              required
              placeholder="clinica@ejemplo.cr"
              value={form.contactEmail}
              onChange={handleChange}
            />

            <div className="space-y-1.5">
              <Input
                label="Contraseña"
                type={showPassword ? 'text' : 'password'}
                name="password"
                id="password"
                autoComplete="new-password"
                required
                minLength={8}
                placeholder="Mínimo 8 caracteres"
                value={form.password}
                onChange={handleChange}
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
              id="confirmPassword"
              autoComplete="new-password"
              required
              placeholder="Repite tu contraseña"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
            />

            <div className="pt-1">
              <Button type="submit" loading={isPending} fullWidth size="lg">
                {isPending ? 'Enviando solicitud…' : 'Solicitar registro'}
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

          <p className="mt-4 text-center text-xs text-sand-400">
            El equipo PawTrack revisará y activará tu cuenta en 1-2 días hábiles.
          </p>
        </div>
      </div>
    </div>
  )
}

