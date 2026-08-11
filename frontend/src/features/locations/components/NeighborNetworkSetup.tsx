import { useState } from "react";
import { Drawer } from "@/shared/ui/Drawer";
import { Button, Input } from "@/shared/ui";
import { toast } from "@/shared/lib/toast";
import { useEnrollNeighbor } from "../hooks/useNeighbor";

interface NeighborNetworkSetupProps {
  isOpen: boolean;
  onClose: () => void;
}

const RADIUS_OPTIONS = [
  { label: "100 m (mi cuadra)", value: 100 },
  { label: "300 m (barrio cercano)", value: 300 },
  { label: "500 m (recomendado)", value: 500 },
  { label: "1 km", value: 1000 },
  { label: "2 km (toda la colonia)", value: 2000 },
];

export function NeighborNetworkSetup({
  isOpen,
  onClose,
}: NeighborNetworkSetupProps) {
  const [phone, setPhone] = useState("");
  const [phoneError, setPhoneError] = useState("");
  const [radius, setRadius] = useState(500);
  const enroll = useEnrollNeighbor();

  const validate = () => {
    const p = phone.trim();
    const digits = p.replace(/\D/g, "");
    if (digits.length < 8) {
      setPhoneError("Ingresa al menos 8 dígitos.");
      return false;
    }
    setPhoneError("");
    return true;
  };

  const handleSubmit = () => {
    if (!validate()) return;
    enroll.mutate(
      { phone: phone.trim(), radiusMeters: radius },
      {
        onSuccess: () => {
          toast.success(
            "¡Guardia Vecinal activada! Recibirás alertas cuando haya mascotas perdidas cerca.",
          );
          onClose();
        },
        onError: (err: unknown) =>
          toast.error(
            (err as { response?: { data?: { detail?: string } } })?.response
              ?.data?.detail ?? "No se pudo activar. Intenta de nuevo.",
          ),
      },
    );
  };

  return (
    <Drawer
      isOpen={isOpen}
      onClose={onClose}
      title="Activar Guardia Vecinal 🏘️"
      side="bottom"
    >
      <div className="space-y-5 pb-safe">
        {/* Benefit explanation */}
        <div className="rounded-xl border border-trust-200 bg-trust-50 p-4 space-y-2">
          <p className="text-sm font-semibold text-trust-800">
            ¿Qué es la Guardia Vecinal?
          </p>
          <ul className="space-y-1.5 text-xs text-trust-700">
            <li className="flex items-start gap-1.5">
              <span aria-hidden="true" className="mt-0.5">
                🔔
              </span>
              Recibe alertas al instante cuando una mascota se pierde en tu
              cuadra
            </li>
            <li className="flex items-start gap-1.5">
              <span aria-hidden="true" className="mt-0.5">
                👀
              </span>
              Ayuda a vecinos a recuperar a sus mascotas — gana puntos como
              rescatador
            </li>
            <li className="flex items-start gap-1.5">
              <span aria-hidden="true" className="mt-0.5">
                🔒
              </span>
              Tu número nunca es visible públicamente
            </li>
          </ul>
        </div>

        {/* Phone input */}
        <div>
          <label
            htmlFor="neighbor-phone"
            className="mb-1 block text-xs font-medium text-sand-600"
          >
            Tu número de teléfono CR
          </label>
          <Input
            id="neighbor-phone"
            type="tel"
            placeholder="+506 8888-0000"
            value={phone}
            onChange={(e) => {
              setPhone(e.target.value);
              if (phoneError) setPhoneError("");
            }}
            aria-describedby={phoneError ? "neighbor-phone-err" : undefined}
            aria-invalid={!!phoneError}
          />
          {phoneError && (
            <p
              id="neighbor-phone-err"
              role="alert"
              className="mt-1 text-xs text-danger-600"
            >
              {phoneError}
            </p>
          )}
          <p className="mt-1 text-xs text-sand-400">
            Formato: 8 dígitos o +506 XXXX-XXXX
          </p>
        </div>

        {/* Radius selector */}
        <div>
          <p className="mb-2 text-xs font-medium text-sand-600">
            Radio de alerta
          </p>
          <div className="grid grid-cols-1 gap-2">
            {RADIUS_OPTIONS.map((opt) => (
              <button
                key={opt.value}
                type="button"
                onClick={() => setRadius(opt.value)}
                className={[
                  "flex items-center justify-between rounded-xl border px-4 py-2.5 text-sm font-medium transition-all",
                  radius === opt.value
                    ? "border-brand-500 bg-brand-50 text-brand-800"
                    : "border-sand-200 bg-white text-sand-700 hover:border-sand-300",
                ].join(" ")}
              >
                {opt.label}
                {radius === opt.value && (
                  <svg
                    viewBox="0 0 16 16"
                    fill="currentColor"
                    className="h-4 w-4 text-brand-500"
                    aria-hidden="true"
                  >
                    <path d="M13.78 4.22a.75.75 0 0 1 0 1.06l-7.25 7.25a.75.75 0 0 1-1.06 0L2.22 9.28a.75.75 0 0 1 1.06-1.06L6 10.94l6.72-6.72a.75.75 0 0 1 1.06 0Z" />
                  </svg>
                )}
              </button>
            ))}
          </div>
        </div>

        <Button fullWidth loading={enroll.isPending} onClick={handleSubmit}>
          Activar Guardia Vecinal
        </Button>

        <p className="text-center text-xs text-sand-400">
          Puedes desactivarte en cualquier momento desde tu perfil.
        </p>
      </div>
    </Drawer>
  );
}
