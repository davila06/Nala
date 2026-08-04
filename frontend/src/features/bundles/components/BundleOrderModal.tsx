import { useState } from "react";
import { toast } from "@/shared/lib/toast";
import { Button, Input } from "@/shared/ui";
import {
  useCreateBundleOrder,
  useMyBundleOrders,
  useReportBundlePayment,
  useCancelBundleOrder,
} from "../hooks/useBundles";
import {
  COLLAR_MODEL_LABELS,
  STATUS_COLORS,
  BUNDLE_AMOUNT_CRC,
  type CollarModel,
  type BundleOrderDto,
} from "../api/bundleApi";

// ── CR cantons for shipping ───────────────────────────────────────────────────
const CANTONS = [
  "San José", "Escazú", "Desamparados", "Puriscal", "Tarrazú", "Aserrí",
  "Mora", "Goicoechea", "Santa Ana", "Alajuelita", "Vásquez de Coronado",
  "Acosta", "Tibás", "Moravia", "Montes de Oca", "Turrubares", "Dota",
  "Curridabat", "Pérez Zeledón", "León Cortés",
  "Alajuela", "San Ramón", "Grecia", "San Mateo", "Atenas", "Naranjo",
  "Palmares", "Poás", "Orotina", "San Carlos", "Zarcero", "Sarchí",
  "Upala", "Los Chiles", "Guatuso",
  "Cartago", "Paraíso", "La Unión", "Jiménez", "Turrialba", "Alvarado",
  "Oreamuno", "El Guarco",
  "Heredia", "Barva", "Santo Domingo", "Santa Bárbara", "San Rafael",
  "San Isidro", "Belén", "Flores", "San Pablo", "Sarapiquí",
  "Liberia", "Nicoya", "Santa Cruz", "Bagaces", "Carrillo", "Cañas",
  "Abangares", "Tilarán", "Nandayure", "La Cruz", "Hojancha",
  "Puntarenas", "Esparza", "Buenos Aires", "Montes de Oro", "Osa",
  "Quepos", "Golfito", "Coto Brus", "Parrita", "Corredores", "Garabito",
  "Limón", "Pococí", "Siquirres", "Talamanca", "Matina", "Guácimo",
];

// ── Order status card ─────────────────────────────────────────────────────────

