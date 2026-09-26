import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button, Card, Input, Badge } from "@/shared/ui";
import { toast } from "@/shared/lib/toast";
import { useAuthStore } from "../store/authStore";
import {
  useDisableMfa,
  useEnableMfa,
  useMfaStepUp,
  useMySessions,
  useMyTrustedDevices,
  useRevokeMySession,
  useRevokeTrustedDevice,
  useSetupMfa,
  useTrustCurrentDevice,
} from "../hooks/useProfile";

function formatTimestamp(value: string) {
  return new Intl.DateTimeFormat("es-CR", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

export function AccountSecuritySection({ hasMfa }: { hasMfa: boolean }) {
  const navigate = useNavigate();
  const clearAuth = useAuthStore((state) => state.clearAuth);
  const [setupSecret, setSetupSecret] = useState("");
  const [setupUri, setSetupUri] = useState("");
  const [setupCode, setSetupCode] = useState("");
  const [stepUpCode, setStepUpCode] = useState("");
  const [deviceName, setDeviceName] = useState("Este dispositivo");
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);
  const sessions = useMySessions();
  const devices = useMyTrustedDevices();
  const setupMfa = useSetupMfa();
  const enableMfa = useEnableMfa();
  const stepUpMfa = useMfaStepUp();
  const disableMfa = useDisableMfa();
  const trustDevice = useTrustCurrentDevice();
  const revokeSession = useRevokeMySession();
  const revokeDevice = useRevokeTrustedDevice();

  const verifyStepUp = async () => {
    if (!stepUpCode.trim()) throw new Error("MISSING_MFA_CODE");
    await stepUpMfa.mutateAsync(stepUpCode.trim());
    setStepUpCode("");
  };

  const handleSetupMfa = async () => {
    try {
      const setup = await setupMfa.mutateAsync();
      setSetupSecret(setup.secret);
      setSetupUri(setup.otpauthUri);
    } catch {
      toast.error("No se pudo iniciar la configuración de MFA.");
    }
  };

  const handleEnableMfa = async () => {
    try {
      const enabled = await enableMfa.mutateAsync({ secret: setupSecret, code: setupCode.trim() });
      setRecoveryCodes(enabled.recoveryCodes);
      setSetupSecret("");
      setSetupUri("");
      setSetupCode("");
      toast.success("MFA quedó activado.");
    } catch {
      toast.error("No se pudo activar MFA. Verifica el código.");
    }
  };

  const handleTrustDevice = async () => {
    try {
      await verifyStepUp();
      await trustDevice.mutateAsync(deviceName.trim());
      toast.success("Dispositivo confiable agregado por 30 días.");
    } catch {
      toast.error("No se pudo confiar el dispositivo. Verifica MFA y el nombre.");
    }
  };

  const handleRevokeSession = async (sessionId: string, isCurrent: boolean) => {
    try {
      await verifyStepUp();
      await revokeSession.mutateAsync(sessionId);
      if (isCurrent) {
        clearAuth();
        void navigate("/login", { replace: true });
      } else {
        toast.success("Sesión revocada.");
      }
    } catch {
      toast.error("No se pudo revocar la sesión. Verifica MFA.");
    }
  };

  const handleRevokeDevice = async (deviceId: string) => {
    try {
      await verifyStepUp();
      await revokeDevice.mutateAsync(deviceId);
      toast.success("Dispositivo revocado.");
    } catch {
      toast.error("No se pudo revocar el dispositivo. Verifica MFA.");
    }
  };

  const handleDisableMfa = async () => {
    try {
      await verifyStepUp();
      await disableMfa.mutateAsync();
      clearAuth();
      void navigate("/login", { replace: true });
    } catch {
      toast.error("No se pudo desactivar MFA. Verifica el código.");
    }
  };

  return (
    <Card>
      <div className="flex items-start justify-between gap-4">
        <div>
          <h2 className="text-base font-semibold text-sand-800">Seguridad de la cuenta</h2>
          <p className="mt-1 text-sm text-sand-500">MFA, sesiones y dispositivos autorizados.</p>
        </div>
        <Badge variant={hasMfa ? "trust" : "neutral"}>{hasMfa ? "MFA activa" : "MFA inactiva"}</Badge>
      </div>

      {!hasMfa ? (
        <div className="mt-4 space-y-3">
          {!setupSecret ? (
            <Button variant="secondary" size="sm" loading={setupMfa.isPending} onClick={() => void handleSetupMfa()}>
              Configurar MFA
            </Button>
          ) : (
            <>
              <div className="rounded-lg border border-sand-200 bg-sand-50 p-3">
                <p className="text-xs font-medium text-sand-600">Secreto de configuración</p>
                <code className="mt-1 block break-all font-mono text-sm text-sand-900">{setupSecret}</code>
                <a className="mt-2 inline-block text-xs font-semibold text-brand-700 underline" href={setupUri}>
                  Abrir en app de autenticación
                </a>
              </div>
              <Input
                label="Código de autenticación"
                value={setupCode}
                onChange={(event) => setSetupCode(event.target.value)}
                autoComplete="one-time-code"
                maxLength={32}
              />
              <Button
                size="sm"
                loading={enableMfa.isPending}
                disabled={!setupCode.trim()}
                onClick={() => void handleEnableMfa()}
              >
                Activar MFA
              </Button>
            </>
          )}
        </div>
      ) : (
        <div className="mt-4 space-y-3">
          <Input
            label="Código MFA para cambios de seguridad"
            value={stepUpCode}
            onChange={(event) => setStepUpCode(event.target.value)}
            autoComplete="one-time-code"
            maxLength={32}
          />
          <div className="flex flex-wrap gap-2">
            <Button
              variant="danger"
              size="sm"
              loading={disableMfa.isPending}
              disabled={!stepUpCode.trim()}
              onClick={() => void handleDisableMfa()}
            >
              Desactivar MFA
            </Button>
          </div>
        </div>
      )}

      {recoveryCodes.length > 0 && (
        <div className="mt-4 rounded-lg border border-warn-300 bg-warn-50 p-3">
          <h3 className="text-sm font-semibold text-sand-900">Códigos de recuperación</h3>
          <div className="mt-2 grid grid-cols-2 gap-2 font-mono text-xs text-sand-800">
            {recoveryCodes.map((code) => (
              <code key={code}>{code}</code>
            ))}
          </div>
          <Button className="mt-3" size="sm" variant="secondary" onClick={() => setRecoveryCodes([])}>
            Listo
          </Button>
        </div>
      )}

      <div className="mt-6 border-t border-sand-200 pt-4">
        <h3 className="text-sm font-semibold text-sand-800">Sesiones activas</h3>
        {sessions.isLoading ? (
          <p className="mt-2 text-sm text-sand-500">Cargando sesiones…</p>
        ) : (
          <ul className="mt-2 divide-y divide-sand-100">
            {(sessions.data ?? []).map((session) => (
              <li key={session.sessionId} className="flex flex-wrap items-center justify-between gap-3 py-3">
                <div>
                  <p className="text-sm font-medium text-sand-800">{session.isCurrent ? "Sesión actual" : "Sesión"}</p>
                  <p className="text-xs text-sand-500">Última actividad: {formatTimestamp(session.lastActivityAt)}</p>
                </div>
                <Button
                  variant="ghost"
                  size="sm"
                  disabled={!stepUpCode.trim() || revokeSession.isPending}
                  onClick={() => void handleRevokeSession(session.sessionId, session.isCurrent)}
                >
                  Revocar
                </Button>
              </li>
            ))}
          </ul>
        )}
      </div>

      {hasMfa && (
        <div className="mt-5 border-t border-sand-200 pt-4">
          <h3 className="text-sm font-semibold text-sand-800">Dispositivos confiables</h3>
          <div className="mt-3 grid gap-3 sm:grid-cols-[1fr_auto]">
            <Input
              label="Nombre del dispositivo"
              value={deviceName}
              maxLength={100}
              onChange={(event) => setDeviceName(event.target.value)}
            />
            <Button
              className="self-end"
              size="sm"
              disabled={!stepUpCode.trim() || !deviceName.trim() || trustDevice.isPending}
              loading={trustDevice.isPending}
              onClick={() => void handleTrustDevice()}
            >
              Confiar dispositivo
            </Button>
          </div>
          {devices.isLoading ? (
            <p className="mt-2 text-sm text-sand-500">Cargando dispositivos…</p>
          ) : (
            <ul className="mt-2 divide-y divide-sand-100">
              {(devices.data ?? []).map((device) => (
                <li key={device.id} className="flex flex-wrap items-center justify-between gap-3 py-3">
                  <div>
                    <p className="text-sm font-medium text-sand-800">{device.deviceName}</p>
                    <p className="text-xs text-sand-500">Vence: {formatTimestamp(device.expiresAt)}</p>
                  </div>
                  <Button
                    variant="ghost"
                    size="sm"
                    disabled={!stepUpCode.trim() || revokeDevice.isPending}
                    onClick={() => void handleRevokeDevice(device.id)}
                  >
                    Revocar
                  </Button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </Card>
  );
}
