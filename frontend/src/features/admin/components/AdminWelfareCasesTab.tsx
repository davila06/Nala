import { useState } from "react";
import {
  useAdminWelfareCases,
  useAssignWelfareCase,
  useDismissWelfareCase,
  useResolveWelfareCase,
  useSetWelfareCaseSeverity,
  useStartWelfareCaseTriage,
} from "../hooks/useAdmin";
import type {
  AnimalWelfareCaseSummaryDto,
  WelfareSeverity,
} from "../api/adminApi";
import { Input } from "@/shared/ui";

const SEVERITY_LABELS: Record<WelfareSeverity, string> = {
  Low: "Baja",
  Medium: "Media",
  High: "Alta",
  Critical: "Crítica",
};

const SEVERITY_CLASS: Record<WelfareSeverity, string> = {
  Low: "bg-rescue-50 text-rescue-700 border-rescue-100",
  Medium: "bg-trust-50 text-trust-700 border-trust-100",
  High: "bg-warn-50 text-warn-800 border-warn-200",
  Critical: "bg-danger-50 text-danger-700 border-danger-200",
};

function WelfareCaseCard({
  welfareCase,
}: {
  welfareCase: AnimalWelfareCaseSummaryDto;
}) {
  const triage = useStartWelfareCaseTriage();
  const severity = useSetWelfareCaseSeverity();
  const assign = useAssignWelfareCase();
  const resolve = useResolveWelfareCase();
  const dismiss = useDismissWelfareCase();
  const [organizationUserId, setOrganizationUserId] = useState(
    welfareCase.assignedOrganizationUserId ?? "",
  );
  const [role, setRole] = useState(welfareCase.assignedRole ?? "Municipality");
  const [reason, setReason] = useState("Caso revisado por NALA Ops");

  const isClosed = ["Resolved", "Dismissed", "ClosedNoAction"].includes(
    welfareCase.status,
  );
  const busy =
    triage.isPending ||
    severity.isPending ||
    assign.isPending ||
    resolve.isPending ||
    dismiss.isPending;

  return (
    <li className="rounded-2xl border border-sand-200 bg-surface p-4 shadow-sm">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <p className="font-black text-sand-900">
              #{welfareCase.publicCode}
            </p>
            <span
              className={`rounded-full border px-2 py-0.5 text-[10px] font-bold ${SEVERITY_CLASS[welfareCase.severity]}`}
            >
              {SEVERITY_LABELS[welfareCase.severity]}
            </span>
            <span className="rounded-full bg-sand-100 px-2 py-0.5 text-[10px] font-semibold text-sand-600">
              {welfareCase.status}
            </span>
          </div>
          <p className="mt-1 text-xs text-sand-500">
            {welfareCase.type} · {welfareCase.canton} ·{" "}
            {new Date(welfareCase.createdAt).toLocaleDateString("es-CR")}
          </p>
        </div>
        {!isClosed && (
          <button
            type="button"
            disabled={busy}
            onClick={() => void triage.mutateAsync(welfareCase.id)}
            className="rounded-xl bg-trust-100 px-3 py-1.5 text-xs font-semibold text-trust-700 disabled:opacity-50"
          >
            Triage
          </button>
        )}
      </div>

      <div className="mt-4 grid gap-2 md:grid-cols-[1fr_1fr_auto]">
        <select
          value={welfareCase.severity}
          disabled={busy || isClosed}
          onChange={(event) =>
            void severity.mutateAsync({
              caseId: welfareCase.id,
              severity: event.target.value as WelfareSeverity,
            })
          }
          className="rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm text-sand-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-trust-300"
        >
          {Object.entries(SEVERITY_LABELS).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
        <Input
          value={organizationUserId}
          onChange={(event) => setOrganizationUserId(event.target.value)}
          placeholder="Usuario/organización asignada"
          disabled={busy || isClosed}
        />
        <Input
          value={role}
          onChange={(event) => setRole(event.target.value)}
          placeholder="Rol"
          disabled={busy || isClosed}
        />
      </div>

      <div className="mt-2 flex flex-wrap gap-2">
        <button
          type="button"
          disabled={
            busy || isClosed || !organizationUserId.trim() || !role.trim()
          }
          onClick={() =>
            void assign.mutateAsync({
              caseId: welfareCase.id,
              organizationUserId,
              role,
            })
          }
          className="rounded-xl bg-rescue-100 px-3 py-1.5 text-xs font-semibold text-rescue-700 disabled:opacity-50"
        >
          Asignar
        </button>
        <button
          type="button"
          disabled={busy || isClosed || !reason.trim()}
          onClick={() =>
            void resolve.mutateAsync({ caseId: welfareCase.id, reason })
          }
          className="rounded-xl bg-sand-900 px-3 py-1.5 text-xs font-semibold text-white disabled:opacity-50"
        >
          Resolver
        </button>
        <button
          type="button"
          disabled={busy || isClosed || !reason.trim()}
          onClick={() =>
            void dismiss.mutateAsync({ caseId: welfareCase.id, reason })
          }
          className="rounded-xl bg-danger-100 px-3 py-1.5 text-xs font-semibold text-danger-700 disabled:opacity-50"
        >
          Descartar
        </button>
      </div>
      <Input
        value={reason}
        onChange={(event) => setReason(event.target.value)}
        placeholder="Motivo de cierre o revisión"
        className="mt-3"
      />
    </li>
  );
}

export function AdminWelfareCasesTab() {
  const { data, isLoading, isError } = useAdminWelfareCases();

  if (isLoading) {
    return <div className="h-32 animate-pulse rounded-2xl bg-sand-100" />;
  }

  if (isError) {
    return (
      <div className="rounded-2xl border border-danger-200 bg-danger-50 px-4 py-3 text-sm text-danger-700">
        No se pudo cargar la cola de bienestar animal.
      </div>
    );
  }

  if (!data?.items.length) {
    return (
      <div className="rounded-2xl border border-dashed border-sand-200 py-12 text-center text-sm text-sand-500">
        No hay casos de bienestar pendientes.
      </div>
    );
  }

  return (
    <section className="space-y-4">
      <div className="rounded-2xl border border-sand-200 bg-sand-50 p-4">
        <p className="text-sm font-black text-sand-900">
          Bienestar animal SENASA-ready
        </p>
        <p className="mt-1 text-xs text-sand-500">
          Cola interna para triage, severidad, asignación operativa y cierre
          documentado sin exponer datos sensibles.
        </p>
      </div>
      <ul className="space-y-3">
        {data.items.map((welfareCase) => (
          <WelfareCaseCard key={welfareCase.id} welfareCase={welfareCase} />
        ))}
      </ul>
    </section>
  );
}
