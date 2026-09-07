import { useEffect, useState } from "react";
import { Helmet } from "react-helmet-async";
import { Button, Input } from "@/shared/ui";
import { Alert } from "@/shared/ui/Alert";
import { Skeleton } from "@/shared/ui/Spinner";
import { toast } from "@/shared/lib/toast";
import {
  SERVICE_PROVIDER_CATEGORY_LABELS,
  type ServiceProviderCategory,
} from "../api/serviceProvidersApi";
import {
  useMyServiceProvider,
  useUpdateServiceProviderProfile,
} from "../hooks/useServiceProviders";

const categories = Object.entries(SERVICE_PROVIDER_CATEGORY_LABELS) as [
  ServiceProviderCategory,
  string,
][];

export default function ServiceProviderProfilePage() {
  const { data: provider, isLoading } = useMyServiceProvider();
  const update = useUpdateServiceProviderProfile();
  const [form, setForm] = useState({
    name: "",
    description: "",
    category: "Trainer" as ServiceProviderCategory,
    address: "",
    lat: "",
    lng: "",
    phoneNumber: "",
    website: "",
  });
  useEffect(() => {
    if (!provider) return;
    setForm({
      name: provider.name,
      description: provider.description,
      category: provider.category,
      address: provider.address,
      lat: String(provider.lat),
      lng: String(provider.lng),
      phoneNumber: provider.phoneNumber ?? "",
      website: provider.website ?? "",
    });
  }, [provider]);
  const setField = (key: keyof typeof form, value: string) =>
    setForm((current) => ({ ...current, [key]: value }));
  const submit = (event: React.FormEvent) => {
    event.preventDefault();
    update.mutate(
      {
        name: form.name,
        description: form.description,
        category: form.category,
        address: form.address,
        lat: Number(form.lat),
        lng: Number(form.lng),
        phoneNumber: form.phoneNumber || undefined,
        website: form.website || undefined,
      },
      {
        onSuccess: () => toast.success("Perfil actualizado"),
        onError: () => toast.error("No se pudo actualizar el perfil."),
      },
    );
  };
  if (isLoading)
    return (
      <div className="mx-auto max-w-2xl p-8">
        <Skeleton className="h-64 rounded-xl" />
      </div>
    );
  if (!provider)
    return (
      <main className="mx-auto max-w-2xl p-8 text-center text-sand-600">
        No tienes un perfil registrado.
      </main>
    );
  return (
    <main className="mx-auto max-w-2xl space-y-5 px-4 py-8">
      <Helmet>
        <title>Editar perfil · PawTrack CR</title>
      </Helmet>
      <header>
        <h1 className="font-display text-2xl font-semibold text-ink-900">
          Perfil comercial
        </h1>
        <p className="text-sm text-sand-600">
          La ubicacion y datos de contacto se muestran segun el estado de tu
          perfil.
        </p>
      </header>
      {update.isError ? (
        <Alert variant="error">No se pudieron guardar los cambios.</Alert>
      ) : null}
      <form
        onSubmit={submit}
        className="space-y-4 rounded-xl border border-sand-100 bg-surface p-5"
      >
        <label className="block text-sm font-medium text-sand-700">
          Nombre
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
            className="mt-1 w-full rounded-lg border border-sand-200 bg-surface px-3 py-2"
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
            onChange={(event) => setField("description", event.target.value)}
            rows={4}
            className="mt-1 w-full rounded-lg border border-sand-200 px-3 py-2"
            required
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
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="block text-sm font-medium text-sand-700">
            Latitud
            <Input
              type="number"
              step="any"
              value={form.lat}
              onChange={(event) => setField("lat", event.target.value)}
              required
            />
          </label>
          <label className="block text-sm font-medium text-sand-700">
            Longitud
            <Input
              type="number"
              step="any"
              value={form.lng}
              onChange={(event) => setField("lng", event.target.value)}
              required
            />
          </label>
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="block text-sm font-medium text-sand-700">
            Telefono
            <Input
              value={form.phoneNumber}
              onChange={(event) => setField("phoneNumber", event.target.value)}
            />
          </label>
          <label className="block text-sm font-medium text-sand-700">
            Sitio web
            <Input
              type="url"
              value={form.website}
              onChange={(event) => setField("website", event.target.value)}
            />
          </label>
        </div>
        <Button type="submit" loading={update.isPending}>
          Guardar cambios
        </Button>
      </form>
    </main>
  );
}
