import { useState } from 'react'
import { useGenerateHandoverCode, useVerifyHandoverCode } from '../hooks/useSafety'
import { Alert } from '@/shared/ui/Alert'

// ── Owner panel — generates + displays the code ───────────────────────────────

interface OwnerHandoverPanelProps {
  lostPetEventId: string
}

export function OwnerHandoverPanel({ lostPetEventId }: OwnerHandoverPanelProps) {
  const { mutateAsync: generate, isPending, data, reset } = useGenerateHandoverCode()
  const [genError, setGenError] = useState<string | null>(null)

  const handleGenerate = async () => {
    setGenError(null)
    try {
      await generate(lostPetEventId)
    } catch {
      setGenError('No se pudo generar el código. Intenta de nuevo.')
    }
  }

  return (
    <div className="rounded-2xl border border-sand-200 bg-white p-5 shadow-sm">
      <p className="mb-1 text-xs font-semibold uppercase tracking-widest text-sand-500">
        Entrega segura
      </p>
      <h3 className="mb-3 text-base font-bold text-sand-900">Código de verificación</h3>
      <p className="mb-4 text-sm text-sand-600">
        Genera un código de 4 dígitos y compártelo verbalmente con el rescatista cuando se
        encuentren. El rescatista lo ingresará en la app para confirmar la entrega segura.
      </p>

      {data ? (
        <div className="mb-4 flex flex-col items-center gap-2">
          <div className="flex gap-2">
            {data.code.split('').map((digit, i) => (
              <span
                key={i}
                className="flex h-14 w-12 items-center justify-center rounded-xl border-2 border-sand-900 text-3xl font-black text-sand-900"
              >
                {digit}
              </span>
            ))}
          </div>
          <p className="text-xs text-sand-400">Válido por {data.expiresInHours} horas · de un solo uso</p>
          <button
            type="button"
            onClick={() => { reset(); setGenError(null) }}
            className="text-xs text-sand-500 underline hover:text-sand-800"
          >
            Generar nuevo código
          </button>
        </div>
      ) : (
        <>
          {genError && (
          <Alert variant="error">{genError}</Alert>
          )}
          <button
            type="button"
            onClick={() => void handleGenerate()}
            disabled={isPending}
            className="flex items-center gap-2 rounded-full bg-sand-900 px-5 py-2.5 text-sm font-bold text-white disabled:opacity-50"
          >
            {isPending && (
              <span className="h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
            )}
            Generar código
          </button>
        </>
      )}
    </div>
  )
}

// ── Rescuer panel — enters the code to confirm handover ───────────────────────

interface RescuerHandoverPanelProps {
  lostPetEventId: string
}

export function RescuerHandoverPanel({ lostPetEventId }: RescuerHandoverPanelProps) {
  const [code, setCode] = useState('')
  const [verifyError, setVerifyError] = useState<string | null>(null)
  const { mutateAsync: verify, isPending, data, reset } = useVerifyHandoverCode()

  const handleVerify = async (e: React.FormEvent) => {
    e.preventDefault()
    const trimmed = code.trim()
    if (trimmed.length !== 4 || !/^\d{4}$/.test(trimmed)) {
      setVerifyError('El código debe ser un número de 4 dígitos.')
      return
    }
    setVerifyError(null)
    try {
      await verify({ lostPetEventId, code: trimmed })
    } catch {
      setVerifyError('No se pudo verificar el código. Intenta de nuevo.')
    }
  }

  if (data?.verified) {
    return (
      <div className="rounded-2xl border border-rescue-200 bg-rescue-50 p-5">
        <p className="text-2xl">✅</p>
        <h3 className="mt-2 text-base font-bold text-rescue-800">Entrega confirmada</h3>
        <p className="mt-1 text-sm text-rescue-700">
          La entrega segura ha sido registrada. ¡Gracias por ayudar a reunir a esta mascota con su familia!
        </p>
      </div>
    )
  }

  if (data && !data.verified) {
    return (
      <div className="rounded-2xl border border-danger-200 bg-danger-50 p-5">
        <p className="text-2xl">❌</p>
        <h3 className="mt-2 text-base font-bold text-danger-800">Código incorrecto o expirado</h3>
        <p className="mt-1 text-sm text-danger-700">
          El código no es válido o ya fue utilizado. Pide al dueño que genere uno nuevo.
        </p>
        <button
          type="button"
          onClick={() => { reset(); setCode('') }}
          className="mt-3 text-sm text-danger-700 underline hover:text-danger-900"
        >
          Intentar de nuevo
        </button>
      </div>
    )
  }

  return (
    <div className="rounded-2xl border border-sand-200 bg-white p-5 shadow-sm">
      <p className="mb-1 text-xs font-semibold uppercase tracking-widest text-sand-500">
        Confirmación de entrega
      </p>
      <h3 className="mb-3 text-base font-bold text-sand-900">Ingresa el código del dueño</h3>
      <p className="mb-4 text-sm text-sand-600">
        El dueño te comunicará verbalmente un código de 4 dígitos al encontrarse.
        Ingrésalo aquí para confirmar la entrega segura.
      </p>
      <form onSubmit={(e) => void handleVerify(e)} className="flex flex-col gap-3">
        <input
          type="text"
          inputMode="numeric"
          pattern="\d{4}"
          maxLength={4}
          value={code}
          onChange={(e) => setCode(e.target.value.replace(/\D/g, '').slice(0, 4))}
          placeholder="0000"
          className="w-32 rounded-xl border border-sand-300 px-4 py-3 text-center text-2xl font-bold tracking-widest text-sand-900 outline-none transition focus:border-sand-900"
        />
        {verifyError && (
          <Alert variant="error">{verifyError}</Alert>
        )}
        <button
          type="submit"
          disabled={isPending || code.length !== 4}
          className="flex w-fit items-center gap-2 rounded-full bg-sand-900 px-5 py-2.5 text-sm font-bold text-white disabled:opacity-50"
        >
          {isPending && (
            <span className="h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
          )}
          Confirmar entrega
        </button>
      </form>
    </div>
  )
}

