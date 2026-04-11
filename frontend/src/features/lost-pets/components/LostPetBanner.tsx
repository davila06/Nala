interface LostPetBannerProps {
  petName: string
  publicMessage?: string | null
  onReportLost?: () => void
  className?: string
}

export function LostPetBanner({ petName, publicMessage, onReportLost, className = '' }: LostPetBannerProps) {
  return (
    <div className={`rounded-2xl border border-danger-200 bg-danger-50 px-4 py-4 ${className}`}>
      <div className="flex items-start gap-3">
        <span className="mt-0.5 text-2xl" aria-hidden="true">🚨</span>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-bold text-danger-700">
            {petName} está reportado como perdido
          </p>
          <p className="mt-0.5 text-xs text-danger-500">
            Comparte el perfil público para ayudar a encontrarlo.
          </p>
          {publicMessage && (
            <blockquote className="mt-3 rounded-xl border border-danger-200 bg-white px-4 py-3">
              <p className="text-sm font-medium text-danger-800 leading-relaxed">
                <span aria-hidden="true">💬</span> {publicMessage}
              </p>
            </blockquote>
          )}
        </div>
        {onReportLost && (
          <button
            type="button"
            onClick={onReportLost}
            className="shrink-0 rounded-xl bg-danger-600 px-3 py-2.5 text-xs font-semibold text-white hover:bg-danger-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-danger-400 focus-visible:ring-offset-1"
          >
            Ver reporte
          </button>
        )}
      </div>
    </div>
  )
}

