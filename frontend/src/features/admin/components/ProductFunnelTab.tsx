import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/shared/lib/apiClient";

interface ProductFunnel {
  from: string;
  to: string;
  canton: string | null;
  events: Record<string, number>;
}

function useProductFunnel() {
  return useQuery({
    queryKey: ["admin-product-funnel"],
    queryFn: () =>
      apiClient
        .get<ProductFunnel>("/product-events/funnel", {
          params: { from: new Date(Date.now() - 30 * 86400000).toISOString() },
        })
        .then((response) => response.data),
    staleTime: 60_000,
  });
}

const LABELS: Record<string, string> = {
  PetRegistered: "Mascotas registradas",
  PetProfileCompleted: "Perfiles completados",
  QrGenerated: "QR generados",
  QrScanned: "QR escaneados",
  LostPetReported: "Pérdidas reportadas",
  FoundPetReported: "Mascotas encontradas",
};

export function ProductFunnelTab() {
  const { data, isLoading, isError } = useProductFunnel();
  const entries = Object.entries(data?.events ?? {}).filter(
    ([name]) => LABELS[name],
  );
  const max = Math.max(...entries.map(([, count]) => count), 1);

  if (isLoading)
    return <p className="p-6 text-sm text-sand-500">Cargando funnel...</p>;
  if (isError)
    return (
      <p role="alert" className="p-6 text-sm text-danger-600">
        No se pudo cargar el funnel.
      </p>
    );

  return (
    <section className="rounded-2xl border border-sand-200 bg-surface p-5 shadow-sm">
      <div className="mb-5">
        <h2 className="text-lg font-bold text-sand-900">
          Funnel de activación
        </h2>
        <p className="text-sm text-sand-500">
          Eventos agregados de los últimos 30 días. Sin PII.
        </p>
      </div>
      {entries.length === 0 ? (
        <p className="text-sm text-sand-500">Aún no hay eventos de producto.</p>
      ) : (
        <ul className="space-y-4">
          {entries.map(([name, count]) => (
            <li key={name}>
              <div className="mb-1 flex justify-between text-sm">
                <span className="font-medium text-sand-700">
                  {LABELS[name]}
                </span>
                <span className="font-bold tabular-nums text-sand-900">
                  {count.toLocaleString("es-CR")}
                </span>
              </div>
              <div className="h-3 overflow-hidden rounded-full bg-sand-100">
                <div
                  className="h-full rounded-full bg-brand-500"
                  style={{ width: `${Math.max((count / max) * 100, 2)}%` }}
                />
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
