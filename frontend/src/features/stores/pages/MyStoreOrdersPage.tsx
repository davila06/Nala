import { Helmet } from "react-helmet-async";
import { useState } from "react";
import { Skeleton } from "@/shared/ui/Spinner";
import { ORDER_STATUS_COLORS, ORDER_STATUS_LABELS } from "../api/storesApi";
import type { StoreOrderDto, StoreOrderStatus } from "../api/storesApi";
import { useMyOrders } from "../hooks/useStoreOrders";

const TERMINAL: StoreOrderStatus[] = ["Delivered", "Cancelled"];

const ICON: Record<StoreOrderStatus, string> = {
  PendingPayment: "💳",
  PaymentReported: "✅",
  Confirmed: "📋",
  Preparing: "👨‍🍳",
  ReadyForPickup: "🏪",
  OutForDelivery: "🚚",
  Delivered: "🎉",
  Cancelled: "❌",
};

function OrderRow({ order }: { order: StoreOrderDto }) {
  const isTerminal = TERMINAL.includes(order.status);

  return (
    <li className="rounded-2xl border border-sand-100 bg-surface p-4 space-y-3">
      {/* Header */}
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="font-semibold text-ink-900 text-sm line-clamp-1">
            {order.storeName}
          </p>
          <p className="text-xs text-sand-500">
            {new Date(order.placedAt).toLocaleDateString("es-CR", {
              day: "2-digit",
              month: "long",
              year: "numeric",
            })}
          </p>
        </div>
        <span
          className={`text-xs font-semibold rounded-full px-2.5 py-0.5 ${ORDER_STATUS_COLORS[order.status]}`}
        >
          {ICON[order.status]} {ORDER_STATUS_LABELS[order.status]}
        </span>
      </div>

      {/* Items */}
      <ul className="text-xs text-sand-700 space-y-0.5">
        {order.items.map((l) => (
          <li key={l.productId} className="flex justify-between">
            <span>
              {l.productName} × {l.quantity}
            </span>
            <span>
              ₡{(l.unitPriceCrc * l.quantity).toLocaleString("es-CR")}
            </span>
          </li>
        ))}
      </ul>

      {/* Footer */}
      <div className="flex items-center justify-between pt-1 border-t border-sand-100">
        <span className="text-xs text-sand-500">
          {order.fulfillmentType === "Delivery"
            ? "🚚 Entrega"
            : "🏪 Retiro en tienda"}
        </span>
        <span className="font-semibold text-ink-900 text-sm">
          ₡{order.totalCrc.toLocaleString("es-CR")}
        </span>
      </div>

      {/* Progress bar (non-terminal) */}
      {!isTerminal && (
        <ProgressBar
          status={order.status}
          fulfillment={order.fulfillmentType}
        />
      )}
    </li>
  );
}

const STEPS_DELIVERY: StoreOrderStatus[] = [
  "PendingPayment",
  "PaymentReported",
  "Confirmed",
  "Preparing",
  "OutForDelivery",
  "Delivered",
];
const STEPS_PICKUP: StoreOrderStatus[] = [
  "PendingPayment",
  "PaymentReported",
  "Confirmed",
  "Preparing",
  "ReadyForPickup",
  "Delivered",
];

function ProgressBar({
  status,
  fulfillment,
}: {
  status: StoreOrderStatus;
  fulfillment: string;
}) {
  const steps = fulfillment === "Delivery" ? STEPS_DELIVERY : STEPS_PICKUP;
  const current = steps.indexOf(status);
  const pct =
    current < 0 ? 0 : Math.round((current / (steps.length - 1)) * 100);

  return (
    <div>
      <div className="relative h-1.5 bg-sand-200 rounded-full overflow-hidden">
        <div
          className="absolute inset-y-0 left-0 bg-brand-500 rounded-full transition-all duration-700"
          style={{ width: `${pct}%` }}
        />
      </div>
      <div className="flex justify-between mt-1">
        {steps.map((s, i) => (
          <span
            key={s}
            className={`text-[9px] leading-none ${i <= current ? "text-brand-600 font-semibold" : "text-sand-400"}`}
          >
            {ICON[s]}
          </span>
        ))}
      </div>
    </div>
  );
}

export default function MyStoreOrdersPage() {
  const [page, setPage] = useState(1);
  const { data: orders = [], isLoading } = useMyOrders(page);

  const active = orders.filter((o) => !TERMINAL.includes(o.status));
  const past = orders.filter((o) => TERMINAL.includes(o.status));
  const hasMore = orders.length === 20; // 20 = pageSize

  return (
    <>
      <Helmet>
        <title>Mis pedidos · PawTrack CR</title>
      </Helmet>

      <div className="mx-auto max-w-lg px-4 py-8 space-y-8">
        <h1 className="text-2xl font-bold text-ink-900">Mis pedidos</h1>

        {isLoading && (
          <div className="space-y-3">
            {[...Array(3)].map((_, i) => (
              <Skeleton key={i} className="h-32 rounded-2xl" />
            ))}
          </div>
        )}

        {!isLoading && orders.length === 0 && (
          <div className="text-center py-16 text-sand-400 space-y-2">
            <p className="text-4xl">🛒</p>
            <p className="font-semibold text-sand-600">
              Aún no has hecho pedidos
            </p>
            <p className="text-sm">
              Explora las tiendas en el mapa y agrega productos a tu carrito.
            </p>
          </div>
        )}

        {active.length > 0 && (
          <section className="space-y-3">
            <h2 className="text-sm font-semibold text-sand-600 uppercase tracking-wide">
              En curso
            </h2>
            <ul className="space-y-3">
              {active.map((o) => (
                <OrderRow key={o.id} order={o} />
              ))}
            </ul>
          </section>
        )}

        {past.length > 0 && (
          <section className="space-y-3">
            <h2 className="text-sm font-semibold text-sand-600 uppercase tracking-wide">
              Historial
            </h2>
            <ul className="space-y-3">
              {past.map((o) => (
                <OrderRow key={o.id} order={o} />
              ))}
            </ul>
          </section>
        )}

        {/* Pagination */}
        {!isLoading && (page > 1 || hasMore) && (
          <div className="flex items-center justify-center gap-4 pt-2">
            <button
              type="button"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
              className="rounded-xl border border-sand-200 px-4 py-2 text-sm font-medium text-sand-700 hover:bg-sand-50 disabled:opacity-40 disabled:cursor-not-allowed"
            >
              ← Anterior
            </button>
            <span className="text-xs text-sand-500">Página {page}</span>
            <button
              type="button"
              onClick={() => setPage((p) => p + 1)}
              disabled={!hasMore}
              className="rounded-xl border border-sand-200 px-4 py-2 text-sm font-medium text-sand-700 hover:bg-sand-50 disabled:opacity-40 disabled:cursor-not-allowed"
            >
              Siguiente →
            </button>
          </div>
        )}
      </div>
    </>
  );
}
