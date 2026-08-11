import { useCallback, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import { motion, AnimatePresence } from "framer-motion";
import { useMutation } from "@tanstack/react-query";
import { matchingApi, type VisualMatchResult } from "../api/matchingApi";
import { Button } from "@/shared/ui/Button";

// ── Helpers ───────────────────────────────────────────────────────────────────

function scoreLabel(s: number): { text: string; color: string; bg: string } {
  if (s >= 0.85)
    return {
      text: "Alta coincidencia",
      color: "text-rescue-700",
      bg: "bg-rescue-100",
    };
  if (s >= 0.72)
    return {
      text: "Posible coincidencia",
      color: "text-brand-700",
      bg: "bg-brand-100",
    };
  return {
    text: "Coincidencia leve",
    color: "text-sand-600",
    bg: "bg-sand-100",
  };
}

const SPECIES_LABEL: Record<string, string> = {
  Dog: "Perro",
  Cat: "Gato",
  Bird: "Ave",
  Rabbit: "Conejo",
  Other: "Otra",
};

// ── Step components ───────────────────────────────────────────────────────────

function StepUpload({
  onPhoto,
  isLoading,
}: {
  onPhoto: (file: File) => void;
  isLoading: boolean;
}) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const [selected, setSelected] = useState<File | null>(null);

  const handleFile = useCallback((file: File) => {
    if (!file.type.startsWith("image/")) return;
    setSelected(file);
    setPreview(URL.createObjectURL(file));
  }, []);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0];
    if (f) handleFile(f);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    const f = e.dataTransfer.files?.[0];
    if (f) handleFile(f);
  };

  return (
    <div className="space-y-5">
      {/* Drop zone */}
      <button
        type="button"
        onClick={() => inputRef.current?.click()}
        onDrop={handleDrop}
        onDragOver={(e) => e.preventDefault()}
        aria-label="Subir foto de la mascota"
        className={[
          "relative flex w-full flex-col items-center justify-center gap-3 rounded-2xl border-2 border-dashed transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400",
          preview
            ? "border-brand-300 bg-brand-50"
            : "border-sand-300 bg-sand-50 hover:border-brand-300 hover:bg-brand-50",
        ].join(" ")}
        style={{ minHeight: 220 }}
      >
        {preview ? (
          <img
            src={preview}
            alt="Vista previa de la mascota"
            className="max-h-52 w-full rounded-xl object-contain"
          />
        ) : (
          <>
            <span className="text-5xl" aria-hidden="true">
              📷
            </span>
            <p className="text-sm font-semibold text-sand-700">
              Toca para tomar o subir una foto
            </p>
            <p className="text-xs text-sand-400">
              JPEG · PNG · WebP · máx. 5 MB
            </p>
          </>
        )}
      </button>

      <input
        ref={inputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        capture="environment"
        className="sr-only"
        onChange={handleChange}
        aria-hidden="true"
      />

      {selected && (
        <Button fullWidth loading={isLoading} onClick={() => onPhoto(selected)}>
          {isLoading ? "Buscando coincidencias…" : "Buscar mascota 🔍"}
        </Button>
      )}

      <p className="text-center text-xs text-sand-400">
        La foto no se guarda — solo se usa para comparar con los reportes
        activos.
      </p>
    </div>
  );
}

