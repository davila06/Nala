import { useState } from "react";
import { Card, Button } from "@/shared/ui";
import { toast } from "@/shared/lib/toast";
import { useHaptic } from "@/shared/hooks/useHaptic";
import { usePaymentProfiles, useDeletePaymentProfile, useSavePaymentProfile } from "../hooks/usePaymentProfiles";
import { SecureCardPaymentForm, type CardPaymentData } from "./SecureCardPaymentForm";

export function PaymentMethodsCard() {
  const { data: profiles = [], isLoading } = usePaymentProfiles();
  const deleteProfile = useDeletePaymentProfile();
  const saveProfile = useSavePaymentProfile();
  const [showAddForm, setShowAddForm] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const { tap, success, warning } = useHaptic();

  const handleDelete = async (profileId: string) => {
    warning();
    setDeletingId(profileId);
    try {
      await deleteProfile.mutateAsync(profileId);
      toast.success("Método de pago eliminado.");
    } catch {
      toast.error("No se pudo eliminar el método de pago.");
    } finally {
      setDeletingId(null);
    }
  };

  const handleSaveCard = async (data: CardPaymentData) => {
    if (!data.transientToken) return;
    try {
      await saveProfile.mutateAsync({
        transientToken: data.transientToken,
        cardholderName: data.cardholderName,
        setAsDefault: data.saveProfile ?? true,
      });
      toast.success("Tarjeta guardada exitosamente.");
      setShowAddForm(false);
      success();
    } catch {
      toast.error("No se pudo guardar la tarjeta. Intenta de nuevo.");
      warning();
    }
  };

  return (
    <Card>
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-base font-bold text-sand-900">Métodos de pago guardados</h2>
            <p className="text-xs text-sand-500">Tarjetas registradas para cobros recurrentes y compras rápidas</p>
          </div>
          <span className="text-xl" aria-hidden="true">
            💳
          </span>
        </div>

        {isLoading ? (
          <div className="py-4 text-center text-xs text-sand-400">Cargando métodos de pago…</div>
        ) : profiles.length === 0 ? (
          <div className="rounded-2xl border border-dashed border-sand-300 p-4 text-center space-y-1">
            <p className="text-xs font-semibold text-sand-700">No tienes tarjetas guardadas</p>
            <p className="text-[11px] text-sand-400">
              Agrega una tarjeta para renovar tu suscripción sin interrupciones.
            </p>
          </div>
        ) : (
          <div className="space-y-2">
            {profiles.map((p) => {
              const isDeleting = deletingId === p.id;
              return (
                <div
                  key={p.id}
                  className="flex items-center justify-between p-3.5 rounded-2xl border border-sand-200 bg-surface-warm shadow-2xs"
                >
                  <div className="flex items-center gap-3">
                    <span className="text-2xl" aria-hidden="true">
                      {p.cardBrand.toLowerCase().includes("visa")
                        ? "💳"
                        : p.cardBrand.toLowerCase().includes("master")
                          ? "💳"
                          : "💳"}
                    </span>
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="text-xs font-bold text-sand-900">
                          {p.cardBrand} •••• {p.lastFourDigits}
                        </span>
                        {p.isDefault && (
                          <span className="rounded-full bg-sand-200 px-2 py-0.5 text-[9px] font-extrabold uppercase text-sand-700">
                            Predeterminada
                          </span>
                        )}
                      </div>
                      <p className="text-[10px] text-sand-500">
                        {p.cardholderName || "Titular"} · Vence {p.expirationMonth?.toString().padStart(2, "0")}/
                        {p.expirationYear}
                      </p>
                    </div>
                  </div>

                  <button
                    type="button"
                    disabled={isDeleting || deleteProfile.isPending}
                    onClick={() => void handleDelete(p.id)}
                    aria-label={`Eliminar tarjeta terminada en ${p.lastFourDigits}`}
                    className="rounded-xl p-2 text-sand-400 hover:bg-danger-50 hover:text-danger-600 transition-colors disabled:opacity-50"
                  >
                    {isDeleting ? (
                      <span className="h-4 w-4 block rounded-full border-2 border-danger-400 border-t-transparent animate-spin" />
                    ) : (
                      <svg viewBox="0 0 16 16" fill="currentColor" className="h-4 w-4" aria-hidden="true">
                        <path
                          fillRule="evenodd"
                          d="M5 3.25V4H2.75a.75.75 0 0 0 0 1.5h.3l.815 8.15A1.5 1.5 0 0 0 5.357 15h5.285a1.5 1.5 0 0 0 1.493-1.35l.815-8.15h.3a.75.75 0 0 0 0-1.5H11v-.75A2.25 2.25 0 0 0 8.75 1h-1.5A2.25 2.25 0 0 0 5 3.25Zm2.25-.75a.75.75 0 0 0-.75.75V4h3v-.75a.75.75 0 0 0-.75-.75h-1.5ZM6.05 6a.75.75 0 0 1 .787.713l.275 5.5a.75.75 0 0 1-1.498.074l-.275-5.5A.75.75 0 0 1 6.05 6Zm3.9 0a.75.75 0 0 1 .712.787l-.275 5.5a.75.75 0 0 1-1.498-.074l.275-5.5a.75.75 0 0 1 .786-.713Z"
                          clipRule="evenodd"
                        />
                      </svg>
                    )}
                  </button>
                </div>
              );
            })}
          </div>
        )}

        {/* Add Card Section */}
        {showAddForm ? (
          <div className="rounded-2xl border border-sand-200 bg-surface p-4 space-y-3">
            <div className="flex items-center justify-between pb-1">
              <span className="text-xs font-bold text-sand-900">Registrar nueva tarjeta</span>
              <button
                type="button"
                onClick={() => setShowAddForm(false)}
                className="text-xs font-semibold text-sand-500 hover:text-sand-700"
              >
                Cancelar
              </button>
            </div>
            <SecureCardPaymentForm
              amountCrc={0}
              isProcessing={saveProfile.isPending}
              onPay={handleSaveCard}
              buttonLabel="Guardar tarjeta de forma segura"
            />
          </div>
        ) : (
          <Button
            variant="secondary"
            onClick={() => {
              tap();
              setShowAddForm(true);
            }}
            className="w-full text-xs font-bold"
          >
            + Agregar método de pago
          </Button>
        )}
      </div>
    </Card>
  );
}
