import { Helmet } from "react-helmet-async";
import { useState } from "react";
import { toast } from "@/shared/lib/toast";
import { Skeleton } from "@/shared/ui/Spinner";
import { ORDER_STATUS_COLORS, ORDER_STATUS_LABELS } from "../api/storesApi";
import type { StoreOrderDto, StoreOrderStatus } from "../api/storesApi";
import { useMyOrders, useReportStoreOrderPayment } from "../hooks/useStoreOrders";

const TERMINAL: StoreOrderStatus[] = ["Delivered", "Cancelled", "Rejected", "Expired", "Refunded"];

const ICON: Record<StoreOrderStatus, string> = {
  AwaitingStoreAcceptance: "📨",
  AwaitingPayment: "💳",
  Paid: "✅",
  Expired: "⌛",
  Refunded: "↩️",
  PendingPayment: "💳",
  PaymentReported: "✅",
  Confirmed: "📋",
  Preparing: "👨‍🍳",
  ReadyForPickup: "🏪",
  OutForDelivery: "🚚",
  Delivered: "🎉",
  Cancelled: "❌",
  Rejected: "🚫",
};

function OrderRow({ order }: { order: StoreOrderDto }) {
  const isTerminal = TERMINAL.includes(order.status);
  const reportPayment = useReportStoreOrderPayment();

  return (
    <li className="rounded-2xl border border-sand-100 bg-surface p-4 space-y-3">
      {/* Header */}
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="font-semibold text-ink-900 text-sm line-clamp-1">{order.storeName}</p>
          <p className="text-xs text-copy-secondary">
            {new Date(order.placedAt).toLocaleDateString("es-CR", {
              day: "2-digit",
              month: "long",
              year: "numeric",
            })}
          </p>
        </div>
        <span className={`text-xs font-semibold rounded-full px-2.5 py-0.5 ${ORDER_STATUS_COLORS[order.status]}`}>
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
            <span>₡{(l.unitPriceCrc * l.quantity).toLocaleString("es-CR")}</span>
          </li>
        ))}
      </ul>

      {/* Footer */}
      <div className="flex items-center justify-between pt-1 border-t border-sand-100">
        <span className="text-xs text-copy-secondary">
          {order.fulfillmentType === "Delivery" ? "🚚 Entrega" : "🏪 Retiro en tienda"}
        </span>
        <span className="font-semibold text-ink-900 text-sm">₡{order.totalCrc.toLocaleString("es-CR")}</span>
      </div>

      {(order.status === "AwaitingPayment" || order.status === "PaymentReported") && (
        <div className="rounded-xl border border-warn-200 bg-warn-50 p-3 text-xs text-warn-900">
          <p className="font-semibold">Referencia SINPE: {order.paymentReference}</p>
          {order.stockReservationExpiresAt && (
            <p className="mt-1">
              La disponibilidad se reserva hasta {new Date(order.stockReservationExpiresAt).toLocaleString("es-CR")}.
            </p>
          )}
          {order.status === "AwaitingPayment" && (
            <button
              type="button"
              disabled={reportPayment.isPending}
              onClick={() =>
                reportPayment.mutate(order.id, {
                  onSuccess: () => toast.success("Pago reportado; la tienda debe verificar el abono"),
                  onError: () => toast.error("No se pudo reportar el pago"),
                })
              }
              className="mt-3 rounded-lg bg-warn-700 px-3 py-2 font-semibold text-white disabled:opacity-50"
            >
              Ya pagué por SINPE
            </button>
          )}
          {order.status === "PaymentReported" && (
            <p className="mt-2">Reporte recibido; queda pendiente de verificación de la tienda.</p>
          )}
        </div>
      )}

      {/* Progress bar (non-terminal) */}
      {!isTerminal && <ProgressBar status={order.status} fulfillment={order.fulfillmentType} />}
    </li>
  );
}

const STEPS_DELIVERY: StoreOrderStatus[] = [
  "AwaitingStoreAcceptance",
  "AwaitingPayment",
  "Paid",
  "Preparing",
  "OutForDelivery",
  "Delivered",
];
const STEPS_PICKUP: StoreOrderStatus[] = [
  "AwaitingStoreAcceptance",
  "AwaitingPayment",
  "Paid",
  "Preparing",
  "ReadyForPickup",
  "Delivered",
];

function ProgressBar({ status, fulfillment }: { status: StoreOrderStatus; fulfillment: string }) {
  const steps = fulfillment === "Delivery" ? STEPS_DELIVERY : STEPS_PICKUP;
  const progressStatus =
    status === "PaymentReported"
      ? "AwaitingPayment"
      : status === "Confirmed"
        ? "Paid"
        : status === "PendingPayment"
          ? "AwaitingStoreAcceptance"
          : status;
  const current = steps.indexOf(progressStatus);
  const pct = current < 0 ? 0 : Math.round((current / (steps.length - 1)) * 100);

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
            className={`text-[9px] leading-none ${i <= current ? "text-brand-600 font-semibold" : "text-copy-muted"}`}
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
  const { data: paged, isLoading } = useMyOrders(page);

  const orders = paged?.items ?? [];
  const active = orders.filter((o) => !TERMINAL.includes(o.status));
  const past = orders.filter((o) => TERMINAL.includes(o.status));
  const hasMore = paged?.hasNextPage ?? false;

  return (
    <>
      <Helmet>
        <title>Mis pedidos · PawTrack CR</title>
      </Helmet>

      <div className="mx-auto max-w-lg px-4 py-8 space-y-8">
        <h1 className="text-2xl font-bold text-ink-900">Mis pedidos</h1>

        {isLoading && (
          <div className="space-y-3">
            {Array.from({ length: 3 }).map((_, i) => (
              <Skeleton key={i} className="h-32 rounded-2xl" />
            ))}
          </div>
        )}

        {!isLoading && orders.length === 0 && (
          <div className="text-center py-16 text-copy-muted space-y-2">
            <p className="text-4xl">🛒</p>
            <p className="font-semibold text-copy-secondary">Aún no has hecho pedidos</p>
            <p className="text-sm">Explora las tiendas en el mapa y agrega productos a tu carrito.</p>
          </div>
        )}

        {active.length > 0 && (
          <section className="space-y-3">
            <h2 className="text-sm font-semibold text-copy-secondary uppercase tracking-wide">En curso</h2>
            <ul className="space-y-3">
              {active.map((o) => (
                <OrderRow key={o.id} order={o} />
              ))}
            </ul>
          </section>
        )}

        {past.length > 0 && (
          <section className="space-y-3">
            <h2 className="text-sm font-semibold text-copy-secondary uppercase tracking-wide">Historial</h2>
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
            <span className="text-xs text-copy-secondary">Página {page}</span>
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
