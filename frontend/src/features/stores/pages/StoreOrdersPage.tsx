import { useState } from "react";
import { Helmet } from "react-helmet-async";
import { toast } from "@/shared/lib/toast";
import { Skeleton } from "@/shared/ui/Spinner";
import { Button } from "@/shared/ui/Button";
import {
  useIncomingOrders,
  useConfirmOrder,
  useUpdateOrderStatus,
} from "../hooks/useStoreOrders";
import { ORDER_STATUS_COLORS, ORDER_STATUS_LABELS } from "../api/storesApi";
import type { StoreOrderDto, StoreOrderStatus } from "../api/storesApi";

const NEXT_STATUS_DELIVERY: Partial<
  Record<StoreOrderStatus, StoreOrderStatus>
> = {
  Confirmed: "Preparing",
  Preparing: "OutForDelivery",
  OutForDelivery: "Delivered",
};

const NEXT_STATUS_PICKUP: Partial<Record<StoreOrderStatus, StoreOrderStatus>> =
  {
    Confirmed: "Preparing",
    Preparing: "ReadyForPickup",
    ReadyForPickup: "Delivered",
  };

const CANCELLABLE: StoreOrderStatus[] = [
  "Confirmed",
  "Preparing",
  "ReadyForPickup",
  "OutForDelivery",
];

const REQUEST_STATUSES: StoreOrderStatus[] = [
  "PendingPayment",
  "PaymentReported",
];

function getNextStatus(order: StoreOrderDto): StoreOrderStatus | undefined {
  const map =
    order.fulfillmentType === "Delivery"
      ? NEXT_STATUS_DELIVERY
      : NEXT_STATUS_PICKUP;
  return map[order.status];
}

