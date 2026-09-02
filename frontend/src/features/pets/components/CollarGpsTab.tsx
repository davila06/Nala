import { useState } from "react";
import { motion } from "framer-motion";
import { Link } from "react-router-dom";
import {
  MapContainer,
  TileLayer,
  Marker,
  Popup,
  Polyline,
  useMap,
} from "react-leaflet";
import "leaflet/dist/leaflet.css";
import {
  useCollarHistory,
  useCollarStatus,
  useRegisterCollar,
} from "../hooks/useCollar";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { collarApi } from "../api/collarApi";
import { CollarStatusBadge } from "./CollarStatusBadge";
import { CollarBatteryGauge } from "./CollarBatteryGauge";
import { CollarNotificationPreferencesPanel } from "./CollarNotificationPreferencesPanel";
import { CollarAuditLogTab } from "./CollarAuditLogTab";
import { CollarHandoverDialog } from "./CollarHandoverDialog";
import { CollarLostModeToggle } from "./CollarLostModeToggle";
import { CollarSafeZonesPanel } from "./CollarSafeZonesPanel";
import { CollarLocationHistoryPanel } from "./CollarLocationHistoryPanel";

interface CollarGpsTabProps {
  petId: string;
  isOwner: boolean;
}

const PROVIDER_LABELS = {
  Own: "PawTrack GPS",
  Tractive: "Tractive",
  Kippy: "Kippy",
  Generic: "Genérico",
};

const HOURS_OPTIONS = [
  { value: 1, label: "Última hora" },
  { value: 6, label: "6 horas" },
  { value: 12, label: "12 horas" },
  { value: 24, label: "24 horas" },
  { value: 72, label: "3 días" },
  { value: 168, label: "7 días" },
];

// Fit map to polyline bounds when track changes
function FitBounds({ positions }: { positions: [number, number][] }) {
  const map = useMap();
  if (positions.length >= 2) {
    map.fitBounds(positions, { padding: [24, 24], maxZoom: 16 });
  }
  return null;
}

