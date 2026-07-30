import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  useMyFosterProfile,
  useUpsertMyFosterProfile,
} from "@/features/sightings/hooks/useFosters";
import {
  useMyProfile,
  useUpdateProfile,
  useChangePassword,
  useDeleteAccount,
} from "../hooks/useProfile";
import { useAuthStore } from "../store/authStore";
import type { PetSpecies } from "@/features/sightings/api/fostersApi";
import { Button, Input, Badge, PageSpinner } from "@/shared/ui";
import { toast } from "@/shared/lib/toast";
import { LastSeenMap } from "@/features/lost-pets/components/LastSeenMap";
import { useGeolocation } from "@/features/lost-pets/hooks/useGeolocation";
import { usePushSubscription } from "@/features/notifications/hooks/usePushSubscription";
import { formatDate } from "@/shared/lib/formatDate";

// ── Locale maps ───────────────────────────────────────────────────────────────

const ROLE_LABEL: Record<string, string> = {
  Owner: "Propietario",
  Ally: "Aliado",
  Admin: "Administrador",
  Clinic: "Clínica veterinaria",
};

const SPECIES_LABEL: Record<PetSpecies, string> = {
  Dog: "🐶 Perro",
  Cat: "🐱 Gato",
  Bird: "🐦 Ave",
  Rabbit: "🐰 Conejo",
  Other: "🐾 Otro",
};

const ALL_SPECIES: PetSpecies[] = ["Dog", "Cat", "Bird", "Rabbit", "Other"];

// ── Password strength ─────────────────────────────────────────────────────────

interface PasswordStrength {
  score: 0 | 1 | 2 | 3 | 4;
  label: string;
  color: string;
}

function getPasswordStrength(pwd: string): PasswordStrength {
  if (!pwd) return { score: 0, label: "", color: "" };
  let score = 0;
  if (pwd.length >= 8) score++;
  if (pwd.length >= 12) score++;
  if (/[A-Z]/.test(pwd) && /[a-z]/.test(pwd)) score++;
  if (/\d/.test(pwd)) score++;
  if (/[^A-Za-z0-9]/.test(pwd)) score++;
  const capped = Math.min(score, 4) as 0 | 1 | 2 | 3 | 4;
  const levels: PasswordStrength[] = [
    { score: 0, label: "", color: "" },
    { score: 1, label: "Muy débil", color: "bg-danger-500" },
    { score: 2, label: "Débil", color: "bg-warn-400" },
    { score: 3, label: "Buena", color: "bg-trust-400" },
    { score: 4, label: "Fuerte", color: "bg-rescue-500" },
  ];
  return levels[capped];
}