function StepResults({
  matches,
  onSelect,
  onReportAnyway,
}: {
  matches: VisualMatchResult[];
  onSelect: (m: VisualMatchResult) => void;
  onReportAnyway: () => void;
}) {
  if (matches.length === 0) {
    return (
      <div className="space-y-5 text-center">
        <div className="text-5xl" aria-hidden="true">
          🔍
        </div>
        <div>
          <h2 className="font-display text-lg font-bold text-sand-900">
            Sin coincidencias encontradas
          </h2>
          <p className="mt-1 text-sm text-sand-500">
            No encontramos una mascota perdida que coincida con la foto. Puedes
            reportar tu avistamiento igualmente para que el dueño lo vea.
          </p>
        </div>
        <Button fullWidth onClick={onReportAnyway}>
          Reportar avistamiento igualmente
        </Button>
        <Button
          fullWidth
          variant="secondary"
          onClick={() => window.history.back()}
        >
          Subir otra foto
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div>
        <h2 className="font-display text-lg font-bold text-sand-900">
          {matches.length === 1
            ? "1 posible coincidencia"
            : `${matches.length} posibles coincidencias`}
        </h2>
        <p className="text-sm text-sand-500 mt-0.5">
          ¿Reconoces a alguna de estas mascotas?
        </p>
      </div>

      <ul className="space-y-3">
        {matches.map((m) => {
          const lbl = scoreLabel(m.similarityScore);
          const pct = Math.round(m.similarityScore * 100);
          return (
            <li key={m.petId}>
              <button
                type="button"
                onClick={() => onSelect(m)}
                className="w-full flex items-center gap-3 rounded-xl border border-sand-200 bg-surface p-3 text-left transition-all hover:border-brand-300 hover:shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
              >
                <div className="relative h-16 w-16 shrink-0 overflow-hidden rounded-lg bg-sand-100">
                  {m.photoUrl ? (
                    <img
                      src={m.photoUrl}
                      alt={m.petName}
                      className="h-full w-full object-cover"
                    />
                  ) : (
                    <div
                      className="flex h-full w-full items-center justify-center text-2xl"
                      aria-hidden="true"
                    >
                      🐾
                    </div>
                  )}
                  <span className="absolute bottom-0 right-0 rounded-tl-lg bg-zinc-900/70 px-1 py-0.5 text-[9px] font-bold text-white">
                    {pct}%
                  </span>
                </div>

                <div className="flex-1 min-w-0">
                  <p className="font-semibold text-sand-900 truncate">
                    {m.petName}
                  </p>
                  <p className="text-xs text-sand-500">
                    {SPECIES_LABEL[m.species] ?? m.species}
                  </p>
                  <span
                    className={`mt-1 inline-block rounded-full px-2 py-0.5 text-[10px] font-semibold ${lbl.bg} ${lbl.color}`}
                  >
                    {lbl.text}
                  </span>
                  {m.distanceKm != null && (
                    <p className="text-xs text-brand-600 mt-0.5">
                      📍 {m.distanceKm.toFixed(1)} km de donde la encontraste
                    </p>
                  )}
                </div>

                <svg
                  viewBox="0 0 16 16"
                  fill="currentColor"
                  className="h-4 w-4 shrink-0 text-sand-300"
                  aria-hidden="true"
                >
                  <path
                    fillRule="evenodd"
                    d="M6.22 4.22a.75.75 0 0 1 1.06 0l3.25 3.25a.75.75 0 0 1 0 1.06l-3.25 3.25a.75.75 0 0 1-1.06-1.06L8.94 8 6.22 5.28a.75.75 0 0 1 0-1.06Z"
                    clipRule="evenodd"
                  />
                </svg>
              </button>
            </li>
          );
        })}
      </ul>

      <div className="pt-2 border-t border-sand-100 space-y-2">
        <p className="text-xs text-sand-500 text-center">
          ¿No es ninguna de estas?
        </p>
        <Button fullWidth variant="secondary" onClick={onReportAnyway}>
          Reportar avistamiento igualmente
        </Button>
      </div>
    </div>
  );
}

function StepConfirm({ match }: { match: VisualMatchResult }) {
  const navigate = useNavigate();
  const lbl = scoreLabel(match.similarityScore);

  return (
    <div className="space-y-5">
      <div className="rounded-2xl border border-rescue-200 bg-rescue-50 p-4 flex items-center gap-3">
        {match.photoUrl && (
          <img
            src={match.photoUrl}
            alt={match.petName}
            className="h-14 w-14 rounded-xl object-cover shrink-0 border border-rescue-200"
          />
        )}
        <div>
          <p className="font-display text-base font-bold text-rescue-800">
            {match.petName}
          </p>
          <p className="text-xs text-rescue-600">
            {SPECIES_LABEL[match.species] ?? match.species}
          </p>
          <span
            className={`mt-0.5 inline-block rounded-full px-2 py-0.5 text-[10px] font-semibold ${lbl.bg} ${lbl.color}`}
          >
            {lbl.text}
          </span>
        </div>
      </div>

      <div className="space-y-3">
        <h2 className="font-display text-lg font-bold text-sand-900">
          ¿Qué quieres hacer?
        </h2>

        <a
          href={match.publicProfileUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="flex w-full items-center justify-between rounded-xl border border-trust-200 bg-trust-50 px-4 py-3.5 text-sm font-semibold text-trust-800 hover:bg-trust-100 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-trust-400"
        >
          <span>Ver perfil y contactar al dueño</span>
          <svg
            viewBox="0 0 16 16"
            fill="currentColor"
            className="h-4 w-4"
            aria-hidden="true"
          >
            <path
              fillRule="evenodd"
              d="M6.22 4.22a.75.75 0 0 1 1.06 0l3.25 3.25a.75.75 0 0 1 0 1.06l-3.25 3.25a.75.75 0 0 1-1.06-1.06L8.94 8 6.22 5.28a.75.75 0 0 1 0-1.06Z"
              clipRule="evenodd"
            />
          </svg>
        </a>

        <button
          type="button"
          onClick={() => navigate("/encontre-mascota")}
          className="flex w-full items-center justify-between rounded-xl border border-sand-200 bg-surface px-4 py-3.5 text-sm font-semibold text-sand-700 hover:bg-sand-50 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
        >
          <span>Registrar avistamiento con ubicación y foto</span>
          <svg
            viewBox="0 0 16 16"
            fill="currentColor"
            className="h-4 w-4"
            aria-hidden="true"
          >
            <path
              fillRule="evenodd"
              d="M6.22 4.22a.75.75 0 0 1 1.06 0l3.25 3.25a.75.75 0 0 1 0 1.06l-3.25 3.25a.75.75 0 0 1-1.06-1.06L8.94 8 6.22 5.28a.75.75 0 0 1 0-1.06Z"
              clipRule="evenodd"
            />
          </svg>
        </button>
      </div>

      <div className="rounded-xl border border-brand-200 bg-brand-50 p-4 text-center space-y-2">
        <p className="text-xs font-semibold text-brand-800">
          ¿Ayudas mascotas seguido?
        </p>
        <p className="text-xs text-brand-700">
          Crea una cuenta gratis y gana puntos como rescatador verificado.
        </p>
        <Link
          to="/register"
          className="inline-block rounded-xl bg-brand-600 px-4 py-2 text-xs font-bold text-white hover:bg-brand-700 transition-colors"
        >
          Unirme a la comunidad
        </Link>
      </div>
    </div>
  );
}

// ── Main page ─────────────────────────────────────────────────────────────────

type Step = "upload" | "results" | "confirm";

export default function QuickFoundPetPage() {
  const navigate = useNavigate();
  const [step, setStep] = useState<Step>("upload");
  const [matches, setMatches] = useState<VisualMatchResult[]>([]);
  const [selected, setSelected] = useState<VisualMatchResult | null>(null);

  const matchMutation = useMutation({
    mutationFn: (file: File) => matchingApi.quickMatch({ photo: file }),
    onSuccess: (data) => {
      setMatches(data);
      setStep("results");
    },
  });

  const slideVariants = {
    enter: { x: 40, opacity: 0 },
    center: { x: 0, opacity: 1 },
    exit: { x: -40, opacity: 0 },
  };

  return (
    <>
      <Helmet>
        <title>Encontré una mascota — PawTrack CR</title>
        <meta
          name="description"
          content="Sube una foto de la mascota que encontraste y la IA buscará coincidencias con mascotas perdidas en Costa Rica en segundos."
        />
        <meta
          property="og:title"
          content="Encontré una mascota — PawTrack CR"
        />
        <meta
          property="og:description"
          content="¿Encontraste una mascota? Busca al dueño con reconocimiento visual por IA."
        />
        <meta property="og:type" content="website" />
        <meta
          property="og:image"
          content="https://pawtrack.cr/og-encontre.png"
        />
      </Helmet>

      <div className="min-h-dvh bg-sand-50">
        {/* Header */}
        <header className="sticky top-0 z-10 border-b border-sand-200 bg-surface/95 backdrop-blur-sm">
          <div className="mx-auto flex h-14 max-w-md items-center gap-3 px-4">
            <Link
              to="/"
              className="flex items-center gap-2 font-display text-base font-semibold text-brand-600"
            >
              <span
                aria-hidden="true"
                className="flex h-7 w-7 items-center justify-center rounded-lg bg-brand-500 text-sm text-white"
              >
                🐾
              </span>
              PawTrack CR
            </Link>
            <div className="flex-1" />
            <span className="rounded-full bg-trust-50 px-2.5 py-0.5 text-xs font-semibold text-trust-700">
              IA de búsqueda
            </span>
          </div>
        </header>

        {/* Hero */}
        <div className="bg-gradient-to-br from-trust-900 to-trust-800 px-4 py-6 text-white text-center">
          <p className="text-3xl mb-1" aria-hidden="true">
            🐾
          </p>
          <h1 className="font-display text-xl font-bold">
            ¿Encontraste una mascota?
          </h1>
          <p className="mt-1 text-sm text-trust-200 max-w-xs mx-auto">
            Sube una foto y en segundos buscamos si está reportada como perdida.
          </p>
        </div>

        {/* Step indicator */}
        <div className="border-b border-sand-200 bg-surface">
          <div className="mx-auto flex max-w-md items-center px-4 py-3 gap-0">
            {(["upload", "results", "confirm"] as Step[]).map((s, i) => {
              const steps = ["upload", "results", "confirm"];
              const idx = steps.indexOf(step);
              const isDone = steps.indexOf(s) < idx;
              const isActive = s === step;
              return (
                <div key={s} className="flex flex-1 items-center">
                  <div className="flex-1 flex flex-col items-center">
                    <div
                      className={`h-2 rounded-full w-full transition-colors ${isDone ? "bg-rescue-500" : isActive ? "bg-brand-500" : "bg-sand-200"}`}
                    />
                    <span className="mt-1 text-[10px] text-sand-400">
                      {s === "upload"
                        ? "Foto"
                        : s === "results"
                          ? "Resultados"
                          : "Contacto"}
                    </span>
                  </div>
                  {i < 2 && <div className="w-2" />}
                </div>
              );
            })}
          </div>
        </div>

        {/* Content */}
        <main className="mx-auto max-w-md px-4 py-6">
          {matchMutation.isError && (
            <div
              role="alert"
              className="mb-4 rounded-xl border border-danger-200 bg-danger-50 px-4 py-3 text-sm text-danger-700"
            >
              {(
                matchMutation.error as {
                  response?: { data?: { detail?: string } };
                }
              )?.response?.data?.detail ??
                "No se pudo procesar la imagen. Intenta con otra foto."}
            </div>
          )}

          <AnimatePresence mode="wait" initial={false}>
            <motion.div
              key={step}
              variants={slideVariants}
              initial="enter"
              animate="center"
              exit="exit"
              transition={{ duration: 0.22, ease: [0.4, 0, 0.2, 1] }}
            >
              {step === "upload" && (
                <StepUpload
                  isLoading={matchMutation.isPending}
                  onPhoto={(file) => matchMutation.mutate(file)}
                />
              )}
              {step === "results" && (
                <StepResults
                  matches={matches}
                  onSelect={(m) => {
                    setSelected(m);
                    setStep("confirm");
                  }}
                  onReportAnyway={() => navigate("/encontre-mascota")}
                />
              )}
              {step === "confirm" && selected && (
                <StepConfirm match={selected} />
              )}
            </motion.div>
          </AnimatePresence>
        </main>
      </div>
    </>
  );
}
