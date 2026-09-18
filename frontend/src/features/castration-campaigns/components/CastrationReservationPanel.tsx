import { useState } from "react";
import { CalendarCheck, X } from "lucide-react";
import { useAuthStore } from "@/features/auth/store/authStore";
import { usePets } from "@/features/pets/hooks/usePets";
import { Button } from "@/shared/ui/Button";
import { toast } from "@/shared/lib/toast";
import type { CastrationCampaign } from "../api/castrationCampaignsApi";
import { useReserveCastrationAppointment } from "../hooks/useCastrationCampaigns";

export function CastrationReservationPanel({ campaign }: { campaign: CastrationCampaign }) {
  const user = useAuthStore((state) => state.user);
  const { data: pets = [] } = usePets(Boolean(user));
  const reserve = useReserveCastrationAppointment(campaign.id);
  const [open, setOpen] = useState(false);
  const [petId, setPetId] = useState("");
  const [weightKg, setWeightKg] = useState("");
  const [requiresInvoice, setRequiresInvoice] = useState(false);
  const total = requiresInvoice ? Math.round(campaign.basePriceCrc * 1.13) : campaign.basePriceCrc;

  if (!open)
    return (
      <Button className="mt-4 w-full" onClick={() => setOpen(true)}>
        <CalendarCheck className="h-4 w-4" /> Reservar cupo
      </Button>
    );

  return (
    <form
      className="mt-4 space-y-3 border-t border-sand-200 pt-4"
      onSubmit={(event) => {
        event.preventDefault();
        if (!user) {
          toast.error("Inicia sesión como dueño para reservar.");
          return;
        }
        reserve.mutate(
          {
            petId,
            scheduledAt: campaign.startsAt,
            weightKg: Number(weightKg),
            confirmsFastingInstructions: true,
            isPregnant: false,
            isInHeat: false,
            consentAccepted: true,
            consentVersion: campaign.consentVersion,
            requiresInvoice,
          },
          {
            onSuccess: () => {
              toast.success("Cupo reservado");
              setOpen(false);
            },
            onError: () => toast.error("No se pudo reservar el cupo"),
          },
        );
      }}
    >
      <div className="flex items-center justify-between">
        <p className="text-sm font-bold text-sand-900">Datos de reserva</p>
        <button type="button" onClick={() => setOpen(false)} aria-label="Cerrar reserva">
          <X className="h-4 w-4" />
        </button>
      </div>
      <select
        required
        value={petId}
        onChange={(e) => setPetId(e.target.value)}
        className="w-full rounded-lg border border-sand-300 bg-surface p-2 text-sm"
      >
        <option value="">Selecciona tu mascota</option>
        {pets.map((pet) => (
          <option key={pet.id} value={pet.id}>
            {pet.name}
          </option>
        ))}
      </select>
      <input
        required
        min="0.5"
        max="150"
        step="0.1"
        type="number"
        value={weightKg}
        onChange={(e) => setWeightKg(e.target.value)}
        placeholder="Peso actual (kg)"
        className="w-full rounded-lg border border-sand-300 p-2 text-sm"
      />
      <label className="flex gap-2 text-xs text-sand-700">
        <input required type="checkbox" /> Confirmo ayuno e instrucciones preoperatorias.
      </label>
      <label className="flex gap-2 text-xs text-sand-700">
        <input required type="checkbox" /> Acepto el consentimiento informado {campaign.consentVersion}.
      </label>
      <label className="flex gap-2 text-xs text-sand-700">
        <input type="checkbox" checked={requiresInvoice} onChange={(e) => setRequiresInvoice(e.target.checked)} />{" "}
        Requiero Factura Electrónica (+13% IVA).
      </label>
      <div className="flex justify-between border-t border-sand-200 pt-2 text-sm font-bold">
        <span>Total</span>
        <span>₡{total.toLocaleString("es-CR")}</span>
      </div>
      <Button type="submit" loading={reserve.isPending} className="w-full">
        Confirmar reserva
      </Button>
    </form>
  );
}