export default function ProfilePage() {
  const navigate = useNavigate();
  const { data: serverProfile, isLoading: profileLoading } = useMyProfile();
  const { mutateAsync: updateProfileName, isPending: updatingName } =
    useUpdateProfile();
  const { mutateAsync: changePasswordMutation, isPending: changingPassword } =
    useChangePassword();
  const { mutateAsync: deleteAccountMutation, isPending: deletingAccount } =
    useDeleteAccount();
  const user = useAuthStore((s) => s.user);
  const {
    status: pushStatus,
    subscribe: pushSubscribe,
    unsubscribe: pushUnsubscribe,
  } = usePushSubscription();

  const { data: fosterProfile, isLoading: fosterLoading } =
    useMyFosterProfile();
  const { mutateAsync: saveProfile, isPending: savingFoster } =
    useUpsertMyFosterProfile();

  // Identity section state
  const displayName = serverProfile?.name ?? user?.name ?? "";
  const [editingName, setEditingName] = useState(false);
  const [nameInput, setNameInput] = useState("");

  const handleEditName = () => {
    setNameInput(displayName);
    setEditingName(true);
  };

  const handleSaveName = async () => {
    if (!nameInput.trim()) return;
    try {
      await updateProfileName({ name: nameInput.trim() });
      setEditingName(false);
      toast.success("Nombre actualizado correctamente.");
    } catch {
      toast.error("No se pudo actualizar el nombre. Intenta de nuevo.");
    }
  };

  // Foster section state
  const [isVolunteer, setIsVolunteer] = useState<boolean>(
    fosterProfile?.isAvailable ?? false,
  );
  const [homeLat, setHomeLat] = useState<number>(fosterProfile?.homeLat ?? 0);
  const [homeLng, setHomeLng] = useState<number>(fosterProfile?.homeLng ?? 0);
  const [acceptedSpecies, setAcceptedSpecies] = useState<PetSpecies[]>(
    fosterProfile?.acceptedSpecies ?? ["Dog"],
  );
  const [sizePreference, setSizePreference] = useState<string>(
    fosterProfile?.sizePreference ?? "",
  );
  const [maxDays, setMaxDays] = useState<number>(fosterProfile?.maxDays ?? 3);

  // Change password state
  const [showChangePwd, setShowChangePwd] = useState(false);
  const [currentPwd, setCurrentPwd] = useState("");
  const [newPwd, setNewPwd] = useState("");
  const [confirmNewPwd, setConfirmNewPwd] = useState("");

  const pwdStrength = getPasswordStrength(newPwd);
  const pwdMatch = confirmNewPwd.length > 0 && newPwd === confirmNewPwd;

  // Delete account state
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [deletePassword, setDeletePassword] = useState("");

  const handleChangePassword = async () => {
    if (newPwd !== confirmNewPwd) {
      toast.error("Las contraseñas nuevas no coinciden.");
      return;
    }
    try {
      await changePasswordMutation({
        currentPassword: currentPwd,
        newPassword: newPwd,
      });
      toast.success("Contraseña actualizada correctamente.");
      setShowChangePwd(false);
      setCurrentPwd("");
      setNewPwd("");
      setConfirmNewPwd("");
    } catch {
      toast.error(
        "No se pudo actualizar la contraseña. Verifica que la contraseña actual sea correcta.",
      );
    }
  };

  const handleDeleteAccount = async () => {
    try {
      await deleteAccountMutation({ confirmPassword: deletePassword });
      toast.success("Cuenta eliminada.");
      navigate("/login");
    } catch {
      toast.error("No se pudo eliminar la cuenta. Verifica tu contraseña.");
    }
  };

  const geo = useGeolocation();

  // Auto-center map when geolocation resolves and no pin is set yet
  useEffect(() => {
    if (geo.coords && homeLat === 0 && homeLng === 0) {
      setHomeLat(geo.coords.lat);
      setHomeLng(geo.coords.lng);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [geo.coords]);

  const canSaveFoster = useMemo(
    () => isVolunteer && acceptedSpecies.length > 0 && maxDays > 0,
    [isVolunteer, acceptedSpecies, maxDays],
  );

  const requestLocation = () => {
    geo.request();
  };

  const toggleSpecies = (species: PetSpecies) => {
    setAcceptedSpecies((current) =>
      current.includes(species)
        ? current.filter((s) => s !== species)
        : [...current, species],
    );
  };

  const handleSaveFoster = async () => {
    try {
      await saveProfile({
        homeLat,
        homeLng,
        acceptedSpecies,
        sizePreference: sizePreference || null,
        maxDays,
        isAvailable: isVolunteer,
        availableUntil: null,
      });
      toast.success("Perfil de custodio actualizado correctamente.");
    } catch {
      toast.error(
        "No se pudo guardar el perfil de custodio. Intenta de nuevo.",
      );
    }
  };

  if (profileLoading || fosterLoading) {
    return <PageSpinner />;
  }

  const initials = displayName
    .split(" ")
    .map((w) => w[0] ?? "")
    .join("")
    .toUpperCase()
    .slice(0, 2);

  return (
    <div className="mx-auto max-w-xl px-4 py-8 space-y-6 animate-fade-in-up">
      <h1 className="text-2xl font-bold text-sand-900">Mi perfil</h1>

      {/* ── Identity card ────────────────────────────────────────────── */}
      <div className="rounded-2xl border border-sand-200 field-input p-5">
        <div className="flex items-center gap-4">
          {/* Gradient avatar with initials */}
          <div
            className="flex h-16 w-16 shrink-0 items-center justify-center rounded-full text-xl font-bold text-white select-none shadow-md"
            style={{
              background: `linear-gradient(135deg, var(--color-brand-500) 0%, var(--color-trust-600) 100%)`,
            }}
            aria-hidden="true"
          >
            {initials || "?"}
          </div>

          <div className="min-w-0 flex-1">
            {editingName ? (
              <div className="flex items-center gap-2">
                <Input
                  type="text"
                  value={nameInput}
                  onChange={(e) => setNameInput(e.target.value)}
                  maxLength={100}
                  autoFocus
                  onKeyDown={(e) => {
                    if (e.key === "Enter") void handleSaveName();
                    if (e.key === "Escape") setEditingName(false);
                  }}
                  className="min-w-0 flex-1"
                />
                <Button
                  size="sm"
                  loading={updatingName}
                  disabled={!nameInput.trim()}
                  onClick={() => void handleSaveName()}
                >
                  Guardar
                </Button>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setEditingName(false)}
                >
                  Cancelar
                </Button>
              </div>
            ) : (
              <div className="flex items-center gap-2">
                <span className="truncate text-base font-semibold text-sand-900">
                  {displayName}
                </span>
                <Button variant="ghost" size="sm" onClick={handleEditName}>
                  Editar
                </Button>
              </div>
            )}

            <p className="mt-0.5 truncate text-sm text-sand-500">
              {serverProfile?.email ?? user?.email}
            </p>
            <div className="mt-1 flex items-center gap-2 flex-wrap">
              <Badge variant="neutral">
                {ROLE_LABEL[serverProfile?.role ?? user?.role ?? ""] ??
                  user?.role}
              </Badge>
              {serverProfile?.createdAt && (
                <span className="text-xs text-sand-400">
                  Miembro desde {formatDate(serverProfile.createdAt)}
                </span>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* ── Foster section ────────────────────────────────────────────── */}
      <div className="rounded-2xl border border-sand-200 field-input p-5">
        <h2 className="text-base font-semibold text-sand-800">Voluntariado</h2>
        <p className="mt-1 text-sm text-sand-500">
          Activa esta opción para ofrecer custodia temporal a mascotas
          encontradas.
        </p>

        <button
          type="button"
          role="switch"
          aria-checked={isVolunteer}
          onClick={() => setIsVolunteer((v) => !v)}
          className={`mt-4 flex items-center gap-2 rounded-xl border px-4 py-2 text-sm font-semibold transition-base focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400 ${
            isVolunteer
              ? "border-rescue-500 bg-rescue-50 text-rescue-700"
              : "border-sand-300 bg-white text-sand-600 hover:bg-sand-50"
          }`}
        >
          <span
            className={`h-4 w-4 rounded-full border-2 transition-base ${
              isVolunteer
                ? "border-rescue-500 bg-rescue-500"
                : "border-sand-400 bg-white"
            }`}
            aria-hidden="true"
          />
          Soy custodio voluntario
        </button>

        {/* Resumen guardado cuando colapsado */}
        {!isVolunteer && fosterProfile?.isAvailable && (
          <p className="mt-2 text-xs text-sand-400">
            Perfil anterior:{" "}
            {fosterProfile.acceptedSpecies
              .map((s) => SPECIES_LABEL[s])
              .join(", ")}{" "}
            · máx. {fosterProfile.maxDays} días
          </p>
        )}

        {isVolunteer && (
          <div className="mt-4 space-y-4">
            <div>
              <p className="mb-2 text-sm font-medium text-sand-700">
                Ubicación de referencia
              </p>
              <LastSeenMap
                value={
                  homeLat !== 0 || homeLng !== 0
                    ? { lat: homeLat, lng: homeLng }
                    : null
                }
                onChange={(coords) => {
                  setHomeLat(coords.lat);
                  setHomeLng(coords.lng);
                }}
                userCoords={geo.coords}
                geoStatus={geo.status}
                petName="Tu ubicación de referencia"
                className="h-52 rounded-xl overflow-hidden"
              />
              <div className="mt-2 flex items-center gap-2">
                <Button variant="rescue" size="sm" onClick={requestLocation}>
                  📍 Centrar en mi ubicación actual
                </Button>
                {homeLat !== 0 && (
                  <p className="text-xs text-sand-500">
                    {homeLat.toFixed(5)}, {homeLng.toFixed(5)}
                  </p>
                )}
              </div>
            </div>

            <div>
              <p className="mb-2 text-sm font-medium text-sand-700">
                Especies aceptadas
              </p>
              <div className="flex flex-wrap gap-2">
                {ALL_SPECIES.map((species) => (
                  <button
                    key={species}
                    type="button"
                    onClick={() => toggleSpecies(species)}
                    className={`rounded-full px-3 py-1 text-xs font-semibold transition-base ${
                      acceptedSpecies.includes(species)
                        ? "bg-rescue-100 text-rescue-800"
                        : "bg-sand-100 text-sand-600 hover:bg-sand-200"
                    }`}
                  >
                    {SPECIES_LABEL[species]}
                  </button>
                ))}
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <label className="text-sm text-sand-700">
                Tamaño preferido
                <select
                  value={sizePreference}
                  onChange={(e) => setSizePreference(e.target.value)}
                  className="mt-1 w-full rounded-xl border border-sand-300 px-3 py-2 text-sm focus:border-brand-400 focus:outline-none focus:ring-2 focus:ring-brand-100"
                >
                  <option value="">Sin preferencia</option>
                  <option value="Small">Pequeño</option>
                  <option value="Medium">Mediano</option>
                  <option value="Large">Grande</option>
                </select>
              </label>

              <label className="text-sm text-sand-700">
                Máximo de días
                <input
                  type="number"
                  min={1}
                  max={30}
                  inputMode="numeric"
                  value={maxDays}
                  onChange={(e) => setMaxDays(Number(e.target.value))}
                  className="mt-1 w-full rounded-xl border border-sand-300 px-3 py-2 text-sm focus:border-brand-400 focus:outline-none focus:ring-2 focus:ring-brand-100"
                />
              </label>
            </div>
          </div>
        )}

        {isVolunteer && (
          <Button
            fullWidth
            variant="primary"
            loading={savingFoster}
            disabled={!canSaveFoster}
            onClick={() => void handleSaveFoster()}
            className="mt-5"
          >
            Guardar perfil de custodio
          </Button>
        )}
      </div>

      {/* ── Change password ───────────────────────────────────────────── */}
      <div className="rounded-2xl border border-sand-200 field-input p-5">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-base font-semibold text-sand-800">
              Contraseña
            </h2>
            <p className="mt-0.5 text-sm text-sand-500">
              Actualiza tu contraseña de acceso.
            </p>
          </div>
          <Button
            variant="secondary"
            size="sm"
            onClick={() => setShowChangePwd((v) => !v)}
          >
            {showChangePwd ? "Cancelar" : "Cambiar"}
          </Button>
        </div>

        {showChangePwd && (
          <div className="mt-4 space-y-3">
            <div>
              <label className="mb-1 block text-xs font-medium text-sand-600">
                Contraseña actual
              </label>
              <Input
                type="password"
                value={currentPwd}
                onChange={(e) => setCurrentPwd(e.target.value)}
                autoComplete="current-password"
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-sand-600">
                Nueva contraseña
              </label>
              <Input
                type="password"
                value={newPwd}
                onChange={(e) => setNewPwd(e.target.value)}
                autoComplete="new-password"
                minLength={8}
              />
              {newPwd.length > 0 && (
                <div className="mt-1.5 space-y-1">
                  <div className="flex gap-1">
                    {[1, 2, 3, 4].map((i) => (
                      <div
                        key={i}
                        className={`h-1 flex-1 rounded-full transition-colors ${
                          i <= pwdStrength.score
                            ? pwdStrength.color
                            : "bg-sand-200"
                        }`}
                      />
                    ))}
                  </div>
                  {pwdStrength.label && (
                    <p className="text-[0.7rem] text-sand-500">
                      {pwdStrength.label} · Mínimo 8 caracteres
                    </p>
                  )}
                </div>
              )}
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-sand-600">
                Confirmar nueva contraseña
              </label>
              <Input
                type="password"
                value={confirmNewPwd}
                onChange={(e) => setConfirmNewPwd(e.target.value)}
                autoComplete="new-password"
              />
              {confirmNewPwd.length > 0 && (
                <p
                  className={`mt-1 text-[0.7rem] ${
                    pwdMatch ? "text-rescue-600" : "text-danger-600"
                  }`}
                >
                  {pwdMatch ? "✓ Las contraseñas coinciden" : "× No coinciden"}
                </p>
              )}
            </div>
            <Button
              fullWidth
              loading={changingPassword}
              disabled={
                !currentPwd || !newPwd || newPwd.length < 8 || !pwdMatch
              }
              onClick={() => void handleChangePassword()}
            >
              Guardar nueva contraseña
            </Button>
          </div>
        )}
      </div>

      {/* ── Push notifications ───────────────────────────────────────── */}
      {pushStatus !== "unsupported" && (
        <div className="rounded-2xl border border-sand-200 field-input p-5">
          <div className="flex items-center justify-between gap-4">
            <div>
              <h2 className="text-base font-semibold text-sand-800">
                Notificaciones push
              </h2>
              <p className="mt-0.5 text-sm text-sand-500">
                {pushStatus === "subscribed"
                  ? "Recibirás alertas aunque tengas la app cerrada."
                  : pushStatus === "denied"
                    ? "Bloqueadas en el navegador. Actívalas desde Configuración."
                    : "Recibe alertas de avistamientos y actualizaciones en tiempo real."}
              </p>
            </div>
            <button
              type="button"
              role="switch"
              aria-checked={pushStatus === "subscribed"}
              disabled={pushStatus === "loading" || pushStatus === "denied"}
              onClick={() =>
                pushStatus === "subscribed"
                  ? void pushUnsubscribe()
                  : void pushSubscribe()
              }
              className={`relative h-6 w-11 shrink-0 rounded-full transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 disabled:cursor-not-allowed disabled:opacity-50 ${
                pushStatus === "subscribed" ? "bg-rescue-500" : "bg-sand-300"
              }`}
            >
              <span
                className={`absolute top-0.5 h-5 w-5 rounded-full bg-white shadow transition-transform ${
                  pushStatus === "subscribed"
                    ? "translate-x-5"
                    : "translate-x-0.5"
                }`}
              />
            </button>
          </div>
        </div>
      )}

      {/* ── Delete account ────────────────────────────────────────────── */}
      <div className="rounded-2xl border border-danger-200 bg-danger-50/40 p-5">
        <h2 className="text-base font-semibold text-danger-700">
          Zona de peligro
        </h2>
        <p className="mt-1 text-sm text-sand-600">
          Eliminar tu cuenta borrará todos tus datos y mascotas registradas.
          Esta acción es irreversible.
        </p>

        {!showDeleteConfirm ? (
          <Button
            variant="danger"
            size="sm"
            className="mt-4"
            onClick={() => setShowDeleteConfirm(true)}
          >
            Eliminar cuenta
          </Button>
        ) : (
          <div className="mt-4 space-y-3">
            <p className="text-sm font-medium text-danger-700">
              Ingresa tu contraseña para confirmar la eliminación:
            </p>
            <Input
              type="password"
              value={deletePassword}
              onChange={(e) => setDeletePassword(e.target.value)}
              autoComplete="current-password"
              placeholder="Tu contraseña actual"
            />
            <div className="flex gap-2">
              <Button
                variant="danger"
                loading={deletingAccount}
                disabled={!deletePassword}
                onClick={() => void handleDeleteAccount()}
              >
                Sí, eliminar mi cuenta
              </Button>
              <Button
                variant="secondary"
                onClick={() => {
                  setShowDeleteConfirm(false);
                  setDeletePassword("");
                }}
              >
                Cancelar
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
