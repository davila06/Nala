import { useState } from "react";
import { Helmet } from "react-helmet-async";
import { Button } from "@/shared/ui";
import { Skeleton } from "@/shared/ui/Spinner";
import { toast } from "@/shared/lib/toast";
import {
  useMyProviderBookings,
  useRescheduleProviderBooking,
  useUpdateProviderBookingStatus,
} from "../hooks/useServiceProviders";

const statusLabel: Record<string, string> = {
  Requested: "Solicitada",
  Confirmed: "Confirmada",
  InProgress: "En curso",
  Completed: "Completada",
  CancelledByCustomer: "Cancelada",
  CancelledByProvider: "Cancelada por proveedor",
  NoShow: "No asistio",
};

export default function MyProviderBookingsPage() {
  const { data: bookings = [], isLoading } = useMyProviderBookings();
  const update = useUpdateProviderBookingStatus();
  const reschedule = useRescheduleProviderBooking();
  const [rescheduleAt, setRescheduleAt] = useState<Record<string, string>>({});
  if (isLoading)
    return (
      <div className="mx-auto max-w-3xl p-8">
        <Skeleton className="h-48 rounded-xl" />
      </div>
    );
  return (
    <main className="mx-auto max-w-3xl space-y-5 px-4 py-8">
      <Helmet>
        <title>Mis reservas · PawTrack CR</title>
      </Helmet>
      <header>
        <h1 className="font-display text-2xl font-semibold text-ink-900">
          Mis reservas
        </h1>
        <p className="text-sm text-sand-600">
          Consulta y administra tus solicitudes de servicios.
        </p>
      </header>
      {bookings.length === 0 ? (
        <p className="py-12 text-center text-sm text-sand-500">
          Aun no tienes reservas.
        </p>
      ) : (
        <ul className="space-y-3">
          {bookings.map((booking) => (
            <li
              key={booking.id}
              className="rounded-xl border border-sand-100 bg-surface p-4"
            >
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <p className="font-semibold text-ink-900">
                    {booking.serviceName}
                  </p>
                  <p className="text-sm text-sand-600">
                    {new Date(booking.startsAt).toLocaleString("es-CR", {
                      dateStyle: "medium",
                      timeStyle: "short",
                    })}
                  </p>
                  <p className="mt-1 text-sm font-semibold text-rescue-700">
                    CRC {booking.priceCrc.toLocaleString("es-CR")}
                  </p>
                </div>
                <span className="rounded-full bg-sand-100 px-2 py-1 text-xs font-semibold text-sand-700">
                  {statusLabel[booking.status] ?? booking.status}
                </span>
              </div>
              {booking.status === "Requested" ||
              booking.status === "Confirmed" ? (
                <div className="mt-4 flex flex-wrap items-end gap-2">
                  <label className="text-sm font-medium text-sand-700">
                    Nueva fecha y hora
                    <input
                      type="datetime-local"
                      value={rescheduleAt[booking.id] ?? ""}
                      onChange={(event) =>
                        setRescheduleAt((current) => ({
                          ...current,
                          [booking.id]: event.target.value,
                        }))
                      }
                      className="mt-1 block rounded-lg border border-sand-200 px-3 py-2 text-sm"
                    />
                  </label>
                  <Button
                    size="sm"
                    loading={reschedule.isPending}
                    onClick={() => {
                      const startsAt = rescheduleAt[booking.id];
                      if (!startsAt)
                        return toast.error(
                          "Selecciona una nueva fecha y hora.",
                        );
                      reschedule.mutate(
                        {
                          bookingId: booking.id,
                          startsAt: new Date(startsAt).toISOString(),
                        },
                        {
                          onSuccess: () =>
                            toast.success("Reserva reprogramada"),
                          onError: () =>
                            toast.error("El nuevo horario no esta disponible."),
                        },
                      );
                    }}
                  >
                    Reprogramar
                  </Button>
                  <Button
                    size="sm"
                    variant="secondary"
                    loading={update.isPending}
                    onClick={() =>
                      update.mutate(
                        {
                          bookingId: booking.id,
                          status: "CancelledByCustomer",
                          reason: "Cancelada por el cliente",
                        },
                        {
                          onSuccess: () => toast.success("Reserva cancelada"),
                          onError: () =>
                            toast.error("No se pudo cancelar la reserva."),
                        },
                      )
                    }
                  >
                    Cancelar reserva
                  </Button>
                </div>
              ) : null}
            </li>
          ))}
        </ul>
      )}
    </main>
  );
}
