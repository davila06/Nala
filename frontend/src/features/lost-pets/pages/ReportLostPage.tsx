import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { AnimatePresence, motion } from "framer-motion";
import { usePetDetail } from "@/features/pets/hooks/usePets";
import { PhotoUpload } from "@/features/pets/components/PhotoUpload";
import { useReportLost } from "../hooks/useLostPets";
import { useGeolocation } from "../hooks/useGeolocation";
import { LastSeenMap } from "../components/LastSeenMap";
import { useNeighborCountInArea } from "@/features/locations/hooks/useNeighbor";
import {
  estimateSearchRadius,
  hoursElapsedSince,
  resolveSearchRadiusWithLocalStats,
} from "../utils/searchRadius";
import { useRecoveryRates } from "../hooks/useRecoveryStats";
import { addQueuedReport } from "@/shared/lib/offlineQueue";
import { Skeleton } from "@/shared/ui/Spinner";
import type { LastSeenCoords } from "../components/LastSeenMap";

/** datetime-local inputs need LOCAL time, not UTC */
const toLocalDatetime = (d: Date) =>
  new Date(d.getTime() - d.getTimezoneOffset() * 60000)
    .toISOString()
    .slice(0, 16);

export default function ReportLostPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: pet, isLoading } = usePetDetail(id ?? "");
  const { mutateAsync: reportLost, isPending, error } = useReportLost();
  const geo = useGeolocation();

  // ── All hooks must be called before any early return (Rules of Hooks) ─────
  // useRecoveryRates depends on pet data; use safe defaults while loading.
  const { data: localRecoveryStats } = useRecoveryRates({
    species: pet?.species ?? "Dog",
    breed: pet?.breed ?? null,
    canton: null,
  });

  const [description, setDescription] = useState("");
  const [publicMessage, setPublicMessage] = useState("");
  const [lastSeenAt, setLastSeenAt] = useState(() =>
    toLocalDatetime(new Date()),
  );
  const [coords, setCoords] = useState<LastSeenCoords | null>(null);
  const [recentPhoto, setRecentPhoto] = useState<File | null>(null);
  const [contactName, setContactName] = useState("");
  const [contactPhone, setContactPhone] = useState("");
  const [rewardAmount, setRewardAmount] = useState("");
  const [rewardNote, setRewardNote] = useState("");
  const [queuedOffline, setQueuedOffline] = useState(false);
  const [isQueuingOffline, setIsQueuingOffline] = useState(false);

  const { data: neighborCount } = useNeighborCountInArea(
    coords?.lat, coords?.lng, 500,
  );

  // Auto-request geolocation on mount and seed the pin with the first fix
  useEffect(() => {
    geo.request();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []); // Only on mount — geo.request is stable (useCallback)

  // Once geolocation resolves, auto-place the pin at the user's position
  // (only if the user hasn't already placed it manually)
  useEffect(() => {
    if (geo.status === "granted" && geo.coords && !coords) {
      setCoords(geo.coords);
    }
  }, [geo.status, geo.coords, coords]);

  if (isLoading) {
    return (
      <div className="mx-auto max-w-lg space-y-4 px-4 py-10">
        <Skeleton className="h-5 w-36 rounded" />
        <Skeleton className="h-6 w-48 rounded" />
        {/* Map placeholder */}
        <Skeleton className="h-64 rounded-2xl" />
        {/* datetime field */}
        <Skeleton className="h-10 rounded-xl" />
        {/* description textarea */}
        <Skeleton className="h-24 rounded-xl" />
        {/* contact fields */}
        <Skeleton className="h-10 rounded-xl" />
        <Skeleton className="h-10 rounded-xl" />
        {/* submit */}
        <Skeleton className="h-12 rounded-2xl" />
      </div>
    );
  }

  if (!pet) {
    return (
      <div className="mx-auto max-w-lg px-4 py-10 text-center">
        <p className="text-sand-500">Mascota no encontrada.</p>
        <Link
          to="/dashboard"
          className="mt-4 inline-block text-sm text-brand-600 hover:underline"
        >
          ← Volver
        </Link>
      </div>
    );
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // ── Offline path: persist to IndexedDB queue ──────────────────────────
    if (!navigator.onLine) {
      setIsQueuingOffline(true);
      try {
        await addQueuedReport({
          id: crypto.randomUUID(),
          petId: pet.id,
          petName: pet.name,
          capturedAt: new Date().toISOString(),
          lastSeenAt: new Date(lastSeenAt).toISOString(),
          lastSeenLat: coords?.lat ?? null,
          lastSeenLng: coords?.lng ?? null,
          description: description.trim() || null,
          publicMessage: publicMessage.trim() || null,
          contactName: contactName.trim() || null,
          contactPhone: contactPhone.trim() || null,
          photoBlob: recentPhoto,
        });
        setQueuedOffline(true);
      } finally {
        setIsQueuingOffline(false);
      }
      return;
    }

    // ── Online path: submit immediately ────────────────────────────────────
    try {
      const result = await reportLost({
        petId: pet.id,
        description: description.trim() || null,
        publicMessage: publicMessage.trim() || null,
        lastSeenAt: new Date(lastSeenAt).toISOString(),
        lastSeenLat: coords?.lat ?? null,
        lastSeenLng: coords?.lng ?? null,
        recentPhoto,
        contactName: contactName.trim() || null,
        contactPhone: contactPhone.trim() || null,
        rewardAmount: rewardAmount !== "" ? parseFloat(rewardAmount) : null,
        rewardNote: rewardNote.trim() || null,
      });
      const recentPhotoUrl = recentPhoto
        ? URL.createObjectURL(recentPhoto)
        : null;
      navigate(`/pets/${pet.id}/lost-confirmed`, {
        state: {
          lostEventId: result.id,
          lastSeenAt: new Date(lastSeenAt).toISOString(),
          description: description.trim() || null,
          recentPhotoUrl,
        },
      });
    } catch {
      // error state is handled by the mutation's `error` object shown in the UI
    }
  };

  const heuristicRadius = estimateSearchRadius(
    pet.species,
    pet.breed,
    hoursElapsedSince(new Date(lastSeenAt).toISOString()),
  );

  const estimatedRadius = resolveSearchRadiusWithLocalStats(
    heuristicRadius,
    localRecoveryStats?.p90DistanceMeters,
  );

  // ── Wizard state ──────────────────────────────────────────────────────────
  const [step, setStep] = useState<1 | 2 | 3>(1);
  const [direction, setDirection] = useState<1 | -1>(1);

  const goNext = () => {
    setDirection(1);
    setStep((s) => Math.min(3, s + 1) as 1 | 2 | 3);
  };
  const goBack = () => {
    setDirection(-1);
    setStep((s) => Math.max(1, s - 1) as 1 | 2 | 3);
  };

  const STEPS = [
    { id: 1, label: "Cuándo y dónde", emoji: "📍" },
    { id: 2, label: "Tu mascota", emoji: "🐾" },
    { id: 3, label: "Contacto", emoji: "📞" },
  ];

  const slideVariants = {
    enter: (d: number) => ({ x: d > 0 ? 40 : -40, opacity: 0 }),
    center: { x: 0, opacity: 1 },
    exit: (d: number) => ({ x: d > 0 ? -40 : 40, opacity: 0 }),
  };

  return (
    <div className="mx-auto max-w-lg px-4 py-8">
      <Link
        to={`/pets/${pet.id}`}
        className="mb-5 flex items-center gap-1.5 rounded-lg text-sm text-sand-500 hover:text-sand-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
      >
        ← Volver a {pet.name}
      </Link>

      {/* ── Offline queued confirmation ───────────────────────────────── */}
      {queuedOffline ? (
        <div className="rounded-2xl border border-brand-200 bg-brand-50 p-6 text-center">
          <div className="mb-3 text-5xl" aria-hidden="true">
            📵
          </div>
          <h2 className="text-lg font-bold text-brand-800">
            Sin conexión — reporte guardado
          </h2>
          <p className="mt-2 text-sm text-brand-700">
            El reporte de <strong>{pet.name}</strong> quedó guardado en tu
            dispositivo. Se enviará automáticamente al recuperar conexión.
          </p>
          <Link
            to="/dashboard"
            className="mt-5 inline-block rounded-xl bg-brand-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-brand-700"
          >
            ← Volver al inicio
          </Link>
        </div>
      ) : (
        <>
          {/* ── Wizard header ───────────────────────────────────────── */}
          <div className="mb-6 rounded-2xl border border-danger-200 bg-linear-to-br from-danger-50 to-warn-50 p-5">
            <h1 className="text-lg font-bold text-danger-700">
              🚨 Reportar a {pet.name} como perdido
            </h1>
            <p className="mt-1 text-sm text-danger-600">
              Completa los 3 pasos. Cuanta más información, más rápido lo
              encontramos.
            </p>
          </div>

          {/* ── Step indicators ─────────────────────────────────────── */}
          <div className="mb-6 flex items-center gap-0">
            {STEPS.map((s, i) => {
              const isActive = s.id === step;
              const isDone = s.id < step;
              return (
                <div key={s.id} className="flex flex-1 items-center">
                  <div className="flex flex-col items-center flex-1">
                    <motion.div
                      animate={{
                        scale: isActive ? 1.1 : 1,
                        backgroundColor: isDone
                          ? "#17a26d"
                          : isActive
                            ? "#e8521e"
                            : "#e2d3c4",
                      }}
                      className="flex h-9 w-9 items-center justify-center rounded-full text-sm font-bold text-white shadow-sm"
                      transition={{
                        type: "spring",
                        stiffness: 400,
                        damping: 30,
                      }}
                    >
                      {isDone ? "✓" : s.emoji}
                    </motion.div>
                    <span
                      className={[
                        "mt-1 text-[10px] font-semibold",
                        isActive
                          ? "text-brand-600"
                          : isDone
                            ? "text-rescue-600"
                            : "text-sand-400",
                      ].join(" ")}
                    >
                      {s.label}
                    </span>
                  </div>
                  {i < STEPS.length - 1 && (
                    <div className="h-0.5 w-full flex-1 mx-1 rounded-full overflow-hidden bg-sand-200">
                      <motion.div
                        className="h-full bg-rescue-500 origin-left"
                        animate={{ scaleX: step > s.id ? 1 : 0 }}
                        transition={{ duration: 0.3 }}
                      />
                    </div>
                  )}
                </div>
              );
            })}
          </div>

          {error && (
            <div
              role="alert"
              className="mb-4 rounded-xl bg-danger-50 px-4 py-3 text-sm text-danger-600"
            >
              Ocurrió un error. Intenta de nuevo.
            </div>
          )}

          {/* ── Animated step panels ────────────────────────────────── */}
          <form onSubmit={handleSubmit}>
            <div className="overflow-hidden">
              <AnimatePresence mode="wait" custom={direction} initial={false}>
                <motion.div
                  key={step}
                  custom={direction}
                  variants={slideVariants}
                  initial="enter"
                  animate="center"
                  exit="exit"
                  transition={{ duration: 0.22, ease: [0.4, 0, 0.2, 1] }}
                >
                  {/* ══ STEP 1: Cuándo y Dónde ══════════════════════════════ */}
                  {step === 1 && (
                    <div className="space-y-5">
                      <div>
                        <label
                          htmlFor="lastSeenAt"
                          className="mb-1 block text-sm font-semibold text-sand-700"
                        >
                          ¿Cuándo fue visto por última vez?
                        </label>
                        <input
                          id="lastSeenAt"
                          type="datetime-local"
                          value={lastSeenAt}
                          onChange={(e) => setLastSeenAt(e.target.value)}
                          required
                          max={toLocalDatetime(new Date())}
                          className="w-full rounded-xl border border-sand-300 field-input px-4 py-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-200"
                        />
                      </div>

                      <div>
                        <div className="mb-2 flex items-center justify-between">
                          <span className="text-sm font-semibold text-sand-700">
                            📍 Última ubicación conocida
                          </span>
                          {coords ? (
                            <button
                              type="button"
                              onClick={() => setCoords(null)}
                              className="text-xs text-sand-400 underline hover:text-sand-600"
                            >
                              Quitar pin
                            </button>
                          ) : (
                            <span className="text-xs text-sand-400">
                              Opcional
                            </span>
                          )}
                        </div>

                        {geo.status === "requesting" && (
                          <div className="mb-2 flex items-center gap-2 rounded-lg bg-trust-50 px-3 py-2 text-xs text-trust-600">
                            <span className="inline-block h-3 w-3 animate-spin rounded-full border-2 border-trust-400 border-t-transparent" />
                            Obteniendo tu ubicación…
                          </div>
                        )}
                        {geo.status === "denied" && geo.error && (
                          <div className="mb-2 rounded-lg bg-brand-50 px-3 py-2 text-xs text-brand-700">
                            ⚠️ {geo.error}
                          </div>
                        )}

                        <LastSeenMap
                          value={coords}
                          onChange={setCoords}
                          userCoords={geo.coords}
                          geoStatus={geo.status}
                          petName={pet.name}
                          estimatedRadius={estimatedRadius}
                          className="h-64 w-full overflow-hidden rounded-2xl border border-sand-200 shadow-sm"
                        />
                        <p className="mt-1.5 text-xs text-sand-400">
                          {coords
                            ? `Pin en ${coords.lat.toFixed(5)}, ${coords.lng.toFixed(5)}`
                            : `Toca el mapa para marcar dónde fue visto ${pet.name}.`}
                        </p>

                        {/* Neighbor count hint — only when coords are set */}
                        {coords && neighborCount != null && (
                          <div className={`mt-2 flex items-center gap-2 rounded-xl px-3 py-2 text-xs font-medium ${
                            neighborCount.count > 0
                              ? "bg-trust-50 text-trust-800 border border-trust-200"
                              : "bg-sand-50 text-sand-500 border border-sand-100"
                          }`}>
                            <span aria-hidden="true">🏘️</span>
                            {neighborCount.count > 0
                              ? `${neighborCount.count} vecino${neighborCount.count !== 1 ? "s" : ""} activo${neighborCount.count !== 1 ? "s" : ""} en Guardia Vecinal cercano${neighborCount.count !== 1 ? "s" : ""}. Serán notificados automáticamente.`
                              : "Sé el primero en activar la Guardia Vecinal en esta zona."}
                          </div>
                        )}
                      </div>
                    </div>
                  )}

                  {/* ══ STEP 2: Tu mascota ══════════════════════════════════ */}
                  {step === 2 && (
                    <div className="space-y-5">
                      <div>
                        <p className="mb-1.5 text-sm font-semibold text-sand-700">
                          📷 Foto reciente (opcional)
                        </p>
                        <p className="mb-2 text-xs text-sand-500">
                          Se usará en el flyer de búsqueda y perfil público.
                        </p>
                        <PhotoUpload
                          value={recentPhoto}
                          onChange={setRecentPhoto}
                          disabled={isPending}
                        />
                      </div>

                      <div>
                        <label
                          htmlFor="description"
                          className="mb-1 block text-sm font-semibold text-sand-700"
                        >
                          Descripción (opcional)
                        </label>
                        <textarea
                          id="description"
                          value={description}
                          onChange={(e) => setDescription(e.target.value)}
                          maxLength={1000}
                          rows={4}
                          placeholder="Collar, señas particulares, zona específica…"
                          className="w-full resize-none rounded-xl border border-sand-300 field-input px-4 py-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-200"
                        />
                        <p className="mt-1 text-right text-xs text-sand-400">
                          {description.length}/1000
                        </p>
                      </div>

                      <div className="rounded-2xl border border-brand-200 bg-brand-50 p-4">
                        <label
                          htmlFor="publicMessage"
                          className="mb-0.5 block text-sm font-semibold text-brand-800"
                        >
                          💬 Mensaje para quien encuentre a {pet.name}
                        </label>
                        <p className="mb-2 text-xs text-brand-700">
                          Se mostrará en el perfil QR público.
                        </p>
                        <textarea
                          id="publicMessage"
                          value={publicMessage}
                          onChange={(e) => setPublicMessage(e.target.value)}
                          maxLength={200}
                          rows={3}
                          placeholder={`Si encontraste a ${pet.name}, por favor contáctame. ¡Muchas gracias!`}
                          className="w-full resize-none rounded-xl border border-brand-300 field-input px-4 py-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-200"
                        />
                        <p className="mt-1 text-right text-xs text-brand-600">
                          {publicMessage.length}/200
                        </p>
                      </div>

                      <div className="rounded-2xl border border-warn-200 bg-warn-50 p-4">
                        <p className="mb-0.5 text-sm font-semibold text-warn-800">
                          🏅 Recompensa (opcional)
                        </p>
                        <p className="mb-3 text-xs text-warn-700">
                          No se gestiona dentro de la plataforma.
                        </p>
                        <div className="space-y-3">
                          <div>
                            <label
                              htmlFor="rewardAmount"
                              className="mb-1 block text-xs font-medium text-warn-800"
                            >
                              Monto (₡)
                            </label>
                            <input
                              id="rewardAmount"
                              type="number"
                              min={1}
                              max={10_000_000}
                              step={1000}
                              value={rewardAmount}
                              onChange={(e) => setRewardAmount(e.target.value)}
                              placeholder="Ej. 50000"
                              className="w-full rounded-xl border border-warn-300 field-input px-4 py-2.5 text-sm focus:border-warn-500 focus:outline-none focus:ring-2 focus:ring-warn-200"
                            />
                          </div>
                          <div>
                            <label
                              htmlFor="rewardNote"
                              className="mb-1 block text-xs font-medium text-warn-800"
                            >
                              Nota
                            </label>
                            <input
                              id="rewardNote"
                              type="text"
                              maxLength={150}
                              value={rewardNote}
                              onChange={(e) => setRewardNote(e.target.value)}
                              placeholder="Ej. Se coordinará con la familia"
                              className="w-full rounded-xl border border-warn-300 field-input px-4 py-2.5 text-sm focus:border-warn-500 focus:outline-none focus:ring-2 focus:ring-warn-200"
                            />
                          </div>
                        </div>
                      </div>
                    </div>
                  )}

                  {/* ══ STEP 3: Contacto ════════════════════════════════════ */}
                  {step === 3 && (
                    <div className="space-y-5">
                      <div className="rounded-2xl border border-sand-200 bg-surface-warm p-5">
                        <p className="mb-1 text-sm font-semibold text-sand-700">
                          📞 Contacto de emergencia
                        </p>
                        <p className="mb-4 text-xs text-sand-500">
                          Quien encuentre a {pet.name} verá tu nombre. El
                          teléfono solo se muestra a usuarios registrados.
                        </p>
                        <div className="space-y-3">
                          <div>
                            <label
                              htmlFor="contactName"
                              className="mb-1 block text-xs font-medium text-sand-600"
                            >
                              Nombre de contacto
                            </label>
                            <input
                              id="contactName"
                              type="text"
                              value={contactName}
                              onChange={(e) => setContactName(e.target.value)}
                              maxLength={100}
                              placeholder="Ej. María Pérez"
                              className="w-full rounded-xl border border-sand-300 field-input px-4 py-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-200"
                            />
                          </div>
                          <div>
                            <label
                              htmlFor="contactPhone"
                              className="mb-1 block text-xs font-medium text-sand-600"
                            >
                              Número de teléfono
                            </label>
                            <input
                              id="contactPhone"
                              type="tel"
                              value={contactPhone}
                              onChange={(e) => setContactPhone(e.target.value)}
                              maxLength={30}
                              pattern="[\d\s()+.\-]{7,30}"
                              placeholder="Ej. +506 8888-0000"
                              className="w-full rounded-xl border border-sand-300 field-input px-4 py-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-200"
                            />
                          </div>
                        </div>
                      </div>

                      {/* Summary card */}
                      <div className="rounded-2xl border border-danger-200 bg-danger-50/30 p-4 space-y-2">
                        <p className="text-xs font-bold uppercase tracking-wider text-sand-500">
                          Resumen del reporte
                        </p>
                        <div className="grid grid-cols-2 gap-2 text-xs text-sand-700">
                          <div>
                            <span className="text-sand-400">Mascota</span>
                            <br />
                            <strong>{pet.name}</strong>
                          </div>
                          <div>
                            <span className="text-sand-400">
                              Última vez visto
                            </span>
                            <br />
                            <strong>
                              {new Date(lastSeenAt).toLocaleString("es-CR", {
                                dateStyle: "short",
                                timeStyle: "short",
                              })}
                            </strong>
                          </div>
                          <div>
                            <span className="text-sand-400">Ubicación</span>
                            <br />
                            <strong>
                              {coords
                                ? `${coords.lat.toFixed(4)}, ${coords.lng.toFixed(4)}`
                                : "No indicada"}
                            </strong>
                          </div>
                          <div>
                            <span className="text-sand-400">Foto reciente</span>
                            <br />
                            <strong>
                              {recentPhoto ? "✓ Adjuntada" : "No adjuntada"}
                            </strong>
                          </div>
                        </div>
                      </div>

                      <button
                        type="submit"
                        disabled={isPending || isQueuingOffline}
                        className="group relative w-full overflow-hidden rounded-2xl bg-danger-600 py-4 text-sm font-bold text-white shadow-md shadow-danger-200 hover:bg-danger-700 disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-danger-400 transition-all hover:-translate-y-0.5"
                      >
                        <span
                          className="pointer-events-none absolute inset-0 translate-x-[-120%] skew-x-[-20deg] bg-white/15 group-hover:translate-x-[220%] transition-transform duration-700"
                          aria-hidden="true"
                        />
                        {isPending
                          ? "Enviando reporte…"
                          : isQueuingOffline
                            ? "Guardando sin conexión…"
                            : "🚨 Reportar como perdido"}
                      </button>
                    </div>
                  )}
                </motion.div>
              </AnimatePresence>
            </div>

            {/* ── Navigation buttons ───────────────────────────────── */}
            <div className="mt-6 flex gap-3">
              {step > 1 && (
                <button
                  type="button"
                  onClick={goBack}
                  className="flex-1 rounded-xl border border-sand-300 py-3 text-sm font-semibold text-sand-700 hover:bg-sand-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
                >
                  ← Anterior
                </button>
              )}
              {step < 3 && (
                <button
                  type="button"
                  onClick={goNext}
                  className="flex-1 rounded-xl bg-brand-500 py-3 text-sm font-semibold text-white hover:bg-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
                >
                  Siguiente →
                </button>
              )}
            </div>
          </form>
        </>
      )}
    </div>
  );
}
