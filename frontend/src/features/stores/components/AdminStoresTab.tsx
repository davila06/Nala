import { toast } from "@/shared/lib/toast";
import { Button } from "@/shared/ui";
import { Skeleton } from "@/shared/ui/Spinner";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/lib/apiClient";
import type { PublicStoreDto } from "@/features/stores/api/storesApi";

function useAdminPendingStores() {
  return useQuery({
    queryKey: ["admin-stores-pending"],
    queryFn: () =>
      apiClient
        .get<PublicStoreDto[]>("/admin/stores/pending")
        .then((r) => r.data),
    staleTime: 30_000,
  });
}

function useReviewStore() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ storeId, approve }: { storeId: string; approve: boolean }) =>
      apiClient.put(`/admin/stores/${storeId}/review`, { approve }),
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["admin-stores-pending"] }),
  });
}

export function AdminStoresTab() {
  const { data: stores = [], isLoading } = useAdminPendingStores();
  const review = useReviewStore();

  if (isLoading) return <Skeleton className="h-32 rounded-2xl" />;

  if (stores.length === 0) {
    return (
      <div className="rounded-2xl border border-sand-100 bg-surface-warm p-8 text-center">
        <p className="text-2xl mb-2" aria-hidden="true">
          🛒
        </p>
        <p className="text-sm text-sand-500">
          No hay tiendas pendientes de revisión.
        </p>
      </div>
    );
  }

  return (
    <ul className="space-y-4">
      {stores.map((store) => (
        <li
          key={store.id}
          className="rounded-2xl border border-sand-200 bg-surface p-5 space-y-3"
        >
          <div className="flex items-start justify-between gap-3">
            <div className="flex items-start gap-3">
              {store.logoUrl && (
                <img
                  src={store.logoUrl}
                  alt={store.name}
                  className="h-12 w-12 rounded-xl object-cover shrink-0 border border-sand-200"
                />
              )}
              <div>
                <p className="font-semibold text-sand-900">{store.name}</p>
                <p className="text-xs text-sand-500 mt-0.5">{store.address}</p>
                <p className="text-xs text-sand-400 mt-0.5 line-clamp-2">
                  {store.description}
                </p>
              </div>
            </div>
          </div>

          <div className="flex gap-2">
            <Button
              size="sm"
              variant="rescue"
              loading={review.isPending}
              onClick={() =>
                review.mutate(
                  { storeId: store.id, approve: true },
                  {
                    onSuccess: () => toast.success(`${store.name} aprobada`),
                    onError: () => toast.error("Error al aprobar"),
                  },
                )
              }
            >
              ✓ Aprobar
            </Button>
            <Button
              size="sm"
              variant="danger"
              loading={review.isPending}
              onClick={() =>
                review.mutate(
                  { storeId: store.id, approve: false },
                  {
                    onSuccess: () => toast.success(`${store.name} rechazada`),
                    onError: () => toast.error("Error al rechazar"),
                  },
                )
              }
            >
              ✕ Rechazar
            </Button>
          </div>
        </li>
      ))}
    </ul>
  );
}
