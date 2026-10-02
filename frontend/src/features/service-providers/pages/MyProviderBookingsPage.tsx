import { useState } from "react";
import { Helmet } from "react-helmet-async";
import { Button } from "@/shared/ui";
import { Skeleton } from "@/shared/ui/Spinner";
import { toast } from "@/shared/lib/toast";
import { useChargeCard } from "@/features/payments/hooks/usePaymentProfiles";
import { SecureCardPaymentForm, type CardPaymentData } from "@/features/payments/components/SecureCardPaymentForm";
import {
  useMyProviderBookings,
  useRescheduleProviderBooking,
  useUpdateProviderBookingStatus,
  useCreateProviderBookingPayment,
  useReportProviderBookingPayment,
} from "../hooks/useServiceProviders";
import type { ProviderPaymentDto } from "../api/serviceProvidersApi";

const statusLabel: Record<string, string> = {
  Requested: "Solicitada",
  AwaitingPayment: "Pago pendiente",
  Confirmed: "Confirmada",
  InProgress: "En curso",
  Completed: "Completada",
  CancelledByCustomer: "Cancelada",
  CancelledByProvider: "Cancelada por proveedor",
  NoShow: "No asistio",
  Expired: "Vencida",
  Disputed: "En disputa",
  Refunded: "Reembolsada",
};

export default function MyProviderBookingsPage() {
  const { data: bookings = [], isLoading } = useMyProviderBookings();
  const update = useUpdateProviderBookingStatus();
  const reschedule = useRescheduleProviderBooking();
  const createPayment = useCreateProviderBookingPayment();
  const reportPayment = useReportProviderBookingPayment();
  const chargeCard = useChargeCard();
  const [rescheduleAt, setRescheduleAt] = useState<Record<string, string>>({});
  const [payments, setPayments] = useState<Record<string, ProviderPaymentDto>>({});
  const [cardBookingId, setCardBookingId] = useState<string | null>(null);
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
        <h1 className="font-display text-2xl font-semibold text-ink-900">Mis reservas</h1>
        <p className="text-sm text-copy-secondary">Consulta y administra tus solicitudes de servicios.</p>
      </header>
      {bookings.length === 0 ? (
        <p className="py-12 text-center text-sm text-copy-secondary">Aun no tienes reservas.</p>
      ) : (
        <ul className="space-y-3">
          {bookings.map((booking) => (
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
                  <p className="mt-1 text-sm font-semibold text-rescue-700">
                    CRC {booking.totalCrc.toLocaleString("es-CR")}
                  </p>
                </div>
                <span className="rounded-full bg-sand-100 px-2 py-1 text-xs font-semibold text-sand-700">
                  {statusLabel[booking.status] ?? booking.status}
                </span>
              </div>
              {(booking.status === "Requested" || booking.status === "AwaitingPayment") && (
                <div className="mt-4 space-y-3 border-t border-sand-100 pt-3">
                  {!payments[booking.id] ? (
                    <Button
                      size="sm"
                      loading={createPayment.isPending}
                      onClick={async () => {
                        try {
                          const payment = await createPayment.mutateAsync(booking.id);
                          setPayments((current) => ({ ...current, [booking.id]: payment }));
                        } catch {
                          toast.error("No se pudo preparar el pago de esta reserva.");
                        }
                      }}
                    >
                      {booking.status === "Requested" ? "Iniciar pago" : "Consultar pago"}
                    </Button>
                  ) : (
                    <div className="rounded-xl border border-warn-200 bg-warn-50 p-3 text-xs text-warn-900 space-y-2">
                      <p>
                        Referencia: <strong className="font-mono">{payments[booking.id].paymentReference}</strong>
                      </p>
                      <p>
                        Monto: <strong>₡{payments[booking.id].amountCrc.toLocaleString("es-CR")}</strong> · Estado:{" "}
                        {payments[booking.id].status}
                      </p>
                      {payments[booking.id].status === "Pending" && (
                        <div className="flex flex-wrap gap-2">
                          <Button
                            size="sm"
                            variant="secondary"
                            onClick={() => setCardBookingId(cardBookingId === booking.id ? null : booking.id)}
                          >
                            Pagar con tarjeta
                          </Button>
                          <Button
                            size="sm"
                            loading={reportPayment.isPending}
                            onClick={async () => {
                              try {
                                const reported = await reportPayment.mutateAsync(payments[booking.id].id);
                                setPayments((current) => ({ ...current, [booking.id]: reported }));
                                toast.success("Pago reportado; el administrador debe verificarlo.");
                              } catch {
                                toast.error("No se pudo reportar el pago SINPE.");
                              }
                            }}
                          >
                            Ya pagué por SINPE
                          </Button>
                        </div>
                      )}
                      {payments[booking.id].status === "Reported" && (
                        <p>Pago reportado; espera la verificación manual.</p>
                      )}
                      {payments[booking.id].status === "CardPending" && (
                        <p>
                          Pago en verificación. No vuelvas a pagar mientras confirmamos la respuesta de la pasarela.
                        </p>
                      )}
                      {payments[booking.id].status === "Confirmed" && (
                        <p>Pago confirmado; la reserva se actualizará cuando finalice la sincronización.</p>
                      )}
                    </div>
                  )}
                  {cardBookingId === booking.id && payments[booking.id]?.status === "Pending" && (
                    <SecureCardPaymentForm
                      amountCrc={payments[booking.id].amountCrc}
                      isProcessing={chargeCard.isPending}
                      onPay={async (card: CardPaymentData) => {
                        try {
                          const result = await chargeCard.mutateAsync({
                            amountCrc: booking.totalCrc,
                            purpose: "ProviderBooking",
                            targetEntityId: booking.id,
                            paymentProfileId: card.paymentProfileId,
                            transientToken: card.transientToken,
                            cardholderName: card.cardholderName,
                            saveProfile: card.saveProfile,
                          });
                          if (!result.success) {
                            toast.error(result.errorMessage ?? "La tarjeta no fue autorizada.");
                            return;
                          }
                          const refreshed = await createPayment.mutateAsync(booking.id);
                          setPayments((current) => ({ ...current, [booking.id]: refreshed }));
                          setCardBookingId(null);
                          toast.success("Autorización recibida; esperamos la confirmación final de la pasarela.");
                        } catch {
                          toast.error("No se pudo procesar el pago. Revisa el estado antes de reintentar.");
                        }
                      }}
                      buttonLabel={`Pagar ₡${payments[booking.id].amountCrc.toLocaleString("es-CR")}`}
                    />
                  )}
                </div>
              )}
              {booking.status === "Requested" || booking.status === "Confirmed" ? (
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
                      if (!startsAt) return toast.error("Selecciona una nueva fecha y hora.");
                      reschedule.mutate(
                        {
                          bookingId: booking.id,
                          startsAt: new Date(startsAt).toISOString(),
                        },
                        {
                          onSuccess: () => toast.success("Reserva reprogramada"),
                          onError: () => toast.error("El nuevo horario no esta disponible."),
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
                          onError: () => toast.error("No se pudo cancelar la reserva."),
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