function OrderCard({ order }: { order: BundleOrderDto }) {
  const reportPayment = useReportBundlePayment();
  const cancel = useCancelBundleOrder();
  const [showCancelConfirm, setShowCancelConfirm] = useState(false);

  const isCancellable = order.status === "PendingPayment";
  const isPendingPayment = order.status === "PendingPayment";

  const statusSteps: { key: BundleOrderDto["status"]; label: string }[] = [
    { key: "PendingPayment", label: "Pendiente" },
    { key: "Paid", label: "Confirmado" },
    { key: "Sourcing", label: "Adquiriendo" },
    { key: "Shipped", label: "En camino" },
    { key: "Delivered", label: "Entregado" },
  ];

  const activeIdx = statusSteps.findIndex((s) => s.key === order.status);

  return (
    <div className="rounded-2xl border border-sand-200 bg-surface-warm p-4 space-y-4">
      {/* Header */}
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="font-semibold text-sand-900">{order.collarModelLabel}</p>
          <p className="text-xs text-sand-500">
            Pedido #{order.id.slice(-8).toUpperCase()} ·{" "}
            {new Date(order.createdAt).toLocaleDateString("es-CR")}
          </p>
        </div>
        <span className={`rounded-full px-2.5 py-0.5 text-xs font-semibold ${STATUS_COLORS[order.status]}`}>
          {order.statusLabel}
        </span>
      </div>

      {/* Progress bar (non-cancelled) */}
      {order.status !== "Cancelled" && (
        <div className="flex items-center gap-1">
          {statusSteps.map((step, idx) => (
            <div key={step.key} className="flex-1 flex flex-col items-center gap-1">
              <div className={`h-1.5 w-full rounded-full transition-colors ${
                idx <= activeIdx ? "bg-brand-500" : "bg-sand-200"
              }`} />
              <span className="text-[10px] text-sand-400 hidden sm:block">{step.label}</span>
            </div>
          ))}
        </div>
      )}

      {/* SINPE payment block */}
      {isPendingPayment && (
        <div className="rounded-xl border border-warn-200 bg-warn-50 p-3 space-y-2">
          <p className="text-xs font-semibold text-warn-800">Realiza el pago SINPE</p>
          <div className="flex items-center gap-2">
            <span className="font-mono text-xl font-black tracking-widest text-sand-900">
              {order.paymentReference}
            </span>
            <button
              type="button"
              onClick={() => { void navigator.clipboard.writeText(order.paymentReference); toast.success("Referencia copiada"); }}
              className="rounded-lg bg-warn-200 px-2 py-1 text-xs font-semibold text-warn-800 hover:bg-warn-300"
            >
              Copiar
            </button>
          </div>
          <p className="text-xs text-warn-700">Monto: <strong>₡{order.amountCrc.toLocaleString("es-CR")}</strong></p>
          {!order.paymentReportedByUser ? (
            <Button
              size="sm"
              loading={reportPayment.isPending}
              onClick={() => {
                reportPayment.mutate(order.id, {
                  onSuccess: () => toast.success("Aviso de pago enviado"),
                  onError: () => toast.error("No se pudo registrar"),
                });
              }}
              className="w-full"
            >
              ✓ Ya realicé el pago SINPE
            </Button>
          ) : (
            <p className="text-xs text-trust-700 font-medium">
              ✅ Aviso de pago enviado — pendiente de verificación
            </p>
          )}
        </div>
      )}

      {/* Tracking */}
      {order.trackingNumber && (
        <div className="rounded-xl border border-rescue-200 bg-rescue-50 p-3">
          <p className="text-xs font-semibold text-rescue-800">🚚 Número de seguimiento</p>
          <p className="font-mono font-bold text-sand-900">{order.trackingNumber}</p>
        </div>
      )}

      {/* Shipping summary */}
      <div className="text-xs text-sand-500 space-y-0.5">
        <p>📍 {order.shippingAddress}, {order.shippingCanton}</p>
        <p>👤 {order.shippingFullName} · {order.shippingPhone}</p>
      </div>

      {/* Cancel */}
      {isCancellable && !showCancelConfirm && (
        <button
          type="button"
          onClick={() => setShowCancelConfirm(true)}
          className="text-xs text-danger-500 underline hover:text-danger-700"
        >
          Cancelar pedido
        </button>
      )}
      {showCancelConfirm && (
        <div className="rounded-xl border border-danger-200 bg-danger-50 p-3 space-y-2">
          <p className="text-xs font-semibold text-danger-700">¿Cancelar este pedido?</p>
          <div className="flex gap-2">
            <Button
              variant="danger"
              size="sm"
              loading={cancel.isPending}
              onClick={() => {
                cancel.mutate(order.id, {
                  onSuccess: () => toast.success("Pedido cancelado"),
                  onError: (err: unknown) =>
                    toast.error((err as { response?: { data?: { detail?: string } } })?.response?.data?.detail ?? "Error"),
                });
              }}
            >
              Sí, cancelar
            </Button>
            <Button variant="secondary" size="sm" onClick={() => setShowCancelConfirm(false)}>
              No, mantener
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}

// ── Create order form ─────────────────────────────────────────────────────────

function CreateOrderForm({ onSuccess }: { onSuccess: () => void }) {
  const create = useCreateBundleOrder();
  const [collarModel, setCollarModel] = useState<CollarModel>("TractiveGPSDog4");
  const [fullName, setFullName] = useState("");
  const [address, setAddress] = useState("");
  const [canton, setCanton] = useState("San José");
  const [phone, setPhone] = useState("");
  const [notes, setNotes] = useState("");

  const handleSubmit = () => {
    if (!fullName.trim() || !address.trim() || !phone.trim()) {
      toast.error("Completa todos los campos obligatorios");
      return;
    }
    create.mutate(
      { collarModel, shippingFullName: fullName.trim(), shippingAddress: address.trim(),
        shippingCanton: canton, shippingPhone: phone.trim(), deliveryNotes: notes.trim() || undefined },
      {
        onSuccess: () => { toast.success("¡Pedido creado! Revisa tu correo para las instrucciones de pago."); onSuccess(); },
        onError: (err: unknown) =>
          toast.error((err as { response?: { data?: { detail?: string } } })?.response?.data?.detail ?? "Error al crear el pedido"),
      },
    );
  };

  return (
    <div className="space-y-4">
      {/* Summary card */}
      <div className="rounded-2xl border border-brand-200 bg-brand-50 p-4 space-y-1">
        <p className="text-sm font-bold text-brand-800">📦 Bundle PawTrack GPS — Bajo demanda</p>
        <p className="text-xs text-brand-700">Collar GPS + 12 meses de PawTrack Plus</p>
        <p className="text-lg font-black text-brand-900">₡{BUNDLE_AMOUNT_CRC.toLocaleString("es-CR")}</p>
        <p className="text-xs text-brand-600 opacity-75">Pago único · Sin suscripción adicional a PawTrack</p>
      </div>

      {/* Collar model */}
      <div>
        <label className="mb-1 block text-xs font-medium text-sand-600">Modelo de collar *</label>
        <select
          value={collarModel}
          onChange={(e) => setCollarModel(e.target.value as CollarModel)}
          className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
        >
          {(Object.keys(COLLAR_MODEL_LABELS) as CollarModel[]).map((m) => (
            <option key={m} value={m}>{COLLAR_MODEL_LABELS[m]}</option>
          ))}
        </select>
      </div>

      {/* Shipping */}
      <div className="space-y-2">
        <p className="text-xs font-semibold uppercase tracking-wide text-sand-500">Datos de envío</p>
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">Nombre completo *</label>
          <Input value={fullName} onChange={(e) => setFullName(e.target.value)} placeholder="Juan Pérez García" />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">Dirección completa *</label>
          <Input value={address} onChange={(e) => setAddress(e.target.value)} placeholder="200m norte de la iglesia…" />
        </div>
        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className="mb-1 block text-xs font-medium text-sand-600">Cantón *</label>
            <select
              value={canton}
              onChange={(e) => setCanton(e.target.value)}
              className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
            >
              {CANTONS.map((c) => <option key={c} value={c}>{c}</option>)}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-sand-600">Teléfono *</label>
            <Input type="tel" value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="8888-8888" />
          </div>
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">Notas de entrega</label>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={2}
            className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm placeholder:text-sand-400 focus:outline-none focus:ring-2 focus:ring-brand-400"
            placeholder="Horario preferido, referencias del lugar, etc."
          />
        </div>
      </div>

      <div className="rounded-xl border border-sand-200 bg-sand-50 p-3 text-xs text-sand-600 space-y-1">
        <p className="font-semibold">¿Cómo funciona el pago?</p>
        <p>1. Recibirás una referencia SINPE Móvil en este pedido y por correo.</p>
        <p>2. Realiza la transferencia y marca el pago como hecho.</p>
        <p>3. Confirmamos el pago en 24-48 h y activamos tu plan Plus.</p>
        <p>4. Adquirimos y enviamos tu collar. Recibirás el número de seguimiento.</p>
      </div>

      <Button
        onClick={handleSubmit}
        loading={create.isPending}
        disabled={!fullName.trim() || !address.trim() || !phone.trim()}
        className="w-full"
      >
        Confirmar pedido — ₡{BUNDLE_AMOUNT_CRC.toLocaleString("es-CR")}
      </Button>
    </div>
  );
}

// ── Main component ────────────────────────────────────────────────────────────

export function BundleOrderModal({ onClose: _onClose }: { onClose: () => void }) {
  const { data: orders, isLoading } = useMyBundleOrders();
  const [showForm, setShowForm] = useState(false);

  const activeOrder = orders?.find(
    (o) => o.status !== "Cancelled" && o.status !== "Delivered",
  );
  const pastOrders = orders?.filter(
    (o) => o.status === "Cancelled" || o.status === "Delivered",
  ) ?? [];

  if (showForm) {
    return (
      <div className="space-y-4">
        <div className="flex items-center gap-2">
          <button type="button" onClick={() => setShowForm(false)} className="text-sm text-sand-500 hover:text-sand-700">
            ← Volver
          </button>
          <h2 className="font-display text-base font-semibold text-sand-900">Nuevo pedido de collar</h2>
        </div>
        <CreateOrderForm onSuccess={() => setShowForm(false)} />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="font-display text-base font-semibold text-sand-900">📦 Collar GPS + Plan Plus</h2>
        {!activeOrder && (
          <Button size="sm" onClick={() => setShowForm(true)}>+ Pedir collar</Button>
        )}
      </div>

      {isLoading && <div className="animate-pulse h-24 rounded-2xl bg-sand-100" />}

      {activeOrder && <OrderCard order={activeOrder} />}

      {!activeOrder && !isLoading && (
        <div className="rounded-2xl border border-dashed border-sand-200 bg-surface-warm p-6 text-center space-y-2">
          <p className="text-2xl">📡</p>
          <p className="text-sm font-semibold text-sand-700">
            Collar GPS + 12 meses Plus — ₡49,900
          </p>
          <p className="text-xs text-sand-500">
            Tractive GPS DOG 4 o CAT 4 importado a tu puerta. Pago único, sin contrato.
          </p>
          <Button onClick={() => setShowForm(true)} className="mt-2">
            Pedir mi collar ahora
          </Button>
        </div>
      )}

      {pastOrders.length > 0 && (
        <details>
          <summary className="cursor-pointer text-xs font-semibold text-sand-400 hover:text-sand-600">
            {pastOrders.length} pedido{pastOrders.length !== 1 ? "s" : ""} completado{pastOrders.length !== 1 ? "s" : ""}/cancelado{pastOrders.length !== 1 ? "s" : ""}
          </summary>
          <ul className="mt-2 space-y-2">
            {pastOrders.map((o) => <OrderCard key={o.id} order={o} />)}
          </ul>
        </details>
      )}
    </div>
  );
}
