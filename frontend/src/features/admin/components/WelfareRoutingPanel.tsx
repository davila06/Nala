import { useMemo, useState } from "react";
import { Input } from "@/shared/ui";
import { useConfirmWelfareRouting, useWelfareRoutingCandidates } from "../hooks/useAdmin";
import type { AnimalWelfareCaseSummaryDto, WelfareRoutingCandidateDto } from "../api/adminApi";

function candidateKey(candidate: WelfareRoutingCandidateDto) {
  return `${candidate.recipientType}:${candidate.userId}`;
}

interface WelfareRoutingPanelProps {
  welfareCase: AnimalWelfareCaseSummaryDto;
  disabled: boolean;
}

export function WelfareRoutingPanel({ welfareCase, disabled }: WelfareRoutingPanelProps) {
  const [open, setOpen] = useState(false);
  const [selectedKey, setSelectedKey] = useState("");
  const [reason, setReason] = useState("Triage revisado por Admin/Superadmin");
  const query = useWelfareRoutingCandidates(welfareCase.id, open && !disabled);
  const confirm = useConfirmWelfareRouting();
  const candidates = query.data ?? [];

  const suggested = useMemo(
    () =>
      candidates.find(
        (candidate) =>
          candidate.userId === welfareCase.suggestedOrganizationUserId &&
          candidate.recipientType === welfareCase.suggestedRole,
      ),
    [candidates, welfareCase.suggestedOrganizationUserId, welfareCase.suggestedRole],
  );
  const selected =
    candidates.find((candidate) => candidateKey(candidate) === selectedKey) ?? suggested ?? candidates[0];
  const busy = disabled || query.isFetching || confirm.isPending;

  return (
    <div className="mt-3 rounded-xl border border-trust-200 bg-trust-50/60 p-3">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <p className="text-xs font-bold text-sand-900">Ruteo de la denuncia</p>
          <p className="mt-0.5 text-xs text-copy-secondary">
            {welfareCase.autoRoutingRequested
              ? "La persona solicitó una sugerencia automática; todavía no se comparte con el destinatario."
              : "La asignación manual solo ofrece organizaciones activas elegibles para este caso."}
          </p>
          {suggested && (
            <p className="mt-1 text-xs font-semibold text-trust-800">
              Sugerido: {suggested.organizationName}
              {suggested.distanceMetres != null ? ` · ${suggested.distanceMetres} m` : ` · ${suggested.coverageLabel}`}
            </p>
          )}
        </div>
        {!open && (
          <button
            type="button"
            disabled={disabled}
            onClick={() => setOpen(true)}
            className="rounded-lg border border-trust-300 bg-white px-3 py-2 text-xs font-bold text-trust-800 hover:bg-trust-50 disabled:opacity-50"
          >
            Elegir destinatario
          </button>
        )}
      </div>

      {open && (
        <div className="mt-3 space-y-2">
          {query.isLoading && (
            <p role="status" className="text-xs text-copy-secondary">
              Buscando organizaciones elegibles…
            </p>
          )}
          {query.isError && (
            <p role="alert" className="text-xs text-danger-700">
              No se pudieron cargar los destinatarios.
            </p>
          )}
          {!query.isLoading && !query.isError && candidates.length === 0 && (
            <p role="status" className="text-xs text-copy-secondary">
              No hay aliados verificados en cobertura ni municipalidad activa para este cantón.
            </p>
          )}
          {candidates.length > 0 && (
            <>
              <label className="block text-xs font-semibold text-sand-800">
                Destinatario
                <select
                  value={selected ? candidateKey(selected) : ""}
                  onChange={(event) => setSelectedKey(event.target.value)}
                  disabled={busy}
                  className="mt-1 w-full rounded-lg border border-sand-300 bg-white px-3 py-2 text-sm"
                >
                  {candidates.map((candidate) => (
                    <option key={candidateKey(candidate)} value={candidateKey(candidate)}>
                      {candidate.organizationName} ·{" "}
                      {candidate.recipientType === "Ally" ? "Aliado verificado" : "Municipalidad"}
                      {candidate.distanceMetres != null
                        ? ` · ${candidate.distanceMetres} m`
                        : ` · ${candidate.coverageLabel}`}
                    </option>
                  ))}
                </select>
              </label>
              <Input
                value={reason}
                onChange={(event) => setReason(event.target.value)}
                aria-label="Motivo de asignación"
              />
              {confirm.isError && (
                <p role="alert" className="text-xs text-danger-700">
                  No se pudo confirmar; el destinatario pudo perder elegibilidad. Actualiza la lista e intenta de nuevo.
                </p>
              )}
              <div className="flex flex-wrap gap-2">
                <button
                  type="button"
                  disabled={busy || !selected || !reason.trim()}
                  onClick={() =>
                    selected &&
                    void confirm.mutateAsync({
                      caseId: welfareCase.id,
                      recipientUserId: selected.userId,
                      recipientType: selected.recipientType,
                      reason,
                    })
                  }
                  className="rounded-lg bg-trust-700 px-3 py-2 text-xs font-bold text-white hover:bg-trust-800 disabled:opacity-50"
                >
                  {confirm.isPending ? "Confirmando…" : "Confirmar y asignar"}
                </button>
                <button
                  type="button"
                  disabled={confirm.isPending}
                  onClick={() => setOpen(false)}
                  className="rounded-lg border border-sand-300 bg-white px-3 py-2 text-xs font-semibold text-sand-700"
                >
                  Cancelar
                </button>
              </div>
            </>
          )}
        </div>
      )}
    </div>
  );
}
