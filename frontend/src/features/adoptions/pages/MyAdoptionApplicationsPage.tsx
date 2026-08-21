import { useState } from "react";
import { Link } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import { Skeleton } from "@/shared/ui/Spinner";
import { useMyAdoptionApplications, useWithdrawApplication } from "../hooks/useAdoptions";
import type { AdoptionApplicationDto } from "../api/adoptionsApi";
import { toast } from "@/shared/lib/toast";

const STATUS_LABELS: Record<string, { label: string; color: string }> = {
  Pending:     { label: "Pendiente", color: "bg-sand-100 text-sand-600" },
  UnderReview: { label: "En revisión", color: "bg-blue-50 text-blue-700" },
  Approved:    { label: "Aprobada ✓", color: "bg-green-50 text-green-700" },
  Rejected:    { label: "No aprobada", color: "bg-red-50 text-red-600" },
  Withdrawn:   { label: "Retirada", color: "bg-sand-100 text-sand-400" },
};

export default function MyAdoptionApplicationsPage() {
  const [page, setPage] = useState(1);
  const { data, isLoading } = useMyAdoptionApplications(page);
  const withdraw = useWithdrawApplication();

  const apps = (data?.items ?? []) as AdoptionApplicationDto[];

  const handleWithdraw = (id: string) => {
    if (!confirm("¿Retirar esta solicitud?")) return;
    withdraw.mutate(id, {
      onSuccess: () => toast.success("Solicitud retirada"),
    });
  };

  return (
    <>
      <Helmet><title>Mis solicitudes · Adopciones · PawTrack CR</title></Helmet>

      <div className="mx-auto max-w-2xl px-4 py-8 space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-xl font-bold text-ink-900">Mis solicitudes de adopción</h1>
          <Link to="/adopciones" className="text-sm text-brand-600 hover:underline">
            Ver animales →
          </Link>
        </div>

        {isLoading ? (
          <div className="space-y-3">
            {[1, 2, 3].map((i) => <Skeleton key={i} className="h-24 rounded-xl" />)}
          </div>
        ) : apps.length === 0 ? (
          <div className="py-16 text-center text-sand-400">
            <p className="text-4xl mb-3">🐾</p>
            <p className="text-sm font-medium">No tienes solicitudes activas</p>
            <Link to="/adopciones" className="mt-3 inline-block text-brand-600 underline text-sm">
              Explorar animales en adopción
            </Link>
          </div>
        ) : (
          <div className="space-y-3">
            {apps.map((app: AdoptionApplicationDto) => {
              const st = STATUS_LABELS[app.status] ?? { label: app.status, color: "" };
              return (
                <div
                  key={app.id}
                  className="rounded-xl border border-sand-100 bg-surface p-4 space-y-2"
                >
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <Link
                        to={`/adopciones/${app.adoptablePetId}`}
                        className="text-sm font-semibold text-ink-800 hover:text-brand-600"
                      >
                        Ver animal →
                      </Link>
                      <p className="text-xs text-sand-400 mt-0.5">
                        Enviada {new Date(app.appliedAt).toLocaleDateString("es-CR")}
                      </p>
                    </div>
                    <span className={`text-xs font-medium px-2.5 py-1 rounded-full ${st.color}`}>
                      {st.label}
                    </span>
                  </div>

                  <p className="text-xs text-sand-500 line-clamp-2">{app.applicantNote}</p>

                  {app.reviewNote && (
                    <p className="text-xs text-ink-600 bg-sand-50 rounded-lg px-3 py-2">
                      Respuesta: {app.reviewNote}
                    </p>
                  )}

                  {(app.status === "Pending" || app.status === "UnderReview") && (
                    <button
                      onClick={() => handleWithdraw(app.id)}
                      disabled={withdraw.isPending}
                      className="text-xs text-sand-400 hover:text-red-500 underline transition-colors"
                    >
                      Retirar solicitud
                    </button>
                  )}
                </div>
              );
            })}
          </div>
        )}

        {(data?.totalPages ?? 1) > 1 && (
          <div className="flex items-center justify-between pt-4 border-t border-sand-100">
            <button
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
              className="px-4 py-2 rounded-xl border border-sand-200 text-sm disabled:opacity-40"
            >
              ← Anterior
            </button>
            <span className="text-sm text-sand-400">Página {page} de {data?.totalPages}</span>
            <button
              disabled={!data?.hasNextPage}
              onClick={() => setPage((p) => p + 1)}
              className="px-4 py-2 rounded-xl border border-sand-200 text-sm disabled:opacity-40"
            >
              Siguiente →
            </button>
          </div>
        )}
      </div>
    </>
  );
}
