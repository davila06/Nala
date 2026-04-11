import { useMemo } from 'react'

interface PasswordStrengthIndicatorProps {
  password: string
}

type StrengthLevel = {
  label: string
  score: 0 | 1 | 2 | 3
  barCls: string
  labelCls: string
}

const RULES: { label: string; test: (p: string) => boolean }[] = [
  { label: 'Mínimo 8 caracteres',    test: (p) => p.length >= 8 },
  { label: 'Una letra mayúscula',    test: (p) => /[A-Z]/.test(p) },
  { label: 'Un número',              test: (p) => /[0-9]/.test(p) },
  { label: 'Un símbolo (!@#$…)',     test: (p) => /[^A-Za-z0-9]/.test(p) },
]

function computeStrength(password: string): StrengthLevel {
  if (!password) return { label: '', score: 0, barCls: '', labelCls: '' }

  let score = 0
  if (password.length >= 8) score++
  if (password.length >= 12) score++
  if (/[A-Z]/.test(password)) score++
  if (/[a-z]/.test(password)) score++
  if (/[0-9]/.test(password)) score++
  if (/[^A-Za-z0-9]/.test(password)) score++

  if (score <= 2) return { label: 'Débil',  score: 1, barCls: 'bg-danger-500', labelCls: 'text-danger-600' }
  if (score <= 4) return { label: 'Media',  score: 2, barCls: 'bg-warn-400',   labelCls: 'text-warn-600'   }
  return            { label: 'Fuerte', score: 3, barCls: 'bg-rescue-500', labelCls: 'text-rescue-600' }
}

export default function PasswordStrengthIndicator({ password }: PasswordStrengthIndicatorProps) {
  const strength = useMemo(() => computeStrength(password), [password])

  if (!password) return null

  return (
    <div className="mt-1 space-y-2" aria-live="polite">
      {/* Barra de fuerza */}
      <div aria-label={`Seguridad de contraseña: ${strength.label}`}>
        <div className="mb-1 flex gap-1">
          {([1, 2, 3] as const).map((level) => (
            <div
              key={level}
              className={[
                'h-1 flex-1 rounded-full transition-colors duration-200',
                strength.score >= level ? strength.barCls : 'bg-sand-200',
              ].join(' ')}
            />
          ))}
        </div>
        {strength.label && (
          <span className={`text-xs font-medium ${strength.labelCls}`}>
            {strength.label}
          </span>
        )}
      </div>

      {/* Requisitos */}
      <ul className="space-y-1" aria-label="Requisitos de contraseña">
        {RULES.map(({ label, test }) => {
          const ok = test(password)
          return (
            <li key={label} className="flex items-center gap-1.5">
              <span
                aria-hidden="true"
                className={`flex h-4 w-4 shrink-0 items-center justify-center rounded-full text-[10px] font-bold transition-colors duration-200 ${
                  ok ? 'bg-rescue-500 text-white' : 'bg-sand-200 text-sand-400'
                }`}
              >
                {ok ? '✓' : '·'}
              </span>
              <span className={`text-xs transition-colors duration-200 ${ok ? 'text-rescue-600 line-through decoration-rescue-400' : 'text-sand-500'}`}>
                {label}
              </span>
            </li>
          )
        })}
      </ul>
    </div>
  )
}
