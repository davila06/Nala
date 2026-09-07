import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Button } from "@/shared/ui";
import { Skeleton } from "@/shared/ui/Spinner";
import { toast } from "@/shared/lib/toast";
import { apiClient } from "@/shared/lib/apiClient";

type Incident = {
  id: string;
  serviceProviderId: string;
  type: string;
  status: "Open" | "Investigating" | "Resolved" | "Appealed" | "Closed";
  description: string;
  resolution: string | null;
  createdAt: string;
};

export function AdminProviderIncidentsTab() {
  const queryClient = useQueryClient();
  const { data: incidents = [], isLoading } = useQuery({
    queryKey: ["admin", "provider-incidents"],
    queryFn: () =>
      apiClient
        .get<Incident[]>("/admin/service-providers/incidents")
        .then(({ data }) => data),
  });
  const transition = useMutation({
    mutationFn: ({
      id,
      action,
      body,
    }: {
      id: string;
      action: string;
      body?: object;
    }) =>
      apiClient.put(
        `/admin/service-providers/incidents/${id}/${action}`,
        body ?? {},
      ),
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: ["admin", "provider-incidents"],
      }),
  });
  if (isLoading) return <Skeleton className="h-40 rounded-xl" />;
  return (
    <section className="space-y-4">
      <header>
        <h2 className="font-display text-xl font-semibold text-ink-900">
          Incidentes de proveedores
        </h2>
        <p className="text-sm text-sand-600">
          Triage, resolución y cierre con auditoría.
        </p>
      </header>
      {incidents.length === 0 ? (
        <p className="py-10 text-center text-sm text-sand-500">
          No hay incidentes.
        </p>
      ) : (
        <ul className="space-y-3">
          {incidents.map((incident) => (
            <li
              key={incident.id}
              className="rounded-xl border border-sand-100 bg-surface p-4"
            >
              <div className="flex flex-wrap justify-between gap-3">
                <div>
                  <p className="font-semibold text-ink-900">{incident.type}</p>
                  <p className="text-sm text-sand-600">
                    {incident.description}
                  </p>
                </div>
                <span className="rounded-full bg-sand-100 px-2 py-1 text-xs font-semibold">
                  {incident.status}
                </span>
              </div>
              <div className="mt-4 flex gap-2">
                {incident.status === "Open" ||
                incident.status === "Appealed" ? (
                  <Button
                    size="sm"
                    loading={transition.isPending}
                    onClick={() =>
                      transition.mutate(
                        {
                          id: incident.id,
                          action: "investigation",
                          body: {
                            assignedToUserId: incident.serviceProviderId,
                          },
                        },
                        {
                          onSuccess: () =>
                            toast.success("Investigación iniciada"),
                          onError: () =>
                            toast.error("No se pudo iniciar la investigación"),
                        },
                      )
                    }
                  >
                    Investigar
                  </Button>
                ) : null}
                {incident.status === "Investigating" ? (
                  <Button
                    size="sm"
                    loading={transition.isPending}
                    onClick={() =>
                      transition.mutate(
                        {
                          id: incident.id,
                          action: "resolve",
                          body: {
                            resolution: "Resolución administrativa registrada.",
                          },
                        },
                        {
                          onSuccess: () => toast.success("Incidente resuelto"),
                          onError: () => toast.error("No se pudo resolver"),
                        },
                      )
                    }
                  >
                    Resolver
                  </Button>
                ) : null}
                {incident.status === "Resolved" ? (
                  <Button
                    size="sm"
                    loading={transition.isPending}
                    onClick={() =>
                      transition.mutate(
                        { id: incident.id, action: "close" },
                        {
                          onSuccess: () => toast.success("Incidente cerrado"),
                          onError: () => toast.error("No se pudo cerrar"),
                        },
                      )
                    }
                  >
                    Cerrar
                  </Button>
                ) : null}
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
