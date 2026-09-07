import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Button, Input } from "@/shared/ui";
import { Skeleton } from "@/shared/ui/Spinner";
import { toast } from "@/shared/lib/toast";
import { apiClient } from "@/shared/lib/apiClient";

interface ServiceProviderAdminDto {
  id: string;
  name: string;
  category: string;
  address: string;
  status: "Active" | "Suspended";
  suspensionReason: string | null;
  registeredAt: string;
}

export function AdminServiceProvidersTab() {
  const queryClient = useQueryClient();
  const [reason, setReason] = useState("");
  const { data: providers = [], isLoading } = useQuery({
    queryKey: ["admin", "service-providers"],
    queryFn: () =>
      apiClient
        .get<ServiceProviderAdminDto[]>("/admin/service-providers")
        .then((response) => response.data),
  });
  const changeStatus = useMutation({
    mutationFn: ({
      id,
      suspend,
      reason,
    }: {
      id: string;
      suspend: boolean;
      reason?: string;
    }) =>
      apiClient.put(`/admin/service-providers/${id}/operational-status`, {
        suspend,
        reason,
      }),
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: ["admin", "service-providers"],
      }),
  });
  const suspend = (id: string) => {
    if (!reason.trim()) return toast.error("Indica el motivo de suspension.");
    changeStatus.mutate(
      { id, suspend: true, reason },
      {
        onSuccess: () => toast.success("Proveedor suspendido"),
        onError: () => toast.error("No se pudo suspender el proveedor."),
      },
    );
  };
  if (isLoading) return <Skeleton className="h-40 rounded-xl" />;
  return (
    <section className="space-y-4">
      <header>
        <h2 className="font-display text-xl font-semibold text-ink-900">
          Operaciones de proveedores
        </h2>
        <p className="text-sm text-sand-600">
          Suspende perfiles por incidentes y reactivalos cuando corresponda.
        </p>
      </header>
      <label className="block max-w-lg text-sm font-medium text-sand-700">
        Motivo de suspension
        <Input
          value={reason}
          onChange={(event) => setReason(event.target.value)}
          placeholder="Incidente abierto"
          className="mt-1"
        />
      </label>
      {providers.length === 0 ? (
        <p className="py-10 text-center text-sm text-sand-500">
          No hay proveedores activos o suspendidos.
        </p>
      ) : (
        <ul className="space-y-3">
          {providers.map((provider) => (
            <li
              key={provider.id}
              className="rounded-xl border border-sand-100 bg-surface p-4"
            >
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <p className="font-semibold text-ink-900">{provider.name}</p>
                  <p className="text-sm text-sand-600">
                    {provider.category} · {provider.address}
                  </p>
                  {provider.suspensionReason ? (
                    <p className="mt-1 text-sm text-danger-600">
                      Motivo: {provider.suspensionReason}
                    </p>
                  ) : null}
                </div>
                <span
                  className={`rounded-full px-2 py-1 text-xs font-semibold ${provider.status === "Active" ? "bg-rescue-100 text-rescue-700" : "bg-danger-100 text-danger-700"}`}
                >
                  {provider.status === "Active" ? "Activo" : "Suspendido"}
                </span>
              </div>
              <div className="mt-4">
                {provider.status === "Active" ? (
                  <Button
                    size="sm"
                    variant="danger"
                    loading={changeStatus.isPending}
                    onClick={() => suspend(provider.id)}
                  >
                    Suspender
                  </Button>
                ) : (
                  <Button
                    size="sm"
                    loading={changeStatus.isPending}
                    onClick={() =>
                      changeStatus.mutate(
                        { id: provider.id, suspend: false },
                        {
                          onSuccess: () =>
                            toast.success("Proveedor reactivado"),
                          onError: () =>
                            toast.error("No se pudo reactivar el proveedor."),
                        },
                      )
                    }
                  >
                    Reactivar
                  </Button>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
