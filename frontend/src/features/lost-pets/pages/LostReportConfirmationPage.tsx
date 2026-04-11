import { useEffect, useRef, useState } from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import { BroadcastPanel } from '../components/BroadcastPanel'
import { EmergencyModeButton } from '../components/EmergencyModeButton'
import { SearchChecklist } from '../components/SearchChecklist'
import { SearchFlyerTemplate, type SearchFlyerData } from '../components/SearchFlyerTemplate'
import {
  SocialShareImageTemplate,
  type SocialShareImageData,
} from '../components/SocialShareImageTemplate'
import { SharePetButton } from '../components/SharePetButton'
import { useEmergencyMode } from '../hooks/useEmergencyMode'
import { useGenerateFlyer } from '../hooks/useGenerateFlyer'
import {
  estimateSearchRadius,
  formatRadius,
  hoursElapsedSince,
  resolveSearchRadiusWithLocalStats,
} from '../utils/searchRadius'
import { useRecoveryRates } from '../hooks/useRecoveryStats'
import { usePetDetail } from '@/features/pets/hooks/usePets'

// ── Route state ────────────────────────────────────────────────────────────────

interface ConfirmationRouteState {
  lostEventId: string
  lastSeenAt: string
  description: string | null
  recentPhotoUrl: string | null
}

function isValidState(s: unknown): s is ConfirmationRouteState {
  return (
    typeof s === 'object' &&
    s !== null &&
    typeof (s as Record<string, unknown>).lostEventId === 'string' &&
    typeof (s as Record<string, unknown>).lastSeenAt === 'string'
  )
}

// ── Component ──────────────────────────────────────────────────────────────────

