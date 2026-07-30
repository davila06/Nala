import { useEffect, useRef, useState } from "react";
import { Link, useLocation, useParams } from "react-router-dom";
import { motion } from "framer-motion";
import { BroadcastPanel } from "../components/BroadcastPanel";
import { EmergencyModeButton } from "../components/EmergencyModeButton";
import { SearchChecklist } from "../components/SearchChecklist";
import {
  SearchFlyerTemplate,
  type SearchFlyerData,
} from "../components/SearchFlyerTemplate";
import {
  SocialShareImageTemplate,
  type SocialShareImageData,
} from "../components/SocialShareImageTemplate";
import { SharePetButton } from "../components/SharePetButton";
import { useEmergencyMode } from "../hooks/useEmergencyMode";
import { useGenerateFlyer } from "../hooks/useGenerateFlyer";
import {
  estimateSearchRadius,
  formatRadius,
  hoursElapsedSince,
  resolveSearchRadiusWithLocalStats,
} from "../utils/searchRadius";
import { useRecoveryRates } from "../hooks/useRecoveryStats";
import { usePetDetail } from "@/features/pets/hooks/usePets";

// ── Route state ────────────────────────────────────────────────────────────────

interface ConfirmationRouteState {
  lostEventId: string;
  lastSeenAt: string;
  description: string | null;
  recentPhotoUrl: string | null;
}

function isValidState(s: unknown): s is ConfirmationRouteState {
  return (
    typeof s === "object" &&
    s !== null &&
    typeof (s as Record<string, unknown>).lostEventId === "string" &&
    typeof (s as Record<string, unknown>).lastSeenAt === "string"
  );
}

// ── Component ──────────────────────────────────────────────────────────────────

