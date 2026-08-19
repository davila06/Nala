import { lazy, Suspense } from "react";
import { Link } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import { Skeleton } from "@/shared/ui/Spinner";
import { useMyStore } from "../hooks/useStores";
import { useIncomingOrders } from "../hooks/useStoreOrders";
import { ORDER_STATUS_COLORS, ORDER_STATUS_LABELS } from "../api/storesApi";

export default function StoreDashboardPage() {
  const { data: store, isLoading } = useMyStore();
  const { data: orders = [] } = useIncomingOrders();

  const pendingOrders = orders.filter(
    (o) => o.status === "PendingPayment" || o.status === "PaymentReported",
  );

  if (isLoading) return <div className="mx-auto max-w-2xl px-4 py-10"><Skeleton className="h-48 rounded-2xl" /></div>;

  if (!store) return (
    <div className="mx-auto max-w-lg px-4 py-10 text-center">
      <p className="text-sand-500">No tienes una tienda registrada.</p>
      <Link to="/tienda/registro" className="mt-4 inline-block text-brand-600 underline">Registrar tienda</Link>
    </div>
  );

  return (
    <div className="mx-auto max-w-2xl px-4 py-8 space-y-6 animate-fade-in-up">
      <Helmet><title>{store.name} — Panel · PawTrack CR</title></Helmet>

      {/* Header */}
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-3">
          {store.logoUrl && (
            <img src={store.logoUrl} alt={store.name} className="h-14 w-14 rounded-2xl object-cover border border-sand-200" />
          )}
          <div>
            <h1 className="font-display text-xl font-bold text-sand-900">{store.name}</h1>
            <span className={`rounded-full px-2 py-0.5 text-xs font-semibold ${
              store.status === "Active" ? "bg-rescue-100 text-rescue-700" : "bg-warn-100 text-warn-700"
            }`}>
              {store.status === "Active" ? "✓ Activa" : store.status === "Pending" ? "⏳ Pendiente" : "Suspendida"}
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

      {/* Quick stats */}
      <div className="grid grid-cols-3 gap-3">
        {[
          { label: "Órdenes pendientes", value: pendingOrders.length, icon: "🔔", color: "text-warn-700", link: "/tienda/portal/ordenes" },
          { label: "Órdenes activas", value: orders.filter(o => !["Delivered","Cancelled"].includes(o.status)).length, icon: "📦", color: "text-brand-700", link: "/tienda/portal/ordenes" },
          { label: "Productos", value: "—", icon: "🏷️", color: "text-trust-700", link: "/tienda/portal/productos" },
        ].map((s) => (
          <Link key={s.label} to={s.link} className="rounded-2xl border border-sand-100 bg-surface p-4 hover:bg-sand-50 transition-colors">
            <p className="text-2xl" aria-hidden="true">{s.icon}</p>
            <p className={`text-2xl font-black ${s.color}`}>{s.value}</p>
            <p className="text-xs text-sand-500 mt-0.5">{s.label}</p>
          </Link>
        ))}
      </div>

      {/* Recent orders */}
      <div>
        <div className="flex items-center justify-between mb-3">
          <h2 className="font-semibold text-sand-800">Órdenes recientes</h2>
          <Link to="/tienda/portal/ordenes" className="text-xs text-brand-600 hover:underline">Ver todas →</Link>
        </div>
        {orders.length === 0 ? (
          <p className="py-6 text-center text-sm text-sand-400">Aún no hay pedidos.</p>
        ) : (
          <ul className="space-y-2">
            {orders.slice(0, 5).map((order) => (
              <li key={order.id} className="flex items-center gap-3 rounded-xl border border-sand-100 bg-surface p-3">
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold text-sand-900 truncate">
                    Pedido #{order.id.slice(-6).toUpperCase()}
                  </p>
                  <p className="text-xs text-sand-500">
                    ₡{order.totalCrc.toLocaleString("es-CR")} · {order.fulfillmentType === "Pickup" ? "Retiro" : "Entrega"}
                  </p>
                </div>
                <span className={`shrink-0 rounded-full px-2 py-0.5 text-[10px] font-bold ${ORDER_STATUS_COLORS[order.status]}`}>
                  {ORDER_STATUS_LABELS[order.status]}
                </span>
              </li>
            ))}
          </ul>
        )}
      </div>

      {/* Quick links */}
      <div className="grid grid-cols-2 gap-3">
        <Link to="/tienda/portal/productos" className="flex items-center gap-2 rounded-xl border border-sand-200 bg-surface px-4 py-3 text-sm font-semibold text-sand-700 hover:bg-sand-50">
          🏷️ Gestionar productos
        </Link>
        <Link to="/tienda/portal/ordenes" className="flex items-center gap-2 rounded-xl border border-brand-200 bg-brand-50 px-4 py-3 text-sm font-semibold text-brand-700 hover:bg-brand-100">
          📦 Ver pedidos
        </Link>
      </div>
    </div>
  );
}