export default function LostReportConfirmationPage() {
  const { id } = useParams<{ id: string }>()
  const location = useLocation()

  const routeState = isValidState(location.state) ? location.state : null

  const { data: pet, isLoading } = usePetDetail(id ?? '')

  const flyerRef   = useRef<HTMLDivElement>(null)
  const socialRef  = useRef<HTMLDivElement>(null)
  const checklistRef = useRef<HTMLElement>(null)

  const [shareError, setShareError] = useState<string | null>(null)
  const [shareSuccess, setShareSuccess] = useState(false)

  /**
   * Capture intent drives the deferred html2canvas capture:
   * - 'download' / 'share'          → print flyer (600×840 ×2x)
   * - 'social-download' / 'social-share' → social image (1200×630 ×1x)
   *
   * A useEffect watches for the combination of assets being non-null +
   * a pending intent so html2canvas always runs AFTER the DOM has been
   * committed with real data URLs.
   */
  const [captureIntent, setCaptureIntent] = useState<
    'download' | 'share' | 'social-download' | 'social-share' | null
  >(null)

  const {
    state: flyerState,
    assets,
    prepareAssets,
    downloadFlyer,
    buildFlyerBlob,
    downloadSocialImage,
    buildSocialImageBlob,
    errorMessage,
  } = useGenerateFlyer(id ?? '', pet?.name ?? '', pet?.photoUrl ?? null, routeState?.recentPhotoUrl ?? null)

  const emergencyMode = useEmergencyMode({
    petId: id ?? '',
    petName: pet?.name ?? '',
    flyerHook: { prepareAssets, buildFlyerBlob, assets, state: flyerState },
    flyerRef,
    checklistRef,
  })

  // Pre-fetch assets as soon as the page mounts so they're usually ready
  // by the time the user clicks the download button.
  useEffect(() => {
    void prepareAssets()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  // Execute capture AFTER React has committed the updated flyerData to the DOM.
  // The effect depends on `assets` so it re-fires when assets become non-null.
  useEffect(() => {
    if (captureIntent === null || assets === null || !pet || !routeState) return

    const intent = captureIntent
    let cancelled = false

    const execute = async () => {
      // Allow two animation frames + 120 ms for images to fully paint
      await new Promise<void>((resolve) => {
        requestAnimationFrame(() => requestAnimationFrame(() => setTimeout(resolve, 120)))
      })
      if (cancelled) return

      if (intent === 'download') {
        await downloadFlyer(flyerRef)
      } else if (intent === 'share') {
        if (!navigator.share) {
          setShareError(
            'Tu dispositivo no soporta compartir directamente. Descarga el flyer y compártelo manualmente.',
          )
          return
        }
        try {
          const blob = await buildFlyerBlob(flyerRef)
          const safeName = pet.name.toLowerCase().replace(/\s+/g, '-')
          const file = new File([blob], `flyer-${safeName}.png`, { type: 'image/png' })
          await navigator.share({
            title: `¡Ayuda a encontrar a ${pet.name}!`,
            text: `${pet.name} está perdido. Si lo ves, escanea su QR o contacta al dueño.`,
            url: `${window.location.origin}/p/${pet.id}`,
            files: [file],
          })
          setShareSuccess(true)
        } catch (err) {
          if (err instanceof Error && err.name === 'AbortError') return
          setShareError('No se pudo compartir el flyer. Intenta descargarlo manualmente.')
        }
      } else if (intent === 'social-download') {
        await downloadSocialImage(socialRef)
      } else if (intent === 'social-share') {
        if (!navigator.share) {
          setShareError(
            'Tu dispositivo no soporta compartir directamente. Descarga la imagen y compártela manualmente.',
          )
          return
        }
        try {
          const blob = await buildSocialImageBlob(socialRef)
          const safeName = pet.name.toLowerCase().replace(/\s+/g, '-')
          const file = new File([blob], `alerta-${safeName}-social.png`, { type: 'image/png' })
          await navigator.share({
            title: `¡Ayuda a encontrar a ${pet.name}!`,
            text: `${pet.name} está perdido en Costa Rica. ¿Lo has visto?`,
            url: `${window.location.origin}/p/${pet.id}`,
            files: [file],
          })
          setShareSuccess(true)
        } catch (err) {
          if (err instanceof Error && err.name === 'AbortError') return
          setShareError('No se pudo compartir la imagen. Intenta descargarla manualmente.')
        }
      }

      setCaptureIntent(null)
    }

    void execute()
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [captureIntent, assets])

  // ── Guard: invalid navigation ───────────────────────────────────────────────

  if (!routeState) {
    return (
      <div className="mx-auto max-w-lg px-4 py-12 text-center">
        <p className="text-sand-500">Página no disponible directamente.</p>
        <Link to="/dashboard" className="mt-4 inline-block rounded text-sm text-brand-600 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400">
          ← Volver al inicio
        </Link>
      </div>
    )
  }

  if (isLoading || !pet) {
    return (
      <div className="mx-auto max-w-lg px-4 py-12">
        <div className="space-y-3">
          <div className="h-6 w-40 animate-pulse rounded bg-sand-100" />
          <div className="h-40 animate-pulse rounded-2xl bg-sand-100" />
          <div className="h-10 animate-pulse rounded-xl bg-sand-100" />
        </div>
      </div>
    )
  }

  // ── Flyer data ──────────────────────────────────────────────────────────────

  /**
   * The flyer template is rendered in both states:
   * - Before assets: rendered with null image URLs (placeholder flyer, already in DOM)
   * - After assets: re-rendered with data URLs (html2canvas captures this version)
   * This ensures flyerRef is always attached so html2canvas never fails on a null ref.
   */
  const flyerData: SearchFlyerData = {
    pet: {
      id: pet.id,
      name: pet.name,
      species: pet.species,
      breed: pet.breed,
      photoUrl: pet.photoUrl,
    },
    lastSeenAt: routeState.lastSeenAt,
    description: routeState.description,
    petPhotoDataUrl: assets?.petPhotoDataUrl ?? null,
    recentPhotoDataUrl: assets?.recentPhotoDataUrl ?? null,
    qrCodeDataUrl: assets?.qrCodeDataUrl ?? null,
    baseUrl: window.location.origin,
  }

  /**
   * Social-share image (1200×630).
   * Uses the same pre-fetched assets as the print flyer — no extra network
   * requests required.  The `recentPhotoDataUrl` (latest field photo) takes
   * priority over the profile photo for maximum visual recognition.
   */
  const socialImageData: SocialShareImageData = {
    pet: {
      id: pet.id,
      name: pet.name,
      species: pet.species,
      breed: pet.breed,
      photoUrl: pet.photoUrl,
    },
    petPhotoDataUrl: assets?.recentPhotoDataUrl ?? assets?.petPhotoDataUrl ?? null,
    qrCodeDataUrl: assets?.qrCodeDataUrl ?? null,
    baseUrl: window.location.origin,
  }

  // ── Search radius ────────────────────────────────────────────────────────────

  const heuristicRadius = estimateSearchRadius(
    pet.species,
    pet.breed,
    hoursElapsedSince(routeState.lastSeenAt),
  )

  const { data: localRecoveryStats } = useRecoveryRates({
    species: pet.species,
    breed: pet.breed,
    canton: null,
  })

  const searchRadiusMetres = resolveSearchRadiusWithLocalStats(
    heuristicRadius,
    localRecoveryStats?.p90DistanceMeters,
  )
  const searchRadiusLabel = formatRadius(searchRadiusMetres)

  // ── Handlers ────────────────────────────────────────────────────────────────

  const isCapturing = flyerState === 'loading' || captureIntent !== null

  const handleDownload = () => {
    setShareError(null)
    setCaptureIntent('download')
    // If assets are not loaded yet, ensure the fetch starts
    if (assets === null) void prepareAssets()
  }

  const handleShare = () => {
    setShareError(null)
    setShareSuccess(false)
    setCaptureIntent('share')
    if (assets === null) void prepareAssets()
  }

  const handleSocialDownload = () => {
    setShareError(null)
    setCaptureIntent('social-download')
    if (assets === null) void prepareAssets()
  }

  const handleSocialShare = () => {
    setShareError(null)
    setShareSuccess(false)
    setCaptureIntent('social-share')
    if (assets === null) void prepareAssets()
  }

  // ── Render ──────────────────────────────────────────────────────────────────

  return (
    <div className="mx-auto max-w-lg px-4 py-8">

      {/* ── Success header ────────────────────────────────────────────────── */}
      <div className="mb-6 rounded-2xl border border-rescue-200 bg-rescue-50 p-5 text-center">
        <div className="mb-2 text-4xl" aria-hidden="true">✅</div>
        <h1 className="text-lg font-bold text-rescue-800">Reporte enviado correctamente</h1>
        <p className="mt-1 text-sm text-rescue-700">
          {pet.name} ha sido marcado como perdido. Te notificaremos cuando alguien reporte un avistamiento.
        </p>
      </div>

      {/* ── Emergency mode CTA ───────────────────────────────────────────── */}
      <EmergencyModeButton emergencyMode={emergencyMode} className="mb-6" />

      {/* ── Pet summary card ──────────────────────────────────────────────── */}
      <div className="mb-6 flex items-center gap-4 rounded-2xl border border-sand-200 bg-white p-4">
        {pet.photoUrl ? (
          <img
            src={pet.photoUrl}
            alt={pet.name}
            className="size-16 shrink-0 rounded-xl object-cover"
          />
        ) : (
          <div className="flex size-16 shrink-0 items-center justify-center rounded-xl bg-brand-50 text-3xl">
            {pet.species === 'Dog' ? '🐶' : pet.species === 'Cat' ? '🐱' : '🐾'}
          </div>
        )}
        <div>
          <p className="font-bold text-sand-900">{pet.name}</p>
          <p className="text-sm text-sand-500">
            {({ Dog: 'Perro', Cat: 'Gato', Bird: 'Ave', Rabbit: 'Conejo', Other: 'Otra' }[pet.species] ?? pet.species)}
            {pet.breed ? ` · ${pet.breed}` : ''}
          </p>
        </div>
      </div>

      {/* ── Search radius advisory ─────────────────────────────────────────── */}
      <div
        className="mb-6 rounded-2xl border border-warn-200 bg-warn-50 p-4"
        role="region"
        aria-label="Radio de búsqueda estimado"
      >
        <p className="text-sm font-bold text-warn-800">
          📍 Prioriza buscar en un radio de {searchRadiusLabel}
        </p>
        <p className="mt-1 text-xs text-warn-700">
          Según la especie de {pet.name} y el tiempo transcurrido, es más probable encontrarlo
          en un radio de <strong>{searchRadiusLabel}</strong> del punto de pérdida.
        </p>
      </div>

      {/* ── Flyer section ────────────────────────────────────────────────── */}
      <div className="mb-6 rounded-2xl border border-sand-200 bg-sand-50 p-5">
        <h2 className="mb-1 text-sm font-bold text-sand-800">📄 Flyer de búsqueda</h2>
        <p className="mb-4 text-xs text-sand-500">
          Descarga un flyer listo para imprimir o enviar por WhatsApp con todos los datos de{' '}
          {pet.name}.
        </p>

        {/* Feedback messages */}
        {errorMessage && (
          <div role="alert" className="mb-3 rounded-xl bg-danger-50 px-4 py-3 text-xs text-danger-600">
            {errorMessage}
          </div>
        )}
        {shareError && (
          <div role="alert" className="mb-3 rounded-xl bg-brand-50 px-4 py-3 text-xs text-brand-700">
            {shareError}
          </div>
        )}
        {shareSuccess && (
          <div className="mb-3 rounded-xl bg-rescue-50 px-4 py-3 text-xs text-rescue-700">
            ✅ Flyer compartido con éxito
          </div>
        )}

        <div className="flex flex-col gap-3 sm:flex-row">
          <button
            type="button"
            onClick={handleDownload}
            disabled={isCapturing}
            className="flex flex-1 items-center justify-center gap-2 rounded-xl bg-danger-600 px-4 py-3 text-sm font-semibold text-white hover:bg-danger-700 disabled:opacity-60"
            aria-label="Descargar flyer de búsqueda como imagen PNG"
          >
            {isCapturing && captureIntent === 'download' ? (
              <>
                <span
                  className="inline-block size-4 animate-spin rounded-full border-2 border-white border-t-transparent"
                  aria-hidden="true"
                />
                Generando…
              </>
            ) : (
              '📥 Descargar flyer (imprimir)'
            )}
          </button>

          <button
            type="button"
            onClick={handleShare}
            disabled={isCapturing}
            className="flex flex-1 items-center justify-center gap-2 rounded-xl border border-sand-300 bg-white px-4 py-3 text-sm font-semibold text-sand-700 hover:bg-sand-50 disabled:opacity-60"
            aria-label="Compartir flyer por WhatsApp u otras aplicaciones"
          >
            {isCapturing && captureIntent === 'share' ? (
              <>
                <span
                  className="inline-block size-4 animate-spin rounded-full border-2 border-sand-400 border-t-transparent"
                  aria-hidden="true"
                />
                Preparando…
              </>
            ) : (
              '📤 Compartir flyer'
            )}
          </button>
        </div>
      </div>

      {/* ── Social-share image section ───────────────────────────────────── */}
      <div className="mb-6 rounded-2xl border border-indigo-100 bg-gradient-to-br from-indigo-50 to-violet-50 p-5">
        <h2 className="mb-1 text-sm font-bold text-indigo-900">📲 Imagen para WhatsApp / Redes sociales</h2>
        <p className="mb-4 text-xs text-indigo-700">
          Formato horizontal 1200×630 optimizado para preview en WhatsApp, Telegram e Instagram.
          La foto ocupa el 60% de la imagen para máximo impacto en el feed.
        </p>

        <div className="flex flex-col gap-3 sm:flex-row">
          <button
            type="button"
            onClick={handleSocialDownload}
            disabled={isCapturing}
            className="flex flex-1 items-center justify-center gap-2 rounded-xl bg-indigo-600 px-4 py-3 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-60"
            aria-label="Descargar imagen optimizada para redes sociales"
          >
            {isCapturing && captureIntent === 'social-download' ? (
              <>
                <span
                  className="inline-block size-4 animate-spin rounded-full border-2 border-white border-t-transparent"
                  aria-hidden="true"
                />
                Generando…
              </>
            ) : (
              '📲 Descargar imagen (redes)'
            )}
          </button>

          <button
            type="button"
            onClick={handleSocialShare}
            disabled={isCapturing}
            className="flex flex-1 items-center justify-center gap-2 rounded-xl border border-indigo-200 bg-white px-4 py-3 text-sm font-semibold text-indigo-700 hover:bg-indigo-50 disabled:opacity-60"
            aria-label="Compartir imagen para redes sociales directamente"
          >
            {isCapturing && captureIntent === 'social-share' ? (
              <>
                <span
                  className="inline-block size-4 animate-spin rounded-full border-2 border-indigo-400 border-t-transparent"
                  aria-hidden="true"
                />
                Preparando…
              </>
            ) : (
              '📤 Compartir directo'
            )}
          </button>
        </div>

        <p className="mt-3 text-xs text-indigo-500">
          💡 Guarda la imagen y pégala directamente en WhatsApp, Facebook o Instagram Stories.
        </p>
      </div>

      {/* ── Quick actions ────────────────────────────────────────────────── */}
      <div className="mb-6 grid grid-cols-2 gap-3">
        <Link
          to={`/pets/${pet.id}`}
          className="flex items-center justify-center rounded-xl border border-sand-200 bg-white px-4 py-3 text-center text-sm font-medium text-sand-700 hover:bg-sand-50"
        >
          Ver perfil de {pet.name}
        </Link>
        <Link
          to="/dashboard"
          className="flex items-center justify-center rounded-xl border border-sand-200 bg-white px-4 py-3 text-sm font-medium text-sand-700 hover:bg-sand-50"
        >
          ← Mis mascotas
        </Link>
      </div>

      {/* ── Share profile ─────────────────────────────────────────────────── */}
      <SharePetButton
        petId={pet.id}
        petName={pet.name}
        context={`Se perdió el ${new Date(routeState.lastSeenAt).toLocaleDateString('es-CR', { day: 'numeric', month: 'long' })}.`}
        variant="outline"
        className="mb-4"
      />

      {/* ── Action checklist ───────────────────────────────────────────── */}
      <section ref={checklistRef}>
        <SearchChecklist
          lostEventId={routeState.lostEventId}
          petName={pet.name}
          className="mb-6"
        />
      </section>
      {/* ── Multi-channel broadcast ────────────────────────────────────── */}
      <BroadcastPanel lostEventId={routeState.lostEventId} className="mb-6" />
      {/* ── Hidden templates (always mounted, updated when assets load) ─── */}
      <SearchFlyerTemplate ref={flyerRef} data={flyerData} />
      <SocialShareImageTemplate ref={socialRef} data={socialImageData} />
    </div>
  )
}

