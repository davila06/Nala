import { Link, useParams } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import { usePublicClinicProfile } from "../hooks/useClinics";
import { BillboardBanner } from "@/features/advertising/components/BillboardBanner";

export default function ClinicPublicProfilePage() {
  const { clinicId = "" } = useParams();
  const { data: clinic, isLoading, isError } = usePublicClinicProfile(clinicId);

  if (isLoading) {
    return (
      <main className="mx-auto max-w-3xl px-4 py-12">
        <div className="h-64 animate-pulse rounded-3xl bg-sand-100" />
      </main>
    );
  }

  if (isError || !clinic) {
    return (
      <main className="mx-auto max-w-3xl px-4 py-12 text-center">
        <p className="text-sand-500">No encontramos esta clínica.</p>
        <Link
          to="/clinicas"
          className="mt-4 inline-block text-sm font-bold text-brand-600"
        >
          Volver al directorio
        </Link>
      </main>
    );
  }

  return (
    <>
      <Helmet>
        <title>{clinic.name} · PawTrack CR</title>
      </Helmet>
      <main className="mx-auto max-w-3xl space-y-6 px-4 py-8">
        <Link to="/clinicas" className="text-sm font-semibold text-brand-600">
          ← Directorio
        </Link>
        <BillboardBanner placement="ClinicProfile" />
        <section className="overflow-hidden rounded-3xl border border-sand-200 bg-surface">
          <div className="flex flex-wrap items-center gap-4 border-b border-sand-100 bg-sand-50 p-6">
            {clinic.logoUrl ? (
              <img
                src={clinic.logoUrl}
                alt={clinic.name}
                className="h-20 w-20 rounded-2xl object-cover"
              />
            ) : (
              <div
                className="flex h-20 w-20 items-center justify-center rounded-2xl bg-sand-100 text-4xl"
                aria-hidden="true"
              >
                🏥
              </div>
            )}
            <div>
              <div className="flex flex-wrap items-center gap-2">
                <h1 className="text-2xl font-black text-ink-900">
                  {clinic.name}
                </h1>
                {clinic.isFeatured && (
                  <span className="rounded-full bg-trust-100 px-2 py-1 text-xs font-bold text-trust-700">
                    Verificada
                  </span>
                )}
              </div>
              <p className="mt-1 text-sm text-sand-500">{clinic.address}</p>
              {clinic.isEmergency24h && (
                <p className="mt-2 text-xs font-bold text-danger-600">
                  Emergencias 24/7
                </p>
              )}
            </div>
          </div>
          <div className="grid gap-6 p-6 sm:grid-cols-2">
            <div className="space-y-4">
              <div>
                <h2 className="text-sm font-bold text-sand-800">
                  Sobre la clínica
                </h2>
                <p className="mt-1 whitespace-pre-wrap text-sm text-sand-600">
                  {clinic.description ||
                    "Perfil profesional afiliado a PawTrack CR."}
                </p>
              </div>
              {clinic.services && (
                <div>
                  <h2 className="text-sm font-bold text-sand-800">Servicios</h2>
                  <p className="mt-1 whitespace-pre-wrap text-sm text-sand-600">
                    {clinic.services}
                  </p>
                </div>
              )}
              {clinic.openingHours && (
                <div>
                  <h2 className="text-sm font-bold text-sand-800">Horario</h2>
                  <p className="mt-1 whitespace-pre-wrap text-sm text-sand-600">
                    {clinic.openingHours}
                  </p>
                </div>
              )}
            </div>
            <div className="space-y-3 text-sm text-sand-600">
              {clinic.phoneNumber && (
                <a
                  className="block font-semibold text-brand-600"
                  href={`tel:${clinic.phoneNumber}`}
                >
                  📞 {clinic.phoneNumber}
                </a>
              )}
              {clinic.isEmergency24h && clinic.emergencyPhone && (
                <a
                  className="block font-semibold text-danger-600"
                  href={`tel:${clinic.emergencyPhone}`}
                >
                  🚨 Emergencias: {clinic.emergencyPhone}
                </a>
              )}
              {clinic.website && (
                <a
                  className="block font-semibold text-brand-600"
                  href={clinic.website}
                  target="_blank"
                  rel="noreferrer"
                >
                  🌐 Sitio web
                </a>
              )}
              <a
                className="block font-semibold text-brand-600"
                href={`https://www.google.com/maps/search/?api=1&query=${clinic.lat},${clinic.lng}`}
                target="_blank"
                rel="noreferrer"
              >
                📍 Ver ubicación
              </a>
            </div>
          </div>
        </section>
      </main>
    </>
  );
}
