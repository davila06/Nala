import { Link } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import { Skeleton } from "@/shared/ui/Spinner";
import { useMyStore, useMyStoreProducts } from "../hooks/useStores";
import { useIncomingOrders } from "../hooks/useStoreOrders";
import { ORDER_STATUS_COLORS, ORDER_STATUS_LABELS } from "../api/storesApi";
import { useMySubscription } from "@/features/pets/hooks/useSubscription";

const TIER_LABELS: Record<string, string> = {
  StoreBasic: "Tienda Básica (gratis)",
  StorePlus: "Tienda Plus",
  StorePartner: "Tienda Partner",
};

const TIER_NEXT: Record<string, string | null> = {
  StoreBasic: "Actualiza a Plus (₡12,000/mes) para recibir pedidos in-app.",
  StorePlus:
    "Actualiza a Partner (₡25,000/mes) para analíticas avanzadas y sedes.",
  StorePartner: null,
};

export default function StoreDashboardPage() {
  const { data: store, isLoading } = useMyStore();
  const { data: orders = [] } = useIncomingOrders();
  const { data: products = [] } = useMyStoreProducts();
  const { data: sub } = useMySubscription();

  const tier = sub?.isActive ? (sub.tier ?? "StoreBasic") : "StoreBasic";
  const tierLabel = TIER_LABELS[tier] ?? tier;
  const nextMsg = TIER_NEXT[tier] ?? null;

  const pendingOrders = orders.filter(
    (o) => o.status === "PendingPayment" || o.status === "PaymentReported",
  );

  if (isLoading)
    return (
      <div className="mx-auto max-w-2xl px-4 py-10">
        <Skeleton className="h-48 rounded-2xl" />
      </div>
    );

  if (!store)
    return (
      <div className="mx-auto max-w-lg px-4 py-10 text-center">
        <p className="text-sand-500">No tienes una tienda registrada.</p>
        <Link
          to="/tienda/registro"
          className="mt-4 inline-block text-brand-600 underline"
        >
          Registrar tienda
        </Link>
      </div>
    );

  return (
    <div className="mx-auto max-w-2xl px-4 py-8 space-y-6 animate-fade-in-up">
      <Helmet>
        <title>{store.name} — Panel · PawTrack CR</title>
      </Helmet>

      {/* Header */}
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-3">
          {store.logoUrl && (
            <img
              src={store.logoUrl}
              alt={store.name}
              className="h-14 w-14 rounded-2xl object-cover border border-sand-200"
            />
          )}
          <div>
            <h1 className="font-display text-xl font-bold text-sand-900">
              {store.name}
            </h1>
            <span
              className={`rounded-full px-2 py-0.5 text-xs font-semibold ${
                store.status === "Active"
                  ? "bg-rescue-100 text-rescue-700"
                  : "bg-warn-100 text-warn-700"
              }`}
            >
              {store.status === "Active"
                ? "✓ Activa"
                : store.status === "Pending"
                  ? "⏳ Pendiente"
                  : "Suspendida"}
            </span>
          </div>
        </div>
        <Link
          to="/tienda/portal/perfil"
          className="rounded-xl border border-sand-200 px-3 py-2 text-xs font-semibold text-sand-700 hover:bg-sand-50"
        >
          Editar perfil
        </Link>
      </div>

      {/* Plan status */}
      <div
        className={`rounded-2xl border px-4 py-3 flex items-center gap-3 ${
          tier === "StorePartner"
            ? "border-rescue-200 bg-rescue-50"
            : tier === "StorePlus"
              ? "border-brand-200 bg-brand-50"
              : "border-sand-200 bg-sand-50"
        }`}
      >
        <span className="text-xl shrink-0" aria-hidden="true">
          {tier === "StorePartner" ? "🌟" : tier === "StorePlus" ? "⭐" : "🏪"}
        </span>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-semibold text-sand-900">{tierLabel}</p>
          {sub?.expiresAt && (
            <p className="text-xs text-sand-500 mt-0.5">
              Vence: {new Date(sub.expiresAt).toLocaleDateString("es-CR")}
            </p>
          )}
          {nextMsg && <p className="text-xs text-sand-500 mt-0.5">{nextMsg}</p>}
        </div>
        {nextMsg && (
          <Link
            to="/perfil"
            className="shrink-0 rounded-xl bg-brand-600 px-3 py-1.5 text-xs font-bold text-white hover:bg-brand-700"
          >
            Mejorar →
          </Link>
        )}
      </div>

      {/* Quick stats */}
      <div className="grid grid-cols-3 gap-3">
        {[
          {
            label: "Órdenes pendientes",
            value: pendingOrders.length,
            icon: "🔔",
            color: "text-warn-700",
            link: "/tienda/portal/ordenes",
          },
          {
            label: "Órdenes activas",
            value: orders.filter(
              (o) => !["Delivered", "Cancelled"].includes(o.status),
            ).length,
            icon: "📦",
            color: "text-brand-700",
            link: "/tienda/portal/ordenes",
          },
          {
            label: "Productos",
            value: String(products.length),
            icon: "🏷️",
            color: "text-trust-700",
            link: "/tienda/portal/productos",
          },
        ].map((s) => (
          <Link
            key={s.label}
            to={s.link}
            className="rounded-2xl border border-sand-100 bg-surface p-4 hover:bg-sand-50 transition-colors"
          >
            <p className="text-2xl" aria-hidden="true">
              {s.icon}
            </p>
            <p className={`text-2xl font-black ${s.color}`}>{s.value}</p>
            <p className="text-xs text-sand-500 mt-0.5">{s.label}</p>
          </Link>
        ))}
      </div>

      {/* Recent orders */}
      <div>
        <div className="flex items-center justify-between mb-3">
          <h2 className="font-semibold text-sand-800">Órdenes recientes</h2>
          <Link
            to="/tienda/portal/ordenes"
            className="text-xs text-brand-600 hover:underline"
          >
            Ver todas →
          </Link>
        </div>
        {orders.length === 0 ? (
          <p className="py-6 text-center text-sm text-sand-400">
            Aún no hay pedidos.
          </p>
        ) : (
          <ul className="space-y-2">
            {orders.slice(0, 5).map((order) => (
              <li
                key={order.id}
                className="flex items-center gap-3 rounded-xl border border-sand-100 bg-surface p-3"
              >
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold text-sand-900 truncate">
                    Pedido #{order.id.slice(-6).toUpperCase()}
                  </p>
                  <p className="text-xs text-sand-500">
                    ₡{order.totalCrc.toLocaleString("es-CR")} ·{" "}
                    {order.fulfillmentType === "Pickup" ? "Retiro" : "Entrega"}
                  </p>
                </div>
                <span
                  className={`shrink-0 rounded-full px-2 py-0.5 text-[10px] font-bold ${ORDER_STATUS_COLORS[order.status]}`}
                >
                  {ORDER_STATUS_LABELS[order.status]}
                </span>
              </li>
            ))}
          </ul>
        )}
      </div>

      {/* Quick links */}
      <div className="grid grid-cols-2 gap-3">
        <Link
          to="/tienda/portal/productos"
          className="flex items-center gap-2 rounded-xl border border-sand-200 bg-surface px-4 py-3 text-sm font-semibold text-sand-700 hover:bg-sand-50"
        >
          🏷️ Gestionar productos
        </Link>
        <Link
          to="/tienda/portal/ordenes"
          className="flex items-center gap-2 rounded-xl border border-brand-200 bg-brand-50 px-4 py-3 text-sm font-semibold text-brand-700 hover:bg-brand-100"
        >
          📦 Ver pedidos
        </Link>
        <Link
          to="/tienda/portal/analitica"
          className="flex items-center gap-2 rounded-xl border border-rescue-200 bg-rescue-50 px-4 py-3 text-sm font-semibold text-rescue-700 hover:bg-rescue-100"
        >
          📊 Analíticas
        </Link>
        <Link
          to="/tienda/portal/sedes"
          className="flex items-center gap-2 rounded-xl border border-trust-200 bg-trust-50 px-4 py-3 text-sm font-semibold text-trust-700 hover:bg-trust-100"
        >
          🏪 Sedes
        </Link>
      </div>
    </div>
  );
}