function OrderCard({ order }: { order: StoreOrderDto }) {
  const confirm = useConfirmOrder();
  const updateStatus = useUpdateOrderStatus();
  const [reason, setReason] = useState("");

  const nextStatus = getNextStatus(order);

  return (
    <li className="rounded-2xl border border-sand-100 bg-surface p-4 space-y-3">
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="font-semibold text-sand-900">
            Pedido #{order.id.slice(-6).toUpperCase()}
          </p>
          <p className="text-xs text-sand-500">
            {new Date(order.placedAt).toLocaleString("es-CR")} ·{" "}
            {order.fulfillmentType === "Pickup" ? "🏪 Retiro" : "🚚 Entrega"}
          </p>
        </div>
        <span
          className={`shrink-0 rounded-full px-2.5 py-0.5 text-[10px] font-bold ${ORDER_STATUS_COLORS[order.status]}`}
        >
          {ORDER_STATUS_LABELS[order.status]}
        </span>
      </div>

      <ul className="space-y-1">
        {order.items.map((item) => (
          <li key={item.id} className="flex justify-between text-sm">
            <span className="text-sand-700">
              {item.productName} × {item.quantity}
            </span>
            <span className="font-semibold text-sand-900">
              ₡{item.subtotalCrc.toLocaleString("es-CR")}
            </span>
          </li>
        ))}
        <li className="flex justify-between font-bold text-sm border-t border-sand-100 pt-1">
          <span>Total</span>
          <span className="text-rescue-700">
            ₡{order.totalCrc.toLocaleString("es-CR")}
          </span>
        </li>
      </ul>

      {REQUEST_STATUSES.includes(order.status) && (
        <p className="rounded-xl border border-warn-200 bg-warn-50 p-3 text-xs text-warn-800">
          Solicitud pendiente de revisión. Verifica disponibilidad y condiciones
          antes de confirmarla.
        </p>
      )}

      {order.deliveryAddress && (
        <p className="text-xs text-sand-600">📍 {order.deliveryAddress}</p>
      )}
      {order.customerNote && (
        <p className="text-xs text-sand-600">💬 "{order.customerNote}"</p>
      )}

      {(REQUEST_STATUSES.includes(order.status) ||
        CANCELLABLE.includes(order.status)) && (
        <label className="block text-xs font-medium text-sand-600">
          Motivo o nota para el cliente *
          <input
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            className="mt-1 w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm font-normal text-sand-800 outline-none focus:border-brand-400 focus:ring-2 focus:ring-brand-200"
            placeholder="Ej. producto no disponible o instrucciones de retiro"
            maxLength={500}
          />
        </label>
      )}

      {/* Actions */}
      <div className="flex gap-2">
        {REQUEST_STATUSES.includes(order.status) && (
          <Button
            size="sm"
            loading={confirm.isPending}
            onClick={() =>
              confirm.mutate(
                { orderId: order.id, note: reason.trim() || undefined },
                {
                  onSuccess: () => toast.success("Pedido confirmado"),
                  onError: () => toast.error("Error al confirmar"),
                },
              )
            }
          >
            ✓ Confirmar solicitud
          </Button>
        )}
        {nextStatus && !REQUEST_STATUSES.includes(order.status) && (
          <Button
            size="sm"
            variant="secondary"
            loading={updateStatus.isPending}
            onClick={() =>
              updateStatus.mutate(
                { orderId: order.id, status: nextStatus },
                {
                  onSuccess: () => toast.success("Estado actualizado"),
                  onError: () => toast.error("Error al actualizar"),
                },
              )
            }
          >
            → {ORDER_STATUS_LABELS[nextStatus]}
          </Button>
        )}
        {REQUEST_STATUSES.includes(order.status) && (
          <Button
            size="sm"
            variant="danger"
            loading={updateStatus.isPending}
            onClick={() => {
              if (!reason.trim()) {
                toast.error("Escribe un motivo para rechazar la solicitud.");
                return;
              }
              updateStatus.mutate(
                { orderId: order.id, status: "Rejected", note: reason.trim() },
                {
                  onSuccess: () => toast.success("Solicitud rechazada"),
                  onError: () => toast.error("Error al rechazar"),
                },
              );
            }}
          >
            Rechazar
          </Button>
        )}
        {CANCELLABLE.includes(order.status) && (
          <Button
            size="sm"
            variant="danger"
            loading={updateStatus.isPending}
            onClick={() => {
              if (!reason.trim()) {
                toast.error("Escribe un motivo para cancelar el pedido.");
                return;
              }
              updateStatus.mutate(
                { orderId: order.id, status: "Cancelled", note: reason.trim() },
                {
                  onSuccess: () => toast.success("Pedido cancelado"),
                  onError: () => toast.error("Error al cancelar"),
                },
              );
            }}
          >
            Cancelar
          </Button>
        )}
      </div>
    </li>
  );
}

export default function StoreOrdersPage() {
  const { data: orders = [], isLoading } = useIncomingOrders();
  const [filter, setFilter] = useState<"active" | "all">("active");

  const displayed =
    filter === "active"
      ? orders.filter(
          (o) => !["Delivered", "Cancelled", "Rejected"].includes(o.status),
        )
      : orders;

  return (
    <div className="mx-auto max-w-2xl px-4 py-8 space-y-5 animate-fade-in-up">
      <Helmet>
        <title>Pedidos — PawTrack CR</title>
      </Helmet>

      <div className="flex items-center justify-between">
        <h1 className="font-display text-xl font-bold text-sand-900">
          Pedidos
        </h1>
        <div className="flex gap-2">
          {(["active", "all"] as const).map((f) => (
            <button
              key={f}
              type="button"
              onClick={() => setFilter(f)}
              className={`rounded-xl px-3 py-1.5 text-xs font-semibold transition-colors ${filter === f ? "bg-brand-500 text-white" : "bg-sand-100 text-sand-600 hover:bg-sand-200"}`}
            >
              {f === "active" ? "Activos" : "Todos"}
            </button>
          ))}
        </div>
      </div>

      {isLoading && <Skeleton className="h-48 rounded-2xl" />}

      {!isLoading && displayed.length === 0 && (
        <p className="py-10 text-center text-sm text-sand-400">
          {filter === "active"
            ? "No hay pedidos activos."
            : "No hay pedidos aún."}
        </p>
      )}

      <ul className="space-y-4">
        {displayed.map((order) => (
          <OrderCard key={order.id} order={order} />
        ))}
      </ul>
    </div>
  );
}
