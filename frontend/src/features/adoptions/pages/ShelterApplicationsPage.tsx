import { useParams, Link } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import { Skeleton } from "@/shared/ui/Spinner";
import {
  useApplicationsForAnimal,
  useReviewApplication,
} from "../hooks/useAdoptions";
import { useAdoptableAnimal } from "../hooks/useAdoptions";
import { toast } from "@/shared/lib/toast";
import { useState } from "react";

export default function ShelterApplicationsPage() {
  const { id } = useParams<{ id: string }>();
  const { data: animal } = useAdoptableAnimal(id ?? "");
  const { data: appsPage, isLoading } = useApplicationsForAnimal(id ?? "");
  const apps = appsPage?.items;
  const review = useReviewApplication();
  const [reviewNotes, setReviewNotes] = useState<Record<string, string>>({});

  const handleReview = (applicationId: string, approve: boolean) => {
    const note = reviewNotes[applicationId];
    review.mutate(
      { applicationId, approve, reviewNote: note || undefined },
      {
        onSuccess: () =>
          toast.success(approve ? "Solicitud aprobada" : "Solicitud rechazada"),
      },
    );
  };

  return (
    <>
      <Helmet>
        <title>Solicitudes · {animal?.name ?? "Animal"} · PawTrack CR</title>
      </Helmet>

      <div className="mx-auto max-w-2xl px-4 py-8 space-y-6">
        <div className="flex items-center gap-3">
          <Link
            to="/shelter/dashboard"
            className="text-sm text-brand-600 hover:underline"
          >
            ← Panel
          </Link>
          <h1 className="text-xl font-bold text-ink-900">
            Solicitudes para {animal?.name ?? "…"}
          </h1>
        </div>

        {isLoading ? (
          <div className="space-y-3">
            {[1, 2].map((i) => (
              <Skeleton key={i} className="h-32 rounded-xl" />
            ))}
          </div>
        ) : !apps || apps.length === 0 ? (
          <div className="py-16 text-center text-sand-400">
            <p className="text-4xl mb-3">📭</p>
            <p className="text-sm font-medium">Sin solicitudes todavía</p>
          </div>
        ) : (
          <div className="space-y-4">
            {apps.map((app) => (
              <div
                key={app.id}
                className="rounded-xl border border-sand-100 bg-surface p-4 space-y-3"
              >
                <div className="flex items-start justify-between">
                  <p className="text-xs text-sand-400">
                    Solicitud recibida el{" "}
                    {new Date(app.appliedAt).toLocaleDateString("es-CR")}
                  </p>
                  <StatusBadge status={app.status} />
                </div>

                <p className="text-sm text-ink-700">{app.applicantNote}</p>

                {app.status === "Pending" && (
                  <div className="space-y-2 pt-2 border-t border-sand-100">
                    <textarea
                      value={reviewNotes[app.id] ?? ""}
                      onChange={(e) =>
                        setReviewNotes((n) => ({
                          ...n,
                          [app.id]: e.target.value,
                        }))
                      }
                      placeholder="Nota de respuesta (opcional, se enviará al solicitante)"
                      maxLength={300}
                      rows={2}
                      className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400 resize-none"
                    />
                    <div className="flex gap-2">
                      <button
                        onClick={() => handleReview(app.id, true)}
                        disabled={review.isPending}
                        className="flex-1 bg-green-500 hover:bg-green-600 text-white text-sm font-semibold py-2 rounded-xl disabled:opacity-50 transition-colors"
                      >
                        Aprobar ✓
                      </button>
                      <button
                        onClick={() => handleReview(app.id, false)}
                        disabled={review.isPending}
                        className="flex-1 border border-sand-200 text-ink-700 hover:border-red-400 hover:text-red-500 text-sm font-semibold py-2 rounded-xl disabled:opacity-50 transition-colors"
                      >
                        Rechazar
                      </button>
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </>
  );
}

function StatusBadge({ status }: { status: string }) {
  const map: Record<string, string> = {
    Pending: "bg-sand-100 text-sand-600",
    UnderReview: "bg-blue-50 text-blue-700",
    Approved: "bg-green-50 text-green-700",
    Rejected: "bg-red-50 text-red-600",
    Withdrawn: "bg-sand-100 text-sand-400",
  };
  const labels: Record<string, string> = {
    Pending: "Pendiente",
    UnderReview: "En revisión",
    Approved: "Aprobada",
    Rejected: "Rechazada",
    Withdrawn: "Retirada",
  };
  return (
    <span
      className={`text-[10px] font-bold px-2.5 py-0.5 rounded-full ${map[status] ?? ""}`}
    >
      {labels[status] ?? status}
    </span>
  );
}
