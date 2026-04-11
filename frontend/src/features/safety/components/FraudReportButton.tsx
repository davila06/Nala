import { useState } from 'react'
import { useReportFraud } from '../hooks/useSafety'
import type { FraudContext } from '../api/fraudApi'

interface FraudReportButtonProps {
  context: FraudContext
  relatedEntityId?: string | null
  targetUserId?: string | null
  /** Optional label override. Defaults to "Reportar intento de estafa". */
  label?: string
}

/**
 * Compact button that opens an inline confirmation form.
 * Available on the public pet profile and in the chat panel.
 * Reports are accepted from both authenticated and anonymous users.
 */
export function FraudReportButton({
  context,
  relatedEntityId,
  targetUserId,
  label = 'Reportar intento de estafa',
}: FraudReportButtonProps) {
  const [open, setOpen] = useState(false)
  const [description, setDescription] = useState('')
  const [submitted, setSubmitted] = useState(false)
  const [serverMessage, setServerMessage] = useState<string | null>(null)

  const { mutateAsync: report, isPending } = useReportFraud()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      const result = await report({
        context,
        relatedEntityId,
        targetUserId,
        description: description.trim() || null,
      })
      setServerMessage(result.message)
      setSubmitted(true)
    } catch {
      setServerMessage('No se pudo enviar el reporte. Intenta de nuevo.')
    }
  }

  if (!open) {
    return (
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="flex items-center gap-1.5 rounded-full border border-danger-200 px-3 py-1.5 text-xs font-semibold text-danger-600 transition hover:border-danger-400 hover:bg-danger-50"
      >
        🚨 {label}
      </button>
    )
  }

  if (submitted) {
    return (
      <div className="rounded-2xl border border-rescue-200 bg-rescue-50 px-4 py-3">
        <p className="text-sm font-semibold text-rescue-800">✓ Reporte enviado</p>
        {serverMessage && (
          <p className="mt-1 text-xs text-rescue-700">{serverMessage}</p>
        )}
      </div>
    )
  }

  return (
    <div className="rounded-2xl border border-danger-100 bg-danger-50 p-4">
      <h4 className="mb-1 text-sm font-bold text-danger-800">⚠️ Reportar comportamiento sospechoso</h4>
      <p className="mb-3 text-xs text-danger-700">
        Tu reporte es confidencial y ayuda a mantener la comunidad segura. No compartas tus datos
        personales con personas desconocidas.
      </p>
      <form onSubmit={(e) => void handleSubmit(e)} className="flex flex-col gap-2">
        <textarea
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Describe brevemente lo que ocurrió (opcional)…"
          maxLength={500}
          rows={3}
          className="w-full resize-none rounded-xl border border-danger-200 bg-white px-3 py-2 text-xs text-sand-800 outline-none transition focus:border-danger-400"
        />
        {serverMessage && (
          <p className="rounded-lg bg-white px-3 py-1.5 text-xs text-danger-600">{serverMessage}</p>
        )}
        <div className="flex gap-2">
          <button
            type="submit"
            disabled={isPending}
            className="flex items-center gap-1.5 rounded-full bg-danger-600 px-4 py-2 text-xs font-bold text-white transition hover:bg-danger-700 disabled:opacity-50"
          >
            {isPending && (
              <span className="h-3 w-3 animate-spin rounded-full border-2 border-white border-t-transparent" />
            )}
            Enviar reporte
          </button>
          <button
            type="button"
            onClick={() => setOpen(false)}
            className="rounded-full px-4 py-2 text-xs font-semibold text-sand-500 hover:text-sand-800"
          >
            Cancelar
          </button>
        </div>
      </form>
    </div>
  )
}

