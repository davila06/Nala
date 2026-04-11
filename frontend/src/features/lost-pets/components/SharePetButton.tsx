import { useSharePet, type SharePetOptions } from '../hooks/useSharePet'

// ── Types ──────────────────────────────────────────────────────────────────────

export interface SharePetButtonProps extends SharePetOptions {
  /** Additional CSS classes for the outer wrapper */
  className?: string
  /**
   * Visual variant:
   * - 'primary'  — filled amber button (confirmation page / detail page)
   * - 'outline'  — outlined amber button (public profile)
   * - 'compact'  — icon-only row for tight layouts
   */
  variant?: 'primary' | 'outline' | 'compact'
}

// ── Status label helpers ───────────────────────────────────────────────────────

function nativeLabel(status: string, petName: string): string {
  if (status === 'sharing') return 'Compartiendo…'
  if (status === 'shared') return '¡Compartido!'
  if (status === 'error') return 'Error al compartir'
  return `Compartir perfil de ${petName}`
}

// ── SVG icons (no external deps) ──────────────────────────────────────────────

function IconShare() {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      className="size-4 shrink-0"
    >
      <path d="M4 12v8a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-8" />
      <polyline points="16 6 12 2 8 6" />
      <line x1="12" y1="2" x2="12" y2="15" />
    </svg>
  )
}

function IconCopy() {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      className="size-4 shrink-0"
    >
      <rect x="9" y="9" width="13" height="13" rx="2" ry="2" />
      <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
    </svg>
  )
}

function IconWhatsApp() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24" fill="currentColor" className="size-4 shrink-0">
      <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.198.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 0 1-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 0 1-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 0 1 2.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0 0 12.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 0 0 5.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 0 0-3.48-8.413Z" />
    </svg>
  )
}

function IconFacebook() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24" fill="currentColor" className="size-4 shrink-0">
      <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z" />
    </svg>
  )
}

function IconX() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24" fill="currentColor" className="size-4 shrink-0">
      <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-4.714-6.231-5.401 6.231H2.742l7.73-8.835L1.254 2.25H8.08l4.256 5.629L18.244 2.25Zm-1.161 17.52h1.833L7.084 4.126H5.117L17.083 19.77Z" />
    </svg>
  )
}

// ── Fallback social bar ────────────────────────────────────────────────────────

interface FallbackBarProps {
  onCopy: () => Promise<void>
  onWhatsApp: () => void
  onFacebook: () => void
  onX: () => void
  copied: boolean
}

function FallbackBar({ onCopy, onWhatsApp, onFacebook, onX, copied }: FallbackBarProps) {
  return (
    <div className="mt-2 flex flex-wrap items-center gap-2">
      <button
        type="button"
        onClick={onCopy}
        className="flex items-center gap-1.5 rounded-lg border border-sand-300 px-3 py-2.5 text-xs font-medium text-sand-700 transition-colors hover:bg-sand-50 active:scale-95 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
        aria-label="Copiar enlace al portapapeles"
      >
        <IconCopy />
        {copied ? '¡Copiado!' : 'Copiar link'}
      </button>

      <button
        type="button"
        onClick={onWhatsApp}
        className="flex items-center gap-1.5 rounded-lg bg-[#25D366] px-3 py-2.5 text-xs font-medium text-white transition-colors hover:bg-[#1eb554] active:scale-95 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-green-400"
        aria-label="Compartir por WhatsApp"
      >
        <IconWhatsApp />
        WhatsApp
      </button>

      <button
        type="button"
        onClick={onFacebook}
        className="flex items-center gap-1.5 rounded-lg bg-[#1877F2] px-3 py-2.5 text-xs font-medium text-white transition-colors hover:bg-[#1465d0] active:scale-95 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-400"
        aria-label="Compartir en Facebook"
      >
        <IconFacebook />
        Facebook
      </button>

      <button
        type="button"
        onClick={onX}
        className="flex items-center gap-1.5 rounded-lg bg-sand-900 px-3 py-2.5 text-xs font-medium text-white transition-colors hover:bg-sand-700 active:scale-95 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sand-400"
        aria-label="Compartir en X (Twitter)"
      >
        <IconX />X
      </button>
    </div>
  )
}

// ── Main component ─────────────────────────────────────────────────────────────

/**
 * Share button for a pet's public profile.
 *
 * - Uses the native Web Share API when available (mobile).
 * - Falls back to individual social + clipboard buttons on desktop.
 * - Zero external dependencies.
 */
export function SharePetButton({
  petId,
  petName,
  context,
  baseUrl,
  className = '',
  variant = 'primary',
}: SharePetButtonProps) {
  const { status, canShare, share, copyLink, shareWhatsApp, shareFacebook, shareX } =
    useSharePet({ petId, petName, context, baseUrl })

  const copied = status === 'copied'
  const isSharing = status === 'sharing'
  const isShared = status === 'shared'

  // ── Native share path ──────────────────────────────────────────────────────

  if (canShare) {
    const label = nativeLabel(status, petName)

    const baseClass =
      variant === 'outline'
        ? 'flex w-full items-center justify-center gap-2 rounded-xl border border-brand-400 py-3 text-sm font-semibold text-brand-600 transition-colors hover:bg-brand-50 disabled:opacity-60 active:scale-[.98]'
        : 'flex w-full items-center justify-center gap-2 rounded-xl bg-brand-500 py-3 text-sm font-semibold text-white transition-colors hover:bg-brand-600 disabled:opacity-60 active:scale-[.98]'

    return (
      <div className={className}>
        <button
          type="button"
          onClick={share}
          disabled={isSharing}
          aria-label={label}
          className={baseClass}
        >
          {isShared ? (
            <span aria-hidden="true">✅</span>
          ) : (
            <IconShare />
          )}
          {label}
        </button>
      </div>
    )
  }

  // ── Fallback path (desktop / unsupported browsers) ─────────────────────────

  return (
    <div className={className}>
      <p className="mb-1.5 text-xs font-semibold text-sand-500 uppercase tracking-wide">
        Compartir perfil de {petName}
      </p>
      <FallbackBar
        onCopy={copyLink}
        onWhatsApp={shareWhatsApp}
        onFacebook={shareFacebook}
        onX={shareX}
        copied={copied}
      />
    </div>
  )
}

