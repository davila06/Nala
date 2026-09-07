import { Helmet } from "react-helmet-async";
import { Link } from "react-router-dom";
import { Skeleton } from "@/shared/ui/Spinner";
import { SERVICE_PROVIDER_CATEGORY_LABELS } from "../api/serviceProvidersApi";
import {
  useMyProviderServices,
  useMyServiceProvider,
} from "../hooks/useServiceProviders";

export default function ServiceProviderDashboardPage() {
  const { data: provider, isLoading } = useMyServiceProvider();
  const { data: services = [] } = useMyProviderServices();

  if (isLoading)
    return (
      <div className="mx-auto max-w-3xl p-8">
        <Skeleton className="h-48 rounded-xl" />
      </div>
    );
  if (!provider)
    return (
      <main className="mx-auto max-w-lg p-8 text-center text-sand-600">
        No tienes un servicio registrado.
      </main>
    );

  const isActive = provider.status === "Active";
  return (
    <main className="mx-auto max-w-3xl space-y-6 px-4 py-8">
      <Helmet>
        <title>{provider.name} · Portal PawTrack CR</title>
      </Helmet>
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-sm font-semibold text-brand-600">
            {SERVICE_PROVIDER_CATEGORY_LABELS[provider.category]}
          </p>
          <h1 className="font-display text-3xl font-semibold text-ink-900">
            {provider.name}
          </h1>
        </div>
        <Link
          to="/servicio/portal/servicios"
          className="rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700"
        >
          Gestionar servicios
        </Link>
      </header>
      <section
        className={`rounded-xl border p-4 ${isActive ? "border-rescue-200 bg-rescue-50" : "border-warn-200 bg-warn-50"}`}
      >
        <p className="font-semibold text-ink-900">
          {isActive
            ? "Perfil activo"
            : provider.status === "Rejected"
              ? "Solicitud rechazada"
              : provider.status === "Suspended"
                ? "Perfil suspendido"
                : "Perfil en revision"}
        </p>
        <p className="mt-1 text-sm text-sand-600">
          {isActive
            ? "Tu perfil aparece en el directorio. Mantiene tu oferta actualizada para recibir futuras reservas."
            : "Tu perfil no aparece en el directorio hasta completar la revision administrativa."}
        </p>
      </section>
      <div className="grid gap-4 sm:grid-cols-2">
        <Link
          to="/servicio/portal/servicios"
          className="rounded-xl border border-sand-100 bg-surface p-5 transition hover:border-brand-200 hover:bg-brand-50"
        >
          <p className="text-3xl font-semibold text-brand-700">
            {services.length}
          </p>
          <p className="mt-1 text-sm text-sand-600">Servicios publicados</p>
        </Link>
        <Link
          to="/servicio/portal/perfil"
          className="rounded-xl border border-sand-100 bg-surface p-5 transition hover:border-brand-200 hover:bg-brand-50"
        >
          <p className="text-sm font-semibold text-ink-900">Perfil comercial</p>
          <p className="mt-1 text-sm text-sand-600">
            Actualiza descripcion, categoria y ubicacion.
          </p>
        </Link>
        <Link
          to="/servicio/portal/reservas"
          className="rounded-xl border border-sand-100 bg-surface p-5 transition hover:border-brand-200 hover:bg-brand-50"
        >
          <p className="text-sm font-semibold text-ink-900">
            Reservas entrantes
          </p>
          <p className="mt-1 text-sm text-sand-600">
            Confirma, inicia y completa los servicios solicitados.
          </p>
        </Link>
        <Link
          to="/servicio/portal/verificacion"
          className="rounded-xl border border-sand-100 bg-surface p-5 transition hover:border-brand-200 hover:bg-brand-50"
        >
          <p className="text-sm font-semibold text-ink-900">Verificacion</p>
          <p className="mt-1 text-sm text-sand-600">
            Carga y consulta tu evidencia privada.
          </p>
        </Link>
      </div>
    </main>
  );
}
