import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import { motion } from "framer-motion";
import { Button, Input } from "@/shared/ui";
import { Alert } from "@/shared/ui/Alert";
import { useRegisterStore } from "../hooks/useStores";
import { LastSeenMap } from "@/features/lost-pets/components/LastSeenMap";
import type { LastSeenCoords } from "@/features/lost-pets/components/LastSeenMap";

export default function StoreRegistrationPage() {
  const navigate = useNavigate();
  const { mutate: register, isPending, error } = useRegisterStore();
  const [coords, setCoords] = useState<LastSeenCoords | null>(null);
  const [form, setForm] = useState({
    name: "",
    description: "",
    address: "",
    contactEmail: "",
    password: "",
    confirmPassword: "",
  });
  const [validationError, setValidationError] = useState("");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setValidationError("");
    if (form.password !== form.confirmPassword) {
      setValidationError("Las contraseñas no coinciden.");
      return;
    }
    if (!coords) {
      setValidationError("Marca la ubicación de tu tienda en el mapa.");
      return;
    }
    register(
      {
        name: form.name,
        description: form.description,
        address: form.address,
        lat: coords.lat,
        lng: coords.lng,
        contactEmail: form.contactEmail,
        password: form.password,
      },
      { onSuccess: () => navigate("/tienda/pendiente") },
    );
  };

  const field = (key: keyof typeof form) => ({
    value: form[key],
    onChange: (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
      setForm((f) => ({ ...f, [key]: e.target.value })),
  });

  return (
    <div className="min-h-dvh lg:grid lg:grid-cols-[1fr_560px]">
      <Helmet>
        <title>Registra tu tienda — PawTrack CR</title>
      </Helmet>

      {/* Brand panel */}
      <div className="hidden lg:flex flex-col justify-between bg-trust-900 px-12 py-14 text-white">
        <div className="flex items-center gap-3">
          <span className="flex h-10 w-10 items-center justify-center rounded-2xl bg-rescue-500 text-xl">
            🛒
          </span>
          <span className="font-display text-2xl font-semibold tracking-tight">
            PawTrack CR
          </span>
        </div>
        <div className="space-y-6">
          <p className="font-display text-4xl leading-snug font-medium">
            Llega a los dueños de mascotas
            <br />
            <em className="not-italic text-rescue-400">en tu zona.</em>
          </p>
          <ul className="space-y-3 text-trust-200 text-sm leading-relaxed">
            {[
              "Aparece en el mapa de PawTrack CR",
              "Catálogo de productos visible al público",
              "Recibe pedidos in-app con pago SINPE",
              "Panel de órdenes en tiempo real",
            ].map((f) => (
              <li key={f} className="flex items-start gap-2.5">
                <span className="mt-0.5 text-rescue-400">✓</span>
                {f}
              </li>
            ))}
          </ul>
        </div>
        <p className="text-trust-400 text-sm">
          El plan básico es gratis. Planes avanzados desde ₡12,000/mes.
        </p>
      </div>

      {/* Form panel */}
      <div className="flex min-h-dvh flex-col items-center justify-center px-6 py-12 lg:px-12 bg-surface">
        <div className="w-full max-w-sm">
          <div className="mb-8">
            <h1 className="font-display text-3xl font-semibold text-sand-900">
              Registra tu tienda
            </h1>
            <p className="mt-1 text-sm text-sand-500">
              Aprobación en menos de 48 h.
            </p>
          </div>

          {validationError && (
            <Alert variant="error" className="mb-4">
              {validationError}
            </Alert>
          )}
          {error && (
            <Alert variant="error" className="mb-4">
              {(
                error as { response?: { data?: { detail?: string } } }
              )?.response?.data?.detail?.includes("duplicate_email")
                ? "Ya existe una cuenta con ese correo. ¿Quieres iniciar sesión?"
                : "No se pudo registrar la tienda. Verifica los datos e intenta de nuevo."}
            </Alert>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="mb-1 block text-xs font-medium text-sand-600">
                Nombre de la tienda *
              </label>
              <Input
                placeholder="Petshop La Patita"
                {...field("name")}
                required
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-sand-600">
                Descripción *
              </label>
              <textarea
                {...field("description")}
                required
                rows={3}
                placeholder="¿Qué ofreces? ¿Para qué mascotas?"
                className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm placeholder:text-sand-400 focus:outline-none focus:ring-2 focus:ring-brand-400"
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-sand-600">
                Dirección *
              </label>
              <Input
                placeholder="100m sur del parque central"
                {...field("address")}
                required
              />
            </div>
            <div>
              <label className="mb-1.5 block text-xs font-medium text-sand-600">
                Ubicación en el mapa *
                <span className="ml-1 text-sand-400 font-normal">
                  (toca para marcar)
                </span>
              </label>
              <div className="h-48 rounded-2xl overflow-hidden border border-sand-200">
                <LastSeenMap value={coords} onChange={setCoords} />
              </div>
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-sand-600">
                Correo electrónico *
              </label>
              <Input
                type="email"
                placeholder="tienda@ejemplo.com"
                {...field("contactEmail")}
                required
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-sand-600">
                Contraseña *
              </label>
              <Input
                type="password"
                placeholder="Mínimo 8 caracteres"
                {...field("password")}
                required
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-sand-600">
                Confirmar contraseña *
              </label>
              <Input
                type="password"
                placeholder="Repetir contraseña"
                {...field("confirmPassword")}
                required
              />
            </div>

            <Button type="submit" fullWidth loading={isPending}>
              Enviar solicitud
            </Button>
          </form>

          <p className="mt-6 text-center text-xs text-sand-500">
            ¿Ya tienes cuenta?{" "}
            <Link
              to="/login"
              className="font-semibold text-brand-600 hover:underline"
            >
              Inicia sesión
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
