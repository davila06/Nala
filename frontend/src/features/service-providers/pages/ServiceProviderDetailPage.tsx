import { useState } from "react";
import { Helmet } from "react-helmet-async";
import { Link, useParams } from "react-router-dom";
import { Button } from "@/shared/ui";
import { Skeleton } from "@/shared/ui/Spinner";
import { toast } from "@/shared/lib/toast";
import { usePets } from "@/features/pets/hooks/usePets";
import { useAuthStore } from "@/features/auth/store/authStore";
import {
  SERVICE_MODALITY_LABELS,
  SERVICE_PROVIDER_CATEGORY_LABELS,
} from "../api/serviceProvidersApi";
import {
  useCreateProviderBooking,
  useProviderServiceAvailability,
  usePublicProviderServices,
  useServiceProviderDetail,
} from "../hooks/useServiceProviders";
import { BillboardBanner } from "@/features/advertising/components/BillboardBanner";

function todayCostaRica() {
  return new Intl.DateTimeFormat("en-CA", {
    timeZone: "America/Costa_Rica",
  }).format(new Date());
}

export default function ServiceProviderDetailPage() {
  const { id = "" } = useParams();
  const { data: provider, isLoading, isError } = useServiceProviderDetail(id);
  const { data: services = [] } = usePublicProviderServices(id);
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  // Anonymous visitors can't call GET /api/pets — gating avoids a 401 that
  // would otherwise hard-redirect them away from this public page.
  const { data: pets = [] } = usePets(isAuthenticated);
  const createBooking = useCreateProviderBooking();
  const [selectedServiceId, setSelectedServiceId] = useState("");
  const [date, setDate] = useState(todayCostaRica);
  const [selectedSlot, setSelectedSlot] = useState("");
  const [petId, setPetId] = useState("");
  const { data: slots = [], isLoading: slotsLoading } =
    useProviderServiceAvailability(selectedServiceId, date);
  const selectedService = services.find(
    (service) => service.id === selectedServiceId,
  );

  const reserve = () => {
    if (!selectedService || !selectedSlot || !petId)
      return toast.error("Selecciona un servicio, horario y mascota.");
    createBooking.mutate(
      {
        providerServiceId: selectedService.id,
        petId,
        startsAt: selectedSlot,
        quantity: 1,
      },
      {
        onSuccess: () => {
          toast.success("Solicitud de reserva enviada");
          setSelectedSlot("");
        },
        onError: () =>
          toast.error(
            "No se pudo crear la reserva. El horario podria estar ocupado.",
          ),
      },
    );
  };
  if (isLoading)
    return (
      <div className="mx-auto max-w-3xl p-6">
        <Skeleton className="h-64 rounded-xl" />
      </div>
    );
  if (isError || !provider)
    return (
      <main className="mx-auto max-w-3xl p-8 text-center text-sand-600">
        Proveedor no encontrado.{" "}
        <Link to="/servicios" className="text-brand-600 underline">
          Volver al directorio
        </Link>
      </main>
    );
  return (
    <main className="mx-auto max-w-3xl space-y-5 px-4 py-8">
      <Helmet>
        <title>{provider.name} · PawTrack CR</title>
      </Helmet>
      <Link to="/servicios" className="text-sm font-medium text-brand-600">
        Volver a servicios
      </Link>
      <BillboardBanner placement="ServiceProviderProfile" />
      <section className="rounded-xl border border-sand-100 bg-surface p-6">
        <p className="text-sm font-semibold text-brand-600">
          {SERVICE_PROVIDER_CATEGORY_LABELS[provider.category]}
        </p>
        {provider.isVerified ? (
          <p className="mt-1 text-sm font-semibold text-rescue-700">
            Verificado por PawTrack CR
          </p>
        ) : null}
        <h1 className="mt-2 font-display text-3xl font-semibold text-ink-900">
          {provider.name}
        </h1>
        <p className="mt-4 leading-relaxed text-sand-600">
          {provider.description}
        </p>
        <p className="mt-5 text-sm text-sand-500">{provider.address}</p>
        {provider.phoneNumber ? (
          <p className="mt-1 text-sm text-sand-500">{provider.phoneNumber}</p>
        ) : null}
        {provider.website ? (
          <a
            className="mt-4 inline-block text-sm font-semibold text-brand-600 hover:underline"
            href={provider.website}
            rel="noreferrer"
            target="_blank"
          >
            Visitar sitio web
          </a>
        ) : null}
      </section>
      <section
        id="reservas"
        className="space-y-4 rounded-xl border border-sand-100 bg-surface p-6"
      >
        <div>
          <h2 className="font-display text-2xl font-semibold text-ink-900">
            Servicios y disponibilidad
          </h2>
          <p className="mt-1 text-sm text-sand-600">
            Elige un servicio y un horario disponible.
          </p>
        </div>
        {services.length === 0 ? (
          <p className="text-sm text-sand-500">
            Este proveedor aun no ha publicado servicios.
          </p>
        ) : (
          <>
            <div className="grid gap-3 sm:grid-cols-2">
              {services.map((service) => (
                <button
                  key={service.id}
                  type="button"
                  onClick={() => {
                    setSelectedServiceId(service.id);
                    setSelectedSlot("");
                  }}
                  className={`rounded-lg border p-4 text-left ${selectedServiceId === service.id ? "border-brand-600 bg-brand-50" : "border-sand-200 hover:border-brand-300"}`}
                >
                  <p className="font-semibold text-ink-900">{service.name}</p>
                  <p className="mt-1 text-sm text-sand-600">
                    {SERVICE_MODALITY_LABELS[service.modality]} ·{" "}
                    {service.durationMinutes} min
                  </p>
                  <p className="mt-2 text-sm font-semibold text-rescue-700">
                    CRC {service.priceCrc.toLocaleString("es-CR")}
                  </p>
                </button>
              ))}
            </div>
            {selectedService ? (
              <div className="space-y-3 border-t border-sand-100 pt-4">
                <label className="block text-sm font-medium text-sand-700">
                  Fecha
                  <input
                    type="date"
                    min={todayCostaRica()}
                    value={date}
                    onChange={(event) => {
                      setDate(event.target.value);
                      setSelectedSlot("");
                    }}
                    className="mt-1 block rounded-lg border border-sand-200 px-3 py-2"
                  />
                </label>
                {slotsLoading ? (
                  <Skeleton className="h-10 rounded-lg" />
                ) : (
                  <div className="flex flex-wrap gap-2">
                    {slots.map((slot) => (
                      <button
                        key={slot.startsAt}
                        type="button"
                        onClick={() => setSelectedSlot(slot.startsAt)}
                        className={`rounded-lg border px-3 py-2 text-sm ${selectedSlot === slot.startsAt ? "border-brand-600 bg-brand-600 text-white" : "border-sand-200 text-sand-700"}`}
                      >
                        {new Date(slot.startsAt).toLocaleTimeString("es-CR", {
                          hour: "2-digit",
                          minute: "2-digit",
                        })}{" "}
                        ({slot.availableCapacity})
                      </button>
                    ))}
                  </div>
                )}
                {!slotsLoading && slots.length === 0 ? (
                  <p className="text-sm text-sand-500">
                    No hay horarios disponibles en esta fecha.
                  </p>
                ) : null}
                {isAuthenticated ? (
                  <div className="flex flex-wrap items-end gap-3">
                    <label className="block text-sm font-medium text-sand-700">
                      Mascota
                      <select
                        value={petId}
                        onChange={(event) => setPetId(event.target.value)}
                        className="mt-1 block min-w-48 rounded-lg border border-sand-200 px-3 py-2"
                      >
                        <option value="">Selecciona una mascota</option>
                        {pets
                          .filter((pet) => pet.status === "Active")
                          .map((pet) => (
                            <option key={pet.id} value={pet.id}>
                              {pet.name}
                            </option>
                          ))}
                      </select>
                    </label>
                    <Button onClick={reserve} loading={createBooking.isPending}>
                      Solicitar reserva
                    </Button>
                  </div>
                ) : (
                  <Link
                    to="/login"
                    className="inline-block rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white"
                  >
                    Inicia sesion para reservar
                  </Link>
                )}
              </div>
            ) : null}
          </>
        )}
      </section>
    </main>
  );
}