export function CollarGpsTab({ petId, isOwner }: CollarGpsTabProps) {
  const queryClient = useQueryClient();
  const { data: collar, isLoading } = useCollarStatus(petId);
  const { mutateAsync: register, isPending } = useRegisterCollar();
  const [showSetup, setShowSetup] = useState(false);
  const [showDeactivateConfirm, setShowDeactivateConfirm] = useState(false);
  const [generatedKey, setGeneratedKey] = useState<string | null>(null);
  const [keyCopied, setKeyCopied] = useState(false);
  const [deviceId, setDeviceId] = useState("");
  const [hours, setHours] = useState(24);
  const [showNotificationPrefs, setShowNotificationPrefs] = useState(false);
  const [showAuditLog, setShowAuditLog] = useState(false);
  const [showHandoverDialog, setShowHandoverDialog] = useState(false);

  const { data: history, isFetching: historyFetching } = useCollarHistory(
    petId,
    hours,
  );

  const deactivate = useMutation({
    mutationFn: () => collarApi.deactivate(collar!.collarTagSerial!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["collar", petId] });
      setShowDeactivateConfirm(false);
    },
  });

  const generateKey = useMutation({
    mutationFn: () => collarApi.generateDeviceKey(collar!.id),
    onSuccess: (data) => setGeneratedKey(data.collarDeviceKey),
  });

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-2xl bg-sand-100" />;
  }

  if (!collar) {
    return (
      <div className="rounded-2xl border border-dashed border-sand-200 bg-surface-warm p-6 text-center">
        <span className="text-4xl" aria-hidden="true">
          📡
        </span>
        <h3 className="mt-2 text-sm font-semibold text-sand-700">
          Sin dispositivo GPS registrado
        </h3>
        <p className="mt-1 text-xs text-sand-400">
          Conecta un collar GPS para ver la posición en tiempo real y el
          historial de trayectoria.
        </p>
        {isOwner && (
          <div className="mt-4 flex flex-col gap-2 items-center">
            <Link
              to={`/collars/activate?petId=${petId}`}
              className="inline-flex items-center gap-2 rounded-xl bg-brand-600 px-4 py-2 text-xs font-bold text-white hover:bg-brand-700 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
            >
              🏷️ Activar CollarTag PawTrack →
            </Link>
            <button
              type="button"
              onClick={() => setShowSetup(true)}
              className="text-xs text-sand-500 underline hover:text-sand-700"
            >
              Conectar Tractive / Kippy / genérico
            </button>
          </div>
        )}

        {/* Setup form */}
        {showSetup && (
          <div className="mt-4 rounded-2xl border border-brand-200 bg-surface p-4 text-left space-y-3">
            <p className="text-sm font-semibold text-sand-800">
              Configurar collar GPS
            </p>
            <p className="text-xs text-sand-500">
              Compatible con <strong>Tractive</strong>, <strong>Kippy</strong> o
              cualquier GPS genérico. Ingresa el ID del dispositivo impreso en
              el collar.
            </p>
            {/* Tractive OAuth2 connect — redirects to Tractive consent screen */}
            <a
              href={`${import.meta.env.VITE_API_URL}/api/collars/tractive/connect?petId=${petId}`}
              className="flex items-center gap-2 rounded-xl border border-sand-200 bg-surface px-4 py-2.5 text-xs font-semibold text-sand-700 hover:bg-sand-50 transition-colors"
            >
              <span aria-hidden="true">📡</span> Conectar con Tractive →
            </a>
            <div className="flex items-center gap-2">
              <hr className="flex-1 border-sand-200" />
              <span className="text-[10px] text-sand-400">
                o ingresa manualmente
              </span>
              <hr className="flex-1 border-sand-200" />
            </div>
            <label className="block text-xs font-semibold text-sand-700">
              ID del dispositivo (Kippy, genérico, etc.)
              <input
                type="text"
                value={deviceId}
                onChange={(e) => setDeviceId(e.target.value)}
                placeholder="Ej: KIPPY-ABC123"
                className="mt-1 w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400"
              />
            </label>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={async () => {
                  await register({
                    petId,
                    provider: deviceId.toLowerCase().startsWith("tractive")
                      ? "Tractive"
                      : "Generic",
                    externalDeviceId: deviceId || undefined,
                  });
                  setShowSetup(false);
                }}
                disabled={isPending}
                className="rounded-xl bg-brand-600 px-4 py-2 text-xs font-bold text-white hover:bg-brand-700 disabled:opacity-60"
              >
                {isPending ? "Registrando…" : "Registrar"}
              </button>
              <button
                type="button"
                onClick={() => setShowSetup(false)}
                className="text-xs text-sand-400 hover:text-sand-600"
              >
                Cancelar
              </button>
            </div>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Status bar */}
      <motion.div
        initial={{ opacity: 0, y: 4 }}
        animate={{ opacity: 1, y: 0 }}
        className="flex items-center gap-3 rounded-2xl border border-sand-200 bg-surface p-3"
      >
        <span className="text-2xl shrink-0" aria-hidden="true">
          📡
        </span>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-semibold text-sand-900">
            {PROVIDER_LABELS[collar.provider] ?? collar.provider}
            {collar.provider === "Own" && collar.collarTagSerial && (
              <span className="ml-2 text-xs font-mono font-normal text-sand-400">
                {collar.collarTagSerial}
              </span>
            )}
            {collar.externalDeviceId && collar.provider !== "Own" && (
              <span className="ml-2 text-xs font-normal text-sand-400">
                {collar.externalDeviceId}
              </span>
            )}
          </p>
          <p className="text-xs text-sand-500">
            {collar.lastSeenAt
              ? `Última señal: ${new Date(collar.lastSeenAt).toLocaleString("es-CR")}`
              : "Sin señal reciente"}
          </p>
        </div>
        {collar.batteryPercent !== null && (
          <div className="flex flex-col items-center">
            <span
              className={`text-sm font-bold tabular-nums ${collar.batteryPercent < 20 ? "text-danger-600" : "text-rescue-600"}`}
            >
              {collar.batteryPercent}%
            </span>
            <span className="text-[10px] text-sand-400">Batería</span>
          </div>
        )}
        <span
          className={`h-2 w-2 rounded-full shrink-0 ${collar.isActive ? "bg-rescue-500" : "bg-sand-300"}`}
        />
      </motion.div>

      <CollarStatusBadge
        isActive={collar.isActive}
        isOffline={collar.isOffline}
        batteryPercent={collar.batteryPercent}
        batteryAlertThresholdPercent={collar.batteryAlertThresholdPercent}
      />
      <CollarBatteryGauge
        batteryPercent={collar.batteryPercent}
        thresholdPercent={collar.batteryAlertThresholdPercent}
      />

      {isOwner && <CollarLostModeToggle petId={petId} collarId={collar.id} />}

      {isOwner && collar.lastLat !== null && collar.lastLng !== null && (
        <CollarSafeZonesPanel
          collarId={collar.id}
          centerLat={collar.lastLat}
          centerLng={collar.lastLng}
        />
      )}

      {isOwner && collar.lastLat !== null && collar.lastLng !== null && (
        <CollarLocationHistoryPanel
          collarId={collar.id}
          centerLat={collar.lastLat}
          centerLng={collar.lastLng}
        />
      )}

      {isOwner && (
        <div>
          <button
            type="button"
            onClick={() => setShowNotificationPrefs((v) => !v)}
            className="text-xs text-sand-500 underline hover:text-sand-700"
          >
            {showNotificationPrefs
              ? "Ocultar notificaciones"
              : "⚙️ Configurar notificaciones"}
          </button>
          {showNotificationPrefs && (
            <div className="mt-2">
              <CollarNotificationPreferencesPanel
                petId={petId}
                collarId={collar.id}
                offlineAlertsEnabled={collar.offlineAlertsEnabled}
                offlineThresholdMinutes={collar.offlineThresholdMinutes}
                batteryAlertsEnabled={collar.batteryAlertsEnabled}
                batteryAlertThresholdPercent={
                  collar.batteryAlertThresholdPercent
                }
              />
            </div>
          )}
        </div>
      )}

      {isOwner && (
        <div>
          <button
            type="button"
            onClick={() => setShowAuditLog((v) => !v)}
            className="text-xs text-sand-500 underline hover:text-sand-700"
          >
            {showAuditLog ? "Ocultar historial" : "📋 Ver historial de eventos"}
          </button>
          {showAuditLog && (
            <div className="mt-2">
              <CollarAuditLogTab collarId={collar.id} />
            </div>
          )}
        </div>
      )}

      {/* Generar clave de dispositivo (collares Generic u Own sin CollarTag, para OEM push) */}
      {isOwner &&
        !collar.collarTagSerial &&
        (collar.provider === "Generic" || collar.provider === "Own") && (
          <div className="space-y-2">
            <button
              type="button"
              disabled={generateKey.isPending}
              onClick={() => generateKey.mutate()}
              className="text-xs text-brand-600 underline hover:text-brand-800 disabled:opacity-40"
            >
              {generateKey.isPending
                ? "Generando…"
                : "🔑 Generar clave de dispositivo (push OEM)"}
            </button>
            {generatedKey && (
              <div className="rounded-2xl border-2 border-amber-400 bg-amber-50 p-4 space-y-2">
                <p className="text-xs font-bold text-amber-800">
                  ⚠️ Copia esta clave — solo se muestra una vez
                </p>
                <div className="flex items-center gap-2">
                  <code className="flex-1 break-all rounded-lg bg-amber-100 px-3 py-2 text-[11px] font-mono text-amber-900">
                    {generatedKey}
                  </code>
                  <button
                    type="button"
                    onClick={() => {
                      navigator.clipboard.writeText(generatedKey);
                      setKeyCopied(true);
                      setTimeout(() => setKeyCopied(false), 2000);
                    }}
                    className="shrink-0 rounded-lg bg-amber-200 px-3 py-2 text-xs font-bold text-amber-900 hover:bg-amber-300"
                  >
                    {keyCopied ? "✓" : "Copiar"}
                  </button>
                </div>
                <p className="text-[10px] text-amber-700">
                  Usa esta clave en el header{" "}
                  <code className="font-mono">X-Collar-Key</code> para{" "}
                  <code className="font-mono">POST /api/collars/ingest</code>.
                </p>
              </div>
            )}
          </div>
        )}

      {/* Desvincular CollarTag (solo si es dispositivo propio y es dueño) */}
      {isOwner && collar.provider === "Own" && collar.collarTagSerial && (
        <div className="space-y-2">
          {!showHandoverDialog ? (
            <button
              type="button"
              onClick={() => setShowHandoverDialog(true)}
              className="text-xs text-brand-600 underline hover:text-brand-800"
            >
              🔄 Transferir a otro propietario
            </button>
          ) : (
            <CollarHandoverDialog
              collarId={collar.id}
              onClose={() => setShowHandoverDialog(false)}
            />
          )}
          {!showDeactivateConfirm ? (
            <button
              type="button"
              onClick={() => setShowDeactivateConfirm(true)}
              className="text-xs text-sand-400 underline hover:text-red-600"
            >
              Desvincular collar
            </button>
          ) : (
            <div className="rounded-2xl border border-red-200 bg-red-50 p-4 space-y-3">
              <p className="text-sm font-semibold text-red-800">
                ¿Desvincular {collar.collarTagSerial}?
              </p>
              <p className="text-xs text-red-700">
                El collar dejará de reportar posición. Podrás reactivarlo más
                adelante.
              </p>
              <div className="flex gap-2">
                <button
                  type="button"
                  disabled={deactivate.isPending}
                  onClick={() => deactivate.mutate()}
                  className="rounded-xl bg-red-600 px-4 py-2 text-xs font-bold text-white hover:bg-red-700 disabled:opacity-50"
                >
                  {deactivate.isPending ? "Desvinculando…" : "Confirmar"}
                </button>
                <button
                  type="button"
                  onClick={() => setShowDeactivateConfirm(false)}
                  className="text-xs text-sand-500 underline"
                >
                  Cancelar
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Time range selector */}
      <div className="flex items-center justify-between gap-2">
        <p className="text-xs font-semibold text-sand-600">Trayectoria</p>
        <div className="flex gap-1 flex-wrap justify-end">
          {HOURS_OPTIONS.map((opt) => (
            <button
              key={opt.value}
              type="button"
              onClick={() => setHours(opt.value)}
              className={[
                "rounded-xl px-2.5 py-1 text-[10px] font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400",
                hours === opt.value
                  ? "bg-brand-600 text-white"
                  : "bg-sand-100 text-sand-600 hover:bg-sand-200",
              ].join(" ")}
            >
              {opt.label}
            </button>
          ))}
        </div>
      </div>

      {/* Track stats */}
      {history && history.length > 0 && (
        <div className="flex gap-3 text-xs text-sand-500">
          <span className="font-semibold text-sand-700">{history.length}</span>{" "}
          puntos registrados
          {historyFetching && (
            <span className="text-brand-500 animate-pulse">
              · actualizando…
            </span>
          )}
        </div>
      )}

      {/* Map */}
      {collar.lastLat !== null && collar.lastLng !== null ? (
        <div
          className="overflow-hidden rounded-2xl border border-sand-200"
          style={{ height: 320 }}
        >
          <MapContainer
            center={[collar.lastLat, collar.lastLng]}
            zoom={15}
            className="h-full w-full"
            zoomControl={false}
          >
            <TileLayer
              url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
              attribution="© OpenStreetMap"
            />

            {/* Historical track polyline */}
            {history &&
              history.length >= 2 &&
              (() => {
                const positions = history.map((p): [number, number] => [
                  p.lat,
                  p.lng,
                ]);
                return (
                  <>
                    <FitBounds positions={positions} />
                    <Polyline
                      positions={positions}
                      pathOptions={{
                        color: "#f97316",
                        weight: 3,
                        opacity: 0.85,
                        dashArray: undefined,
                      }}
                    />
                    {/* Start dot */}
                    <Marker
                      position={positions[0]}
                      title={`Inicio: ${new Date(history[0].recordedAt).toLocaleTimeString("es-CR")}`}
                    />
                  </>
                );
              })()}

            {/* Current position marker */}
            <Marker position={[collar.lastLat, collar.lastLng]}>
              <Popup>
                <strong>Posición actual</strong>
                <br />
                {collar.lastSeenAt &&
                  new Date(collar.lastSeenAt).toLocaleString("es-CR")}
              </Popup>
            </Marker>
          </MapContainer>
        </div>
      ) : (
        <div className="flex h-40 items-center justify-center rounded-2xl border border-dashed border-sand-200 bg-surface-warm">
          <p className="text-sm text-sand-400">Esperando primera señal GPS…</p>
        </div>
      )}

      <p className="text-center text-[10px] text-sand-400">
        Posición en tiempo real · trayectoria de hasta 7 días · actualización
        automática cada 30 s.
      </p>
    </div>
  );
}
