import { useState } from "react";
import { Helmet } from "react-helmet-async";
import { Link } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { welfareApi, type ReportWelfareCasePayload } from "../api/welfareApi";
import { WelfareLocationPicker } from "../components/WelfareLocationPicker";
import type {
  WelfareCaseType,
  WelfareSeverity,
} from "@/features/admin/api/adminApi";

const DEFAULT_CENTER: [number, number] = [9.9281, -84.0907]; // San Jose, CR

const caseTypes: Array<[WelfareCaseType, string]> = [
  ["SuspectedAbuse", "Maltrato o violencia"],
  ["Neglect", "Negligencia o falta de cuidados"],
  ["Abandonment", "Abandono"],
  ["InjuredAnimal", "Animal herido"],
  ["AnimalAtRisk", "Animal en riesgo"],
  ["Hoarding", "Acumulacion de animales"],
];

const severities: Array<[WelfareSeverity, string]> = [
  ["Low", "Baja"],
  ["Medium", "Media"],
  ["High", "Alta"],
  ["Critical", "Critica / emergencia"],
];

export default function ReportWelfareCasePage() {
  const [type, setType] = useState<WelfareCaseType>("SuspectedAbuse");
  const [severity, setSeverity] = useState<WelfareSeverity>("High");
  const [canton, setCanton] = useState("");
  const [description, setDescription] = useState("");
  const [anonymous, setAnonymous] = useState(true);
  const [useLocation, setUseLocation] = useState(false);
  const [locating, setLocating] = useState(false);
  const [locationError, setLocationError] = useState<string | null>(null);
  const [position, setPosition] = useState<[number, number] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [publicCode, setPublicCode] = useState<string | null>(null);
  const mutation = useMutation({ mutationFn: welfareApi.report });

  function toggleUseLocation(checked: boolean) {
    setUseLocation(checked);
    setLocationError(null);
    if (!checked) {
      setPosition(null);
      return;
    }
    if (!navigator.geolocation) {
      setLocationError("Tu navegador no permite compartir ubicacion.");
      return;
    }
    setLocating(true);
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setPosition([pos.coords.latitude, pos.coords.longitude]);
        setLocating(false);
      },
      () => {
        setLocationError(
          "No se pudo obtener tu ubicacion. Puedes marcar el punto manualmente en el mapa.",
        );
        setPosition(DEFAULT_CENTER);
        setLocating(false);
      },
      { enableHighAccuracy: true, timeout: 10_000 },
    );
  }

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    const payload: ReportWelfareCasePayload = {
      type,
      severity,
      canton: canton.trim(),
      description: description.trim(),
      reporterIsAnonymous: anonymous,
      approxLat: useLocation ? position?.[0] : undefined,
      approxLng: useLocation ? position?.[1] : undefined,
    };
    try {
      const result = await mutation.mutateAsync(payload);
      setPublicCode(result.publicCode);
    } catch {
      setError(
        "No se pudo enviar el reporte. Revisa los datos e intenta de nuevo.",
      );
    }
  }

  return (
    <>
      <Helmet>
        <title>Reportar maltrato animal · PawTrack CR</title>
      </Helmet>
      <main className="mx-auto max-w-2xl px-4 py-10">
        <Link to="/map" className="text-sm font-semibold text-brand-700">
          ← Volver al mapa
        </Link>
        <div className="mt-5 rounded-2xl border border-sand-200 bg-surface p-6 shadow-sm">
          <p className="text-xs font-bold uppercase tracking-wide text-brand-600">
            Bienestar animal
          </p>
          <h1 className="mt-2 font-display text-3xl font-semibold text-ink-900">
            Reportar maltrato o un animal en riesgo
          </h1>
          <p className="mt-2 text-sm text-sand-600">
            Describe hechos observables. No te expongas ni confrontes a la
            persona involucrada; si hay peligro inmediato, contacta primero a
            las autoridades locales.
          </p>
          {publicCode && (
            <div className="mt-5 rounded-lg border border-rescue-200 bg-rescue-50 p-4 text-sm text-rescue-900">
              Reporte recibido. Guarda este codigo para consultar su estado:{" "}
              <strong>{publicCode}</strong>
            </div>
          )}
          <form
            onSubmit={(event) => void submit(event)}
            className="mt-6 space-y-4"
          >
            <label className="block text-sm font-semibold text-ink-900">
              Tipo de caso
              <select
                value={type}
                onChange={(e) => setType(e.target.value as WelfareCaseType)}
                className="mt-1 w-full rounded-lg border border-sand-300 bg-white px-3 py-2"
              >
                {caseTypes.map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </label>
            <label className="block text-sm font-semibold text-ink-900">
              Gravedad
              <select
                value={severity}
                onChange={(e) => setSeverity(e.target.value as WelfareSeverity)}
                className="mt-1 w-full rounded-lg border border-sand-300 bg-white px-3 py-2"
              >
                {severities.map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </label>
            <label className="block text-sm font-semibold text-ink-900">
              Canton
              <input
                required
                value={canton}
                onChange={(e) => setCanton(e.target.value)}
                placeholder="Ej. Heredia"
                className="mt-1 w-full rounded-lg border border-sand-300 bg-white px-3 py-2"
              />
            </label>
            <label className="block text-sm font-semibold text-ink-900">
              Que ocurrio
              <textarea
                required
                minLength={20}
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={6}
                placeholder="Indica lugar, fecha aproximada, hechos observables y si el animal necesita ayuda inmediata."
                className="mt-1 w-full rounded-lg border border-sand-300 bg-white px-3 py-2"
              />
            </label>
            <label className="flex items-start gap-2 text-sm text-sand-700">
              <input
                type="checkbox"
                checked={anonymous}
                onChange={(e) => setAnonymous(e.target.checked)}
                className="mt-1"
              />
              Enviar como reporte anonimo
            </label>
            <label className="flex items-start gap-2 text-sm text-sand-700">
              <input
                type="checkbox"
                checked={useLocation}
                onChange={(e) => toggleUseLocation(e.target.checked)}
                className="mt-1"
              />
              Utilizar ubicacion
            </label>
            {locating && (
              <p className="text-sm text-sand-500">
                Obteniendo tu ubicacion...
              </p>
            )}
            {locationError && (
              <p className="text-sm font-semibold text-danger-700">
                {locationError}
              </p>
            )}
            {useLocation && position && (
              <WelfareLocationPicker
                lat={position[0]}
                lng={position[1]}
                onChange={(lat, lng) => setPosition([lat, lng])}
              />
            )}
            {error && (
              <p role="alert" className="text-sm font-semibold text-danger-700">
                {error}
              </p>
            )}
            <button
              disabled={mutation.isPending}
              className="w-full rounded-lg bg-brand-600 px-4 py-3 font-semibold text-white disabled:opacity-60"
            >
              {mutation.isPending ? "Enviando..." : "Enviar reporte"}
            </button>
          </form>
        </div>
      </main>
    </>
  );
}