export default function LostReportConfirmationPage() {
  const { id } = useParams<{ id: string }>();
  const location = useLocation();

  const routeState = isValidState(location.state) ? location.state : null;

  const { data: pet, isLoading } = usePetDetail(id ?? "");

  const flyerRef = useRef<HTMLDivElement>(null);
  const socialRef = useRef<HTMLDivElement>(null);
  const checklistRef = useRef<HTMLElement>(null);

  const [shareError, setShareError] = useState<string | null>(null);
  const [shareSuccess, setShareSuccess] = useState(false);

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
    "download" | "share" | "social-download" | "social-share" | null
  >(null);

  const {
    state: flyerState,
    assets,
    prepareAssets,
    downloadFlyer,
    buildFlyerBlob,
    downloadSocialImage,
    buildSocialImageBlob,
    errorMessage,
  } = useGenerateFlyer(
    id ?? "",
    pet?.name ?? "",
    pet?.photoUrl ?? null,
    routeState?.recentPhotoUrl ?? null,
  );

  const emergencyMode = useEmergencyMode({
    petId: id ?? "",
    petName: pet?.name ?? "",
    flyerHook: { prepareAssets, buildFlyerBlob, assets, state: flyerState },
    flyerRef,
    checklistRef,
  });

  // Pre-fetch assets as soon as the page mounts so they're usually ready
  // by the time the user clicks the download button.
  useEffect(() => {
    void prepareAssets();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Execute capture AFTER React has committed the updated flyerData to the DOM.
  // The effect depends on `assets` so it re-fires when assets become non-null.
  useEffect(() => {
    if (captureIntent === null || assets === null || !pet || !routeState)
      return;

    const intent = captureIntent;
    let cancelled = false;

    const execute = async () => {
      // Allow two animation frames + 120 ms for images to fully paint
      await new Promise<void>((resolve) => {
        requestAnimationFrame(() =>
          requestAnimationFrame(() => setTimeout(resolve, 120)),
        );
      });
      if (cancelled) return;

      if (intent === "download") {
        await downloadFlyer(flyerRef);
      } else if (intent === "share") {
        if (!navigator.share) {
          setShareError(
            "Tu dispositivo no soporta compartir directamente. Descarga el flyer y compártelo manualmente.",
          );
          return;
        }
        try {
          const blob = await buildFlyerBlob(flyerRef);
          const safeName = pet.name.toLowerCase().replace(/\s+/g, "-");
          const file = new File([blob], `flyer-${safeName}.png`, {
            type: "image/png",
          });
          await navigator.share({
            title: `¡Ayuda a encontrar a ${pet.name}!`,
            text: `${pet.name} está perdido. Si lo ves, escanea su QR o contacta al dueño.`,
            url: `${window.location.origin}/p/${pet.id}`,
            files: [file],
          });
          setShareSuccess(true);
        } catch (err) {
          if (err instanceof Error && err.name === "AbortError") return;
          setShareError(
            "No se pudo compartir el flyer. Intenta descargarlo manualmente.",
          );
        }
      } else if (intent === "social-download") {
        await downloadSocialImage(socialRef);
      } else if (intent === "social-share") {
        if (!navigator.share) {
          setShareError(
            "Tu dispositivo no soporta compartir directamente. Descarga la imagen y compártela manualmente.",
          );
          return;
        }
        try {
          const blob = await buildSocialImageBlob(socialRef);
          const safeName = pet.name.toLowerCase().replace(/\s+/g, "-");
          const file = new File([blob], `alerta-${safeName}-social.png`, {
            type: "image/png",
          });
          await navigator.share({
            title: `¡Ayuda a encontrar a ${pet.name}!`,
            text: `${pet.name} está perdido. ¿Lo has visto?`,
            url: `${window.location.origin}/p/${pet.id}`,
            files: [file],
          });
          setShareSuccess(true);
        } catch (err) {
          if (err instanceof Error && err.name === "AbortError") return;
          setShareError(
            "No se pudo compartir la imagen. Intenta descargarla manualmente.",
          );
        }
      }

      setCaptureIntent(null);
    };

    void execute();
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [captureIntent, assets]);

  // ── Guard: invalid navigation ───────────────────────────────────────────────

  if (!routeState) {
    return (
      <div className="mx-auto max-w-lg px-4 py-12 text-center">
        <p className="text-sand-500">Página no disponible directamente.</p>
        <Link
          to="/dashboard"
          className="mt-4 inline-block rounded text-sm text-brand-600 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
        >
          ← Volver al inicio
        </Link>
      </div>
    );
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
    );
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
  };

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
    petPhotoDataUrl:
      assets?.recentPhotoDataUrl ?? assets?.petPhotoDataUrl ?? null,
    qrCodeDataUrl: assets?.qrCodeDataUrl ?? null,
    baseUrl: window.location.origin,
  };

  // ── Search radius ────────────────────────────────────────────────────────────

  const heuristicRadius = estimateSearchRadius(
    pet.species,
    pet.breed,
    hoursElapsedSince(routeState.lastSeenAt),
  );

  const { data: localRecoveryStats } = useRecoveryRates({
    species: pet.species,
    breed: pet.breed,
    canton: null,
  });

  const searchRadiusMetres = resolveSearchRadiusWithLocalStats(
    heuristicRadius,
    localRecoveryStats?.p90DistanceMeters,
  );
  const searchRadiusLabel = formatRadius(searchRadiusMetres);

  // ── Handlers ────────────────────────────────────────────────────────────────

  const isCapturing = flyerState === "loading" || captureIntent !== null;

  const handleDownload = () => {
    setShareError(null);
    setCaptureIntent("download");
    // If assets are not loaded yet, ensure the fetch starts
    if (assets === null) void prepareAssets();
  };

  const handleShare = () => {
    setShareError(null);
    setShareSuccess(false);
    setCaptureIntent("share");
    if (assets === null) void prepareAssets();
  };

  const handleSocialDownload = () => {
    setShareError(null);
    setCaptureIntent("social-download");
    if (assets === null) void prepareAssets();
  };

  const handleSocialShare = () => {
    setShareError(null);
    setShareSuccess(false);
    setCaptureIntent("social-share");
    if (assets === null) void prepareAssets();
  };

  // ── Render ──────────────────────────────────────────────────────────────────

  return (
    <div className="mx-auto max-w-lg px-4 py-8">
      {/* ── Command Center header — emotional, impactful ───────────────── */}
      <motion.div
        initial={{ opacity: 0, y: -12 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.35, ease: [0.4, 0, 0.2, 1] }}
        className="mb-6 overflow-hidden rounded-2xl border-2 border-danger-300 bg-gradient-to-br from-danger-600 to-danger-700 shadow-xl shadow-danger-900/20"
      >
        {/* Pulsing top bar */}
        <div className="flex items-center justify-center gap-2 bg-danger-800/50 py-2">
          <span className="relative flex h-2.5 w-2.5">
            <span className="absolute inline-flex h-full w-full animate-ping rounded-full field-input opacity-75" />
            <span className="relative inline-flex h-2.5 w-2.5 rounded-full bg-white" />
          </span>
          <span className="text-xs font-bold uppercase tracking-[0.2em] text-white">
            Alerta activa
          </span>
        </div>

        <div className="p-6 text-center">
          <p className="font-display text-3xl font-bold text-white drop-shadow">
            🚨 {pet.name} está perdido
          </p>
          <p className="mt-2 text-sm text-danger-200 leading-relaxed">
            La alerta fue enviada. Cuanto antes actúes, mayor es la probabilidad
            de encontrarlo.
          </p>

          {/* Quick action checklist */}
          <div className="mt-5 rounded-xl bg-white/10 backdrop-blur-sm p-4 text-left space-y-2.5">
            {[
              { done: true, label: "Reporte enviado y alerta activada" },
              { done: false, label: "Comparte el flyer con vecinos y grupos" },
              { done: false, label: "Revisa el mapa de avistamientos" },
              { done: false, label: "Coloca carteles en el radio de búsqueda" },
            ].map((item) => (
              <div key={item.label} className="flex items-start gap-2.5">
                <span
                  className={`flex-shrink-0 text-sm ${item.done ? "text-rescue-300" : "text-white/50"}`}
                >
                  {item.done ? "✓" : "○"}
                </span>
                <span
                  className={`text-xs leading-snug ${item.done ? "text-rescue-200 line-through decoration-rescue-400" : "text-white"}`}
                >
                  {item.label}
                </span>
              </div>
            ))}
          </div>
        </div>
      </motion.div>

      {/* ── Emergency mode CTA ───────────────────────────────────────────── */}
      <EmergencyModeButton emergencyMode={emergencyMode} className="mb-6" />

      {/* ── Pet summary card ──────────────────────────────────────────────── */}
      <div className="mb-6 flex items-center gap-4 rounded-2xl border border-sand-200 field-input p-4">
        {pet.photoUrl ? (
          <img
            src={pet.photoUrl}
            alt={pet.name}
            className="size-16 shrink-0 rounded-xl object-cover"
          />
        ) : (
          <div className="flex size-16 shrink-0 items-center justify-center rounded-xl bg-brand-50 text-3xl">
            {pet.species === "Dog" ? "🐶" : pet.species === "Cat" ? "🐱" : "🐾"}
          </div>
        )}
        <div>
          <p className="font-bold text-sand-900">{pet.name}</p>
          <p className="text-sm text-sand-500">
            {{
              Dog: "Perro",
              Cat: "Gato",
              Bird: "Ave",
              Rabbit: "Conejo",
              Other: "Otra",
            }[pet.species] ?? pet.species}
            {pet.breed ? ` · ${pet.breed}` : ""}
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
          Según la especie de {pet.name} y el tiempo transcurrido, es más
          probable encontrarlo en un radio de{" "}
          <strong>{searchRadiusLabel}</strong> del punto de pérdida.
        </p>
      </div>

      {/* ── Flyer section ────────────────────────────────────────────────── */}
      <div className="mb-6 rounded-2xl border border-sand-200 bg-sand-50 p-5">
        <h2 className="mb-1 text-sm font-bold text-sand-800">
          📄 Flyer de búsqueda
        </h2>
        <p className="mb-4 text-xs text-sand-500">
          Descarga un flyer listo para imprimir o enviar por WhatsApp con todos
          los datos de {pet.name}.
        </p>

        {/* Feedback messages */}
        {errorMessage && (
          <div
            role="alert"
            className="mb-3 rounded-xl bg-danger-50 px-4 py-3 text-xs text-danger-600"
          >
            {errorMessage}
          </div>
        )}
        {shareError && (
          <div
            role="alert"
            className="mb-3 rounded-xl bg-brand-50 px-4 py-3 text-xs text-brand-700"
          >
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
            {isCapturing && captureIntent === "download" ? (
              <>
                <span
                  className="inline-block size-4 animate-spin rounded-full border-2 border-white border-t-transparent"
                  aria-hidden="true"
                />
                Generando…
              </>
            ) : (
              "📥 Descargar flyer (imprimir)"
            )}
          </button>

          <button
            type="button"
            onClick={handleShare}
            disabled={isCapturing}
            className="flex flex-1 items-center justify-center gap-2 rounded-xl border border-sand-300 field-input px-4 py-3 text-sm font-semibold text-sand-700 hover:bg-sand-50 disabled:opacity-60"
            aria-label="Compartir flyer por WhatsApp u otras aplicaciones"
          >
            {isCapturing && captureIntent === "share" ? (
              <>
                <span
                  className="inline-block size-4 animate-spin rounded-full border-2 border-sand-400 border-t-transparent"
                  aria-hidden="true"
                />
                Preparando…
              </>
            ) : (
              "📤 Compartir flyer"
            )}
          </button>
        </div>
      </div>

      {/* ── Social-share image section ───────────────────────────────────── */}
      <div className="mb-6 rounded-2xl border border-indigo-100 bg-gradient-to-br from-indigo-50 to-violet-50 p-5">
        <h2 className="mb-1 text-sm font-bold text-indigo-900">
          📲 Imagen para WhatsApp / Redes sociales
        </h2>
        <p className="mb-4 text-xs text-indigo-700">
          Formato horizontal 1200×630 optimizado para preview en WhatsApp,
          Telegram e Instagram. La foto ocupa el 60% de la imagen para máximo
          impacto en el feed.
        </p>

        <div className="flex flex-col gap-3 sm:flex-row">
          <button
            type="button"
            onClick={handleSocialDownload}
            disabled={isCapturing}
            className="flex flex-1 items-center justify-center gap-2 rounded-xl bg-indigo-600 px-4 py-3 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-60"
            aria-label="Descargar imagen optimizada para redes sociales"
          >
            {isCapturing && captureIntent === "social-download" ? (
              <>
                <span
                  className="inline-block size-4 animate-spin rounded-full border-2 border-white border-t-transparent"
                  aria-hidden="true"
                />
                Generando…
              </>
            ) : (
              "📲 Descargar imagen (redes)"
            )}
          </button>

          <button
            type="button"
            onClick={handleSocialShare}
            disabled={isCapturing}
            className="flex flex-1 items-center justify-center gap-2 rounded-xl border border-indigo-200 field-input px-4 py-3 text-sm font-semibold text-indigo-700 hover:bg-indigo-50 disabled:opacity-60"
            aria-label="Compartir imagen para redes sociales directamente"
          >
            {isCapturing && captureIntent === "social-share" ? (
              <>
                <span
                  className="inline-block size-4 animate-spin rounded-full border-2 border-indigo-400 border-t-transparent"
                  aria-hidden="true"
                />
                Preparando…
              </>
            ) : (
              "📤 Compartir directo"
            )}
          </button>
        </div>

        <p className="mt-3 text-xs text-indigo-500">
          💡 Guarda la imagen y pégala directamente en WhatsApp, Facebook o
          Instagram Stories.
        </p>
      </div>

      {/* ── WhatsApp urgency share ───────────────────────────────────────── */}
      {(() => {
        const profileUrl = `${window.location.origin}/p/${pet.id}`;
        const hours = hoursElapsedSince(routeState.lastSeenAt);
        const urgencyEmoji = hours < 6 ? "🚨🚨🚨" : hours < 24 ? "🚨🚨" : "🚨";
        const urgencyText =
          hours < 6
            ? `URGENTE — hace menos de 6 horas`
            : hours < 24
              ? `URGENTE — desapareció hoy`
              : `Por favor ayuda a encontrarlo`;
        const msg = encodeURIComponent(
          `${urgencyEmoji} ${urgencyText}!\n\n*${pet.name}* (${pet.species === "Dog" ? "perro" : pet.species === "Cat" ? "gato" : pet.species}) está perdido.\n\nEscanea su perfil QR o entra aquí:\n👉 ${profileUrl}\n\n¿Lo has visto? Por favor contáctame.`,
        );
        return (
          <a
            href={`https://wa.me/?text=${msg}`}
            target="_blank"
            rel="noopener noreferrer"
            className="mb-4 flex w-full items-center justify-center gap-3 rounded-2xl bg-[#25D366] px-5 py-4 text-base font-bold text-white shadow-lg shadow-[#25D366]/30 transition-all hover:-translate-y-0.5 hover:bg-[#1ebe5a] hover:shadow-xl focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#25D366] focus-visible:ring-offset-2"
            aria-label={`Compartir alerta de ${pet.name} por WhatsApp`}
          >
            <svg
              viewBox="0 0 24 24"
              className="h-6 w-6 fill-current"
              aria-hidden="true"
            >
              <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z" />
            </svg>
            Compartir por WhatsApp{" "}
            {hours < 6 ? "— ¡URGENTE!" : hours < 24 ? "— Hoy" : ""}
          </a>
        );
      })()}

      {/* ── Quick actions ────────────────────────────────────────────────── */}
      <div className="mb-6 grid grid-cols-2 gap-3">
        <Link
          to={`/pets/${pet.id}`}
          className="flex items-center justify-center rounded-xl border border-sand-200 field-input px-4 py-3 text-center text-sm font-medium text-sand-700 hover:bg-sand-50"
        >
          Ver perfil de {pet.name}
        </Link>
        <Link
          to="/dashboard"
          className="flex items-center justify-center rounded-xl border border-sand-200 field-input px-4 py-3 text-sm font-medium text-sand-700 hover:bg-sand-50"
        >
          ← Mis mascotas
        </Link>
      </div>

      {/* ── Share profile ─────────────────────────────────────────────────── */}
      <SharePetButton
        petId={pet.id}
        petName={pet.name}
        context={`Se perdió el ${new Date(routeState.lastSeenAt).toLocaleDateString("es-CR", { day: "numeric", month: "long" })}.`}
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
  );
}
