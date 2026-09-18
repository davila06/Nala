import { useState } from "react";
import { useParams } from "react-router-dom";
import { ClipboardCheck, Stethoscope } from "lucide-react";
import { Button } from "@/shared/ui/Button";
import { toast } from "@/shared/lib/toast";
import { useCastrationAgenda, useOperateCastrationAppointment } from "../hooks/useCastrationCampaigns";

export default function CastrationOperationsPage() {
  const { campaignId = "" } = useParams();
  const { data, isLoading } = useCastrationAgenda(campaignId);
  const operate = useOperateCastrationAppointment(campaignId);
  const [closingId, setClosingId] = useState<string | null>(null);
  const [veterinarianId, setVeterinarianId] = useState("");
  const [outcome, setOutcome] = useState("");
  const [instructions, setInstructions] = useState("");

  const transition = (appointmentId: string, operation: string) => {
    operate.mutate(
      { appointmentId, operation },
      {
        onSuccess: () => toast.success("Estado actualizado"),
        onError: () => toast.error("No se pudo actualizar la cita"),
      },
    );
  };

  const complete = (appointmentId: string) => {
    operate.mutate(
      {
        appointmentId,
        operation: "Complete",
        details: {
          veterinarianId,
          clinicalOutcome: outcome,
          postOperativeInstructions: instructions,
        },
      },
      {
        onSuccess: () => {
          toast.success("Procedimiento completado");
          setClosingId(null);
          setVeterinarianId("");
          setOutcome("");
          setInstructions("");
        },
        onError: () => toast.error("No se pudo cerrar el procedimiento"),
      },
    );
  };

  return (
    <main className="mx-auto max-w-5xl px-4 py-8 space-y-6">
      <header className="flex items-center gap-3 border-b border-sand-200 pb-5">
        <Stethoscope className="h-7 w-7 text-rescue-700" aria-hidden="true" />
        <div>
          <h1 className="font-display text-2xl font-bold text-sand-900">Agenda clínica</h1>
          <p className="text-sm text-sand-500">Check-in y seguimiento de procedimientos</p>
        </div>
      </header>
      {isLoading ? (
        <p className="text-sm text-sand-500">Cargando agenda…</p>
      ) : data?.items.length === 0 ? (
        <p className="py-10 text-center text-sand-500">No hay citas registradas.</p>
      ) : (
        <div className="space-y-3">
          {data?.items.map((item) => (
            <article key={item.id} className="rounded-lg border border-sand-200 bg-surface p-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <p className="font-semibold text-sand-900">Mascota {item.petId.slice(0, 8)}</p>
                  <p className="text-xs text-sand-500">{new Date(item.scheduledAt).toLocaleString("es-CR")}</p>
                </div>
                <span className="rounded-full bg-sand-100 px-2.5 py-1 text-xs font-bold text-sand-700">
                  {item.status}
                </span>
                <div className="flex gap-2">
                  {item.status === "Reserved" ? (
                    <Button size="sm" onClick={() => transition(item.id, "Confirm")}>
                      Confirmar
                    </Button>
                  ) : null}
                  {item.status === "Confirmed" ? (
                    <>
                      <Button size="sm" onClick={() => transition(item.id, "CheckIn")}>
                        <ClipboardCheck className="h-4 w-4" /> Check-in
                      </Button>
                      <Button size="sm" variant="secondary" onClick={() => transition(item.id, "NoShow")}>
                        No llegó
                      </Button>
                    </>
                  ) : null}
                  {item.status === "CheckedIn" ? (
                    <Button size="sm" onClick={() => setClosingId(item.id)}>
                      Cerrar procedimiento
                    </Button>
                  ) : null}
                </div>
              </div>
              {closingId === item.id ? (
                <form
                  className="mt-4 grid gap-2 border-t border-sand-200 pt-4"
                  onSubmit={(event) => {
                    event.preventDefault();
                    complete(item.id);
                  }}
                >
                  <input
                    required
                    value={veterinarianId}
                    onChange={(e) => setVeterinarianId(e.target.value)}
                    placeholder="ID del veterinario ejecutor"
                    className="rounded-lg border border-sand-300 p-2 text-sm"
                  />
                  <textarea
                    required
                    maxLength={1000}
                    value={outcome}
                    onChange={(e) => setOutcome(e.target.value)}
                    placeholder="Resultado clínico"
                    className="rounded-lg border border-sand-300 p-2 text-sm"
                  />
                  <textarea
                    required
                    maxLength={2000}
                    value={instructions}
                    onChange={(e) => setInstructions(e.target.value)}
                    placeholder="Instrucciones postoperatorias"
                    className="rounded-lg border border-sand-300 p-2 text-sm"
                  />
                  <div className="flex gap-2">
                    <Button type="submit" size="sm" loading={operate.isPending}>
                      Completar
                    </Button>
                    <Button type="button" size="sm" variant="secondary" onClick={() => setClosingId(null)}>
                      Cancelar
                    </Button>
                  </div>
                </form>
              ) : null}
            </article>
          ))}
        </div>
      )}
    </main>
  );
}
