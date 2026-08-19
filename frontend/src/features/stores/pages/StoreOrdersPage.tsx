import { useState } from "react";
import { Helmet } from "react-helmet-async";
import { toast } from "@/shared/lib/toast";
import { Skeleton } from "@/shared/ui/Spinner";
import { Button } from "@/shared/ui/Button";
import { useIncomingOrders, useConfirmOrder, useUpdateOrderStatus } from "../hooks/useStoreOrders";
import { ORDER_STATUS_COLORS, ORDER_STATUS_LABELS } from "../api/storesApi";
import type { StoreOrderDto, StoreOrderStatus } from "../api/storesApi";

const NEXT_STATUS: Partial<Record<StoreOrderStatus, StoreOrderStatus>> = {
  Confirmed:    "Preparing",
  Preparing:    "ReadyForPickup",
  ReadyForPickup: "Delivered",
  OutForDelivery: "Delivered",
};

function OrderCard({ order }: { order: StoreOrderDto }) {
  const confirm     = useConfirmOrder();
  const updateStatus = useUpdateOrderStatus();

  const nextStatus = NEXT_STATUS[order.status];

  return (
    <li className="rounded-2xl border border-sand-100 bg-surface p-4 space-y-3">
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="font-semibold text-sand-900">Pedido #{order.id.slice(-6).toUpperCase()}</p>
          <p className="text-xs text-sand-500">
            {new Date(order.placedAt).toLocaleString("es-CR")} ·{" "}
            {order.fulfillmentType === "Pickup" ? "🏪 Retiro" : "🚚 Entrega"}
          </p>
        </div>
        <span className={`shrink-0 rounded-full px-2.5 py-0.5 text-[10px] font-bold ${ORDER_STATUS_COLORS[order.status]}`}>
          {ORDER_STATUS_LABELS[order.status]}
        </span>
      </div>

      <ul className="space-y-1">
        {order.items.map((item) => (
          <li key={item.id} className="flex justify-between text-sm">
            <span className="text-sand-700">{item.productName} × {item.quantity}</span>
            <span className="font-semibold text-sand-900">₡{item.subtotalCrc.toLocaleString("es-CR")}</span>
          </li>
        ))}
        <li className="flex justify-between font-bold text-sm border-t border-sand-100 pt-1">
          <span>Total</span>
          <span className="text-rescue-700">₡{order.totalCrc.toLocaleString("es-CR")}</span>
        </li>
      </ul>

      <div className="rounded-xl border border-warn-200 bg-warn-50 p-3 space-y-1">
        <p className="text-xs font-bold text-warn-800">
          SINPE: <span className="font-mono">{order.paymentReference}</span>
        </p>
        <p className={`text-xs font-semibold ${order.paymentReportedByCustomer ? "text-rescue-700" : "text-sand-500"}`}>
          {order.paymentReportedByCustomer ? "✓ Pago reportado por el cliente" : "⏳ Esperando reporte de pago"}
        </p>
      </div>

      {order.deliveryAddress && (
        <p className="text-xs text-sand-600">📍 {order.deliveryAddress}</p>
      )}
      {order.customerNote && (
        <p className="text-xs text-sand-600">💬 "{order.customerNote}"</p>
      )}

      {/* Actions */}
      <div className="flex gap-2">
        {order.status === "PaymentReported" && (
          <Button size="sm" loading={confirm.isPending}
            onClick={() => confirm.mutate({ orderId: order.id }, {
              onSuccess: () => toast.success("Pedido confirmado"),
              onError: () => toast.error("Error al confirmar"),
            })}>
            ✓ Confirmar pago
          </Button>
        )}
        {nextStatus && order.status !== "PaymentReported" && (
          <Button size="sm" variant="secondary" loading={updateStatus.isPending}
            onClick={() => updateStatus.mutate({ orderId: order.id, status: nextStatus }, {
              onSuccess: () => toast.success("Estado actualizado"),
              onError: () => toast.error("Error al actualizar"),
            })}>
            → {ORDER_STATUS_LABELS[nextStatus]}
          </Button>
        )}
        {!["Delivered", "Cancelled"].includes(order.status) && (
          <Button size="sm" variant="danger" loading={updateStatus.isPending}
            onClick={() => updateStatus.mutate({ orderId: order.id, status: "Cancelled" }, {
              onSuccess: () => toast.success("Pedido cancelado"),
              onError: () => toast.error("Error al cancelar"),
            })}>
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

  const displayed = filter === "active"
    ? orders.filter((o) => !["Delivered", "Cancelled"].includes(o.status))
    : orders;

  return (
    <div className="mx-auto max-w-2xl px-4 py-8 space-y-5 animate-fade-in-up">
      <Helmet><title>Pedidos — PawTrack CR</title></Helmet>

      <div className="flex items-center justify-between">
        <h1 className="font-display text-xl font-bold text-sand-900">Pedidos</h1>
        <div className="flex gap-2">
          {(["active", "all"] as const).map((f) => (
            <button key={f} type="button" onClick={() => setFilter(f)}
              className={`rounded-xl px-3 py-1.5 text-xs font-semibold transition-colors ${filter === f ? "bg-brand-500 text-white" : "bg-sand-100 text-sand-600 hover:bg-sand-200"}`}>
              {f === "active" ? "Activos" : "Todos"}
            </button>
          ))}
        </div>
      </div>

      {isLoading && <Skeleton className="h-48 rounded-2xl" />}

      {!isLoading && displayed.length === 0 && (
        <p className="py-10 text-center text-sm text-sand-400">
          {filter === "active" ? "No hay pedidos activos." : "No hay pedidos aún."}
        </p>
      )}

      <ul className="space-y-4">
        {displayed.map((order) => <OrderCard key={order.id} order={order} />)}
      </ul>
    </div>
  );
}
