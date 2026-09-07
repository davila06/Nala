import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Button, Input } from "@/shared/ui";
import { Skeleton } from "@/shared/ui/Spinner";
import { toast } from "@/shared/lib/toast";
import { apiClient } from "@/shared/lib/apiClient";
import type { ProviderVerificationDto } from "../api/serviceProvidersApi";

function nextYear() {
  const date = new Date();
  date.setFullYear(date.getFullYear() + 1);
  return date.toISOString().slice(0, 10);
}

export function AdminProviderVerificationsTab() {
  const queryClient = useQueryClient();
  const [expiresAt, setExpiresAt] = useState(nextYear);
  const [reason, setReason] = useState("");
  const { data: verifications = [], isLoading } = useQuery({
    queryKey: ["admin", "provider-verifications"],
    queryFn: () =>
      apiClient
        .get<
          ProviderVerificationDto[]
        >("/admin/service-providers/verifications/pending")
        .then((response) => response.data),
  });
  const review = useMutation({
    mutationFn: ({ id, approve }: { id: string; approve: boolean }) =>
      apiClient.put(`/admin/service-providers/verifications/${id}/review`, {
        approve,
        expiresAt: approve ? expiresAt : null,
        reason: approve ? "Documentacion revisada" : reason,
      }),
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: ["admin", "provider-verifications"],
      }),
  });
  const submitReview = (id: string, approve: boolean) => {
    if (!approve && !reason.trim())
      return toast.error("Indica el motivo de rechazo.");
    review.mutate(
      { id, approve },
      {
        onSuccess: () =>
          toast.success(
            approve ? "Verificacion aprobada" : "Verificacion rechazada",
          ),
        onError: () => toast.error("No se pudo actualizar la verificacion."),
      },
    );
  };
  const download = async (id: string) => {
    try {
      const fileBlob = await apiClient
        .get(`/admin/service-providers/verifications/${id}/document`, {
          responseType: "blob",
        })
        .then((response) => response.data as Blob);
      const url = URL.createObjectURL(fileBlob);
      window.open(url, "_blank", "noopener,noreferrer");
      window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
    } catch {
      toast.error("No se pudo descargar el documento.");
    }
  };
  if (isLoading) return <Skeleton className="h-40 rounded-xl" />;
  return (
    <section className="space-y-4">
      <header>
        <h2 className="font-display text-xl font-semibold text-ink-900">
          Verificaciones de proveedores
        </h2>
        <p className="text-sm text-sand-600">
          Revisa documentos privados y registra una decision auditable.
        </p>
      </header>
      <div className="grid gap-3 rounded-xl border border-sand-100 bg-surface p-4 sm:grid-cols-2">
        <label className="text-sm font-medium text-sand-700">
          Vencimiento al aprobar
          <Input
            type="date"
            value={expiresAt}
            onChange={(event) => setExpiresAt(event.target.value)}
            className="mt-1"
          />
        </label>
        <label className="text-sm font-medium text-sand-700">
          Motivo al rechazar
          <Input
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            className="mt-1"
            placeholder="Documento ilegible o incompleto"
          />
        </label>
      </div>
      {verifications.length === 0 ? (
        <p className="py-10 text-center text-sm text-sand-500">
          No hay verificaciones de proveedores pendientes.
        </p>
      ) : (
        <ul className="space-y-3">
          {verifications.map((verification) => (
            <li
              key={verification.id}
              className="rounded-xl border border-sand-100 bg-surface p-4"
            >
              <p className="font-semibold text-ink-900">
                Verificacion {verification.id.slice(-8).toUpperCase()}
              </p>
              <p className="mt-1 text-sm text-sand-600">
                Enviada:{" "}
                {new Date(verification.submittedAt).toLocaleDateString("es-CR")}
              </p>
              <div className="mt-4 flex gap-2">
                <Button
                  size="sm"
                  variant="secondary"
                  onClick={() => void download(verification.id)}
                >
                  Ver documento
                </Button>
                <Button
                  size="sm"
                  loading={review.isPending}
                  onClick={() => submitReview(verification.id, true)}
                >
                  Aprobar
                </Button>
                <Button
                  size="sm"
                  variant="danger"
                  loading={review.isPending}
                  onClick={() => submitReview(verification.id, false)}
                >
                  Rechazar
                </Button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
