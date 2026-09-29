import { useState } from "react";
import { Helmet } from "react-helmet-async";
import { Button } from "@/shared/ui";
import { Modal } from "@/shared/ui/Modal";
import { Skeleton } from "@/shared/ui/Spinner";
import { toast } from "@/shared/lib/toast";
import type { ProviderBookingStatus } from "../api/serviceProvidersApi";
import { useIncomingProviderBookings, useUpdateProviderBookingStatus } from "../hooks/useServiceProviders";

const actions: Partial<Record<ProviderBookingStatus, { status: ProviderBookingStatus; label: string }>> = {
  Requested: { status: "Confirmed", label: "Confirmar" },
  Confirmed: { status: "InProgress", label: "Iniciar" },
  InProgress: { status: "Completed", label: "Completar" },
};

export default function IncomingProviderBookingsPage() {
  const { data: bookings = [], isLoading } = useIncomingProviderBookings();
  const update = useUpdateProviderBookingStatus();
  const [pendingAction, setPendingAction] = useState<{
    bookingId: string;
    serviceName: string;
    status: ProviderBookingStatus;
    reason?: string;
  } | null>(null);
  if (isLoading)
    return (
      <div className="mx-auto max-w-3xl p-8">
        <Skeleton className="h-48 rounded-xl" />
      </div>
    );
  return (
    <main className="mx-auto max-w-3xl space-y-5 px-4 py-8">
      <Helmet>
        <title>Reservas entrantes · PawTrack CR</title>
      </Helmet>
      <header>
        <h1 className="font-display text-2xl font-semibold text-ink-900">Reservas entrantes</h1>
        <p className="text-sm text-copy-secondary">Confirma y registra el progreso de cada servicio.</p>
      </header>
      {bookings.length === 0 ? (
        <p className="py-12 text-center text-sm text-copy-secondary">Aun no hay reservas.</p>
      ) : (
        <ul className="space-y-3">
          {bookings.map((booking) => {
            const action = actions[booking.status];
            return (
              <li key={booking.id} className="rounded-xl border border-sand-100 bg-surface p-4">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <p className="font-semibold text-ink-900">{booking.serviceName}</p>
                    <p className="text-sm text-copy-secondary">
                      {new Date(booking.startsAt).toLocaleString("es-CR", {
                        dateStyle: "medium",
                        timeStyle: "short",
                      })}
                    </p>
                    <p className="text-xs text-copy-secondary">Capacidad solicitada: {booking.quantity}</p>
                  </div>
                  <span className="rounded-full bg-sand-100 px-2 py-1 text-xs font-semibold text-sand-700">
                    {booking.status}
                  </span>
                </div>
                {action ? (
                  <div className="mt-4 flex gap-2">
                    <Button
                      size="sm"
                      loading={update.isPending}
                      onClick={() =>
                        update.mutate(
                          { bookingId: booking.id, status: action.status },
                          {
                            onSuccess: () => toast.success("Reserva actualizada"),
                            onError: () => toast.error("No se pudo actualizar la reserva."),
                          },
                        )
                      }
                    >
                      {action.label}
                    </Button>
                    <Button
                      size="sm"
                      variant="secondary"
                      loading={update.isPending}
                      onClick={() =>
                        setPendingAction({
                          bookingId: booking.id,
                          serviceName: booking.serviceName,
                          status: "CancelledByProvider",
                          reason: "No disponible",
                        })
                      }
                    >
                      Rechazar
                    </Button>
                    {booking.status === "Confirmed" ? (
                      <Button
                        size="sm"
                        variant="secondary"
                        loading={update.isPending}
                        onClick={() =>
                          setPendingAction({
                            bookingId: booking.id,
                            serviceName: booking.serviceName,
                            status: "NoShow",
                          })
                        }
                      >
                        Marcar inasistencia
                      </Button>
                    ) : null}
                  </div>
                ) : null}
              </li>
            );
          })}
        </ul>
      )}
      <Modal
        isOpen={pendingAction !== null}
        onClose={() => setPendingAction(null)}
        title={pendingAction?.status === "NoShow" ? "Confirmar inasistencia" : "Confirmar rechazo"}
      >
        <p className="text-sm text-copy-secondary">
          {pendingAction?.status === "NoShow" ? "Registrar inasistencia" : "Rechazar la reserva"} de{" "}
          {pendingAction?.serviceName} cambiará su estado. Verifica antes de continuar.
        </p>
        <div className="mt-5 flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setPendingAction(null)}>
            Volver
          </Button>
          <Button
            loading={update.isPending}
            onClick={() => {
              if (!pendingAction) return;
              const payload = {
                bookingId: pendingAction.bookingId,
                status: pendingAction.status,
                reason: pendingAction.reason,
              };
              update.mutate(payload, {
                onSuccess: () => {
                  toast.success("Reserva actualizada");
                  setPendingAction(null);
                },
                onError: () => toast.error("No se pudo actualizar la reserva."),
              });
            }}
          >
            {pendingAction?.status === "NoShow" ? "Registrar inasistencia" : "Confirmar rechazo"}
          </Button>
        </div>
      </Modal>
    </main>
  );
}
