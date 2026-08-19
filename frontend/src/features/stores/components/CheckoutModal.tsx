import { useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { Button, Input } from "@/shared/ui";
import { toast } from "@/shared/lib/toast";
import { useCartStore } from "../store/cartStore";
import { usePlaceOrder, useReportPayment } from "../hooks/useStoreOrders";
import type { StoreOrderDto } from "../api/storesApi";

interface CheckoutModalProps {
  isOpen: boolean;
  onClose: () => void;
}

type Step = "form" | "payment" | "done";

export function CheckoutModal({ isOpen, onClose }: CheckoutModalProps) {
  const { items, storeId, totalCrc, clear } = useCartStore();
  const placeOrder = usePlaceOrder();
  const reportPayment = useReportPayment();
  const [step, setStep] = useState<Step>("form");
  const [order, setOrder] = useState<StoreOrderDto | null>(null);
  const [fulfillment, setFulfillment] = useState<"Pickup" | "Delivery">(
    "Pickup",
  );
  const [deliveryAddress, setDeliveryAddress] = useState("");
  const [note, setNote] = useState("");

  const handlePlaceOrder = () => {
    if (!storeId) return;
    if (fulfillment === "Delivery" && !deliveryAddress.trim()) {
      toast.error("Ingresa la dirección de entrega.");
      return;
    }
    placeOrder.mutate(
      {
        storeId,
        fulfillmentType: fulfillment,
        deliveryAddress:
          fulfillment === "Delivery" ? deliveryAddress.trim() : undefined,
        customerNote: note.trim() || undefined,
        lines: items.map((i) => ({
          productId: i.product.id,
          quantity: i.quantity,
        })),
      },
      {
        onSuccess: (data) => {
          setOrder(data);
          setStep("payment");
          clear();
        },
        onError: () =>
          toast.error("No se pudo crear el pedido. Intenta de nuevo."),
      },
    );
  };

  const handleReportPayment = () => {
    if (!order) return;
    reportPayment.mutate(order.id, {
      onSuccess: () => setStep("done"),
      onError: () => toast.error("Error al reportar el pago."),
    });
  };

  const handleClose = () => {
    // Warn if user tries to close while the payment reference is on screen
    if (step === "payment") {
      toast.error(
        "Guarda la referencia antes de cerrar",
        `SINPE: ${order?.paymentReference ?? ""}`,
      );
      return;
    }
    setStep("form");
    setOrder(null);
    setFulfillment("Pickup");
    setDeliveryAddress("");
    setNote("");
    onClose();
  };

  return (
    <AnimatePresence>
      {isOpen && (
        <motion.div
          className="fixed inset-0 z-[60] flex items-end bg-black/50 sm:items-center sm:justify-center p-4"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          onClick={(e) => {
            if (e.target === e.currentTarget) handleClose();
          }}
        >
          <motion.div
            className="w-full max-w-md rounded-3xl bg-surface shadow-2xl max-h-[92vh] overflow-y-auto"
            initial={{ y: 48, opacity: 0 }}
            animate={{ y: 0, opacity: 1 }}
            exit={{ y: 48, opacity: 0 }}
            transition={{ type: "spring", stiffness: 380, damping: 34 }}
          >
            {/* Header */}
            <div className="flex items-center justify-between px-5 py-4 border-b border-sand-100">
              <h2 className="font-display text-base font-semibold text-sand-900">
                {step === "form"
                  ? "Confirmar pedido"
                  : step === "payment"
                    ? "Realizar pago SINPE"
                    : "¡Pedido enviado!"}
              </h2>
              <button
                type="button"
                onClick={handleClose}
                aria-label="Cerrar"
                className="flex h-8 w-8 items-center justify-center rounded-full text-sand-400 hover:bg-sand-100"
              >
                <svg
                  viewBox="0 0 16 16"
                  fill="currentColor"
                  className="h-4 w-4"
                  aria-hidden="true"
                >
                  <path d="M3.72 3.72a.75.75 0 0 1 1.06 0L8 6.94l3.22-3.22a.75.75 0 1 1 1.06 1.06L9.06 8l3.22 3.22a.75.75 0 1 1-1.06 1.06L8 9.06l-3.22 3.22a.75.75 0 0 1-1.06-1.06L6.94 8 3.72 4.78a.75.75 0 0 1 0-1.06Z" />
                </svg>
              </button>
            </div>

            <div className="px-5 py-5 space-y-4">
              {/* Step 1: Form */}
              {step === "form" && (
                <>
                  {/* Order summary */}
                  <div className="rounded-xl border border-sand-100 bg-sand-50 p-4 space-y-2">
                    {items.map((i) => (
                      <div
                        key={i.product.id}
                        className="flex items-center justify-between text-sm"
                      >
                        <span className="text-sand-700">
                          {i.product.name} × {i.quantity}
                        </span>
                        <span className="font-semibold text-sand-900">
                          ₡
                          {(i.product.priceCrc * i.quantity).toLocaleString(
                            "es-CR",
                          )}
                        </span>
                      </div>
                    ))}
                    <div className="flex items-center justify-between font-bold border-t border-sand-200 pt-2 text-sm">
                      <span>Total</span>
                      <span className="text-rescue-700">
                        ₡{totalCrc().toLocaleString("es-CR")}
                      </span>
                    </div>
                  </div>

                  {/* Fulfillment type */}
                  <div>
                    <p className="mb-2 text-xs font-medium text-sand-600">
                      Tipo de entrega
                    </p>
                    <div className="grid grid-cols-2 gap-2">
                      {(["Pickup", "Delivery"] as const).map((f) => (
                        <button
                          key={f}
                          type="button"
                          onClick={() => setFulfillment(f)}
                          className={`rounded-xl border-2 py-3 text-sm font-semibold transition-all ${fulfillment === f ? "border-brand-500 bg-brand-50 text-brand-800" : "border-sand-200 bg-white text-sand-700 hover:border-sand-300"}`}
                        >
                          {f === "Pickup"
                            ? "🏪 Retiro en tienda"
                            : "🚚 Entrega a domicilio"}
                        </button>
                      ))}
                    </div>
                  </div>

                  {fulfillment === "Delivery" && (
                    <div>
                      <label className="mb-1 block text-xs font-medium text-sand-600">
                        Dirección de entrega *
                      </label>
                      <Input
                        value={deliveryAddress}
                        onChange={(e) => setDeliveryAddress(e.target.value)}
                        placeholder="200m norte del parque..."
                      />
                    </div>
                  )}

                  <div>
                    <label className="mb-1 block text-xs font-medium text-sand-600">
                      Nota para la tienda (opcional)
                    </label>
                    <Input
                      value={note}
                      onChange={(e) => setNote(e.target.value)}
                      placeholder="Sin gluten, alérgico a..."
                    />
                  </div>

                  <Button
                    fullWidth
                    onClick={handlePlaceOrder}
                    loading={placeOrder.isPending}
                  >
                    Hacer pedido
                  </Button>
                </>
              )}

              {/* Step 2: SINPE payment */}
              {step === "payment" && order && (
                <div className="space-y-4">
                  <div className="rounded-2xl border border-warn-200 bg-warn-50 p-5 space-y-3 text-center">
                    <p className="text-xs font-semibold uppercase tracking-wider text-warn-600">
                      Referencia SINPE Móvil
                    </p>
                    <p className="font-mono text-3xl font-black tracking-widest text-sand-900">
                      {order.paymentReference}
                    </p>
                    <button
                      type="button"
                      onClick={() => {
                        navigator.clipboard.writeText(order.paymentReference);
                        toast.success("Referencia copiada");
                      }}
                      className="rounded-xl bg-warn-200 px-4 py-1.5 text-xs font-semibold text-warn-800 hover:bg-warn-300"
                    >
                      Copiar referencia
                    </button>
                  </div>
                  <p className="text-center text-sm text-sand-500">
                    Realiza el SINPE Móvil y toca <strong>"Ya pagué"</strong>{" "}
                    cuando termines. La tienda confirmará tu pedido en breve.
                  </p>
                  <p className="text-center font-bold text-sand-700">
                    Monto: ₡{order.totalCrc.toLocaleString("es-CR")}
                  </p>
                  <Button
                    fullWidth
                    onClick={handleReportPayment}
                    loading={reportPayment.isPending}
                  >
                    ✓ Ya realicé el pago
                  </Button>
                </div>
              )}

              {/* Step 3: Done */}
              {step === "done" && (
                <div className="space-y-4 text-center py-4">
                  <p className="text-5xl" aria-hidden="true">
                    🎉
                  </p>
                  <h3 className="font-display text-xl font-bold text-sand-900">
                    ¡Pago reportado!
                  </h3>
                  <p className="text-sm text-sand-500">
                    La tienda verificará tu pago y confirmará el pedido.
                    Recibirás una notificación.
                  </p>
                  <Button fullWidth variant="secondary" onClick={handleClose}>
                    Cerrar
                  </Button>
                </div>
              )}
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
