import { useState } from "react";
import { Helmet } from "react-helmet-async";
import { Link, useNavigate } from "react-router-dom";
import { Alert } from "@/shared/ui/Alert";
import { Button, Input } from "@/shared/ui";
import {
  LastSeenMap,
  type LastSeenCoords,
} from "@/features/lost-pets/components/LastSeenMap";
import {
  SERVICE_PROVIDER_CATEGORY_LABELS,
  type ServiceProviderCategory,
} from "../api/serviceProvidersApi";
import { useRegisterServiceProvider } from "../hooks/useServiceProviders";

const categories = Object.entries(SERVICE_PROVIDER_CATEGORY_LABELS) as [
  ServiceProviderCategory,
  string,
][];

export default function ServiceProviderRegistrationPage() {
  const navigate = useNavigate();
  const { mutate: register, isPending, error } = useRegisterServiceProvider();
  const [coords, setCoords] = useState<LastSeenCoords | null>(null);
  const [validationError, setValidationError] = useState("");
  const [form, setForm] = useState({
    name: "",
    description: "",
    category: "Trainer" as ServiceProviderCategory,
    address: "",
    contactEmail: "",
    password: "",
    confirmPassword: "",
  });
  const setField = (key: keyof typeof form, value: string) =>
    setForm((current) => ({ ...current, [key]: value }));

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    setValidationError("");
    if (form.password !== form.confirmPassword)
      return setValidationError("Las contrasenas no coinciden.");
    if (!coords)
      return setValidationError(
        "Marca la ubicacion publica de tu servicio en el mapa.",
      );
    register(
      {
        name: form.name,
        description: form.description,
        category: form.category,
        address: form.address,
        lat: coords.lat,
        lng: coords.lng,
        contactEmail: form.contactEmail,
        password: form.password,
      },
      { onSuccess: () => navigate("/servicio/pendiente") },
    );
  };

  return (
    <main className="min-h-dvh bg-surface px-4 py-10 sm:px-6">
      <Helmet>
        <title>Registrar servicio · PawTrack CR</title>
      </Helmet>
      <div className="mx-auto grid max-w-5xl gap-8 lg:grid-cols-[0.8fr_1.2fr]">
        <aside className="border-b border-sand-200 pb-6 lg:border-b-0 lg:border-r lg:pr-8">
          <p className="text-xs font-semibold uppercase tracking-wide text-brand-600">
            Directorio PawTrack
          </p>
          <h1 className="mt-2 font-display text-3xl font-semibold text-ink-900">
            Haz visible tu servicio.
          </h1>
          <p className="mt-3 text-sm leading-relaxed text-sand-600">
            Las solicitudes se revisan antes de mostrarse en el directorio
            publico.
          </p>
        </aside>
        <section>
          {validationError ? (
            <Alert variant="error" className="mb-4">
              {validationError}
            </Alert>
          ) : null}
          {error ? (
            <Alert variant="error" className="mb-4">
              No se pudo enviar la solicitud. Verifica los datos e intenta de
              nuevo.
            </Alert>
          ) : null}
          <form onSubmit={handleSubmit} className="space-y-4">
            <label className="block text-sm font-medium text-sand-700">
              Nombre comercial
              <Input
                value={form.name}
                onChange={(event) => setField("name", event.target.value)}
                required
              />
            </label>
            <label className="block text-sm font-medium text-sand-700">
              Categoria
              <select
                value={form.category}
                onChange={(event) => setField("category", event.target.value)}
                className="mt-1 w-full rounded-lg border border-sand-200 bg-surface px-3 py-2 text-sm"
              >
                {categories.map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </label>
            <label className="block text-sm font-medium text-sand-700">
              Descripcion
              <textarea
                value={form.description}
                onChange={(event) =>
                  setField("description", event.target.value)
                }
                rows={3}
                required
                className="mt-1 w-full rounded-lg border border-sand-200 px-3 py-2 text-sm"
              />
            </label>
            <label className="block text-sm font-medium text-sand-700">
              Direccion publica
              <Input
                value={form.address}
                onChange={(event) => setField("address", event.target.value)}
                required
              />
            </label>
            <div>
              <p className="mb-1 text-sm font-medium text-sand-700">
                Ubicacion publica
              </p>
              <div className="h-52 overflow-hidden rounded-lg border border-sand-200">
                <LastSeenMap
                  value={coords}
                  onChange={setCoords}
                  userCoords={null}
                  geoStatus="idle"
                  petName="Tu servicio"
                />
              </div>
            </div>
            <label className="block text-sm font-medium text-sand-700">
              Correo electronico
              <Input
                type="email"
                value={form.contactEmail}
                onChange={(event) =>
                  setField("contactEmail", event.target.value)
                }
                required
              />
            </label>
            <div className="grid gap-4 sm:grid-cols-2">
              <label className="block text-sm font-medium text-sand-700">
                Contrasena
                <Input
                  type="password"
                  value={form.password}
                  onChange={(event) => setField("password", event.target.value)}
                  required
                />
              </label>
              <label className="block text-sm font-medium text-sand-700">
                Confirmar contrasena
                <Input
                  type="password"
                  value={form.confirmPassword}
                  onChange={(event) =>
                    setField("confirmPassword", event.target.value)
                  }
                  required
                />
              </label>
            </div>
            <Button type="submit" fullWidth loading={isPending}>
              Enviar solicitud
            </Button>
          </form>
          <p className="mt-5 text-center text-sm text-sand-600">
            Ya tienes cuenta?{" "}
            <Link
              to="/login"
              className="font-semibold text-brand-600 hover:underline"
            >
              Inicia sesion
            </Link>
          </p>
        </section>
      </div>
    </main>
  );
}
