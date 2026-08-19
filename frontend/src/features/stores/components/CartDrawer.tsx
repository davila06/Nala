import { AnimatePresence, motion } from "framer-motion";
import { useCartStore } from "../store/cartStore";
import { Button } from "@/shared/ui/Button";

const MAX_QTY = 100;

interface CartDrawerProps {
  isOpen: boolean;
  onClose: () => void;
  onCheckout: () => void;
}

export function CartDrawer({ isOpen, onClose, onCheckout }: CartDrawerProps) {
  const { items, storeName, updateQty, totalCrc, clear } = useCartStore();

  return (
    <AnimatePresence>
      {isOpen && (
        <>
          <motion.div
            className="fixed inset-0 z-50 bg-black/40 backdrop-blur-sm"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={onClose}
            aria-hidden="true"
          />
          <motion.div
            role="dialog"
            aria-modal="true"
            aria-label="Carrito de compras"
            className="fixed inset-x-0 bottom-0 z-50 max-h-[90dvh] overflow-y-auto rounded-t-3xl bg-surface shadow-2xl"
            initial={{ y: "100%" }}
            animate={{ y: 0 }}
            exit={{ y: "100%" }}
            transition={{ type: "spring", damping: 30, stiffness: 300 }}
          >
            {/* Handle */}
            <div
              className="mx-auto mt-3 h-1 w-10 rounded-full bg-sand-200"
              aria-hidden="true"
            />

            {/* Header */}
            <div className="sticky top-0 z-10 flex items-center justify-between gap-3 border-b border-sand-100 bg-surface px-5 py-4">
              <h2 className="font-display text-base font-semibold text-sand-900">
                🛒 Carrito — {storeName}
              </h2>
              <button
                type="button"
                onClick={onClose}
                aria-label="Cerrar carrito"
                className="flex h-8 w-8 items-center justify-center rounded-full text-sand-400 hover:bg-sand-100"
              >
                <svg
                  viewBox="0 0 20 20"
                  fill="currentColor"
                  className="h-4 w-4"
                  aria-hidden="true"
                >
                  <path d="M6.28 5.22a.75.75 0 0 0-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 1 0 1.06 1.06L10 11.06l3.72 3.72a.75.75 0 1 0 1.06-1.06L11.06 10l3.72-3.72a.75.75 0 0 0-1.06-1.06L10 8.94 6.28 5.22Z" />
                </svg>
              </button>
            </div>

            <div className="px-5 py-4 space-y-4">
              {items.length === 0 ? (
                <p className="py-10 text-center text-sm text-sand-400">
                  El carrito está vacío.
                </p>
              ) : (
                <>
                  <ul className="space-y-3">
                    {items.map(({ product, quantity }) => (
                      <li key={product.id} className="flex items-center gap-3">
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-semibold text-sand-900 truncate">
                            {product.name}
                          </p>
                          <p className="text-xs text-sand-500">
                            ₡{product.priceCrc.toLocaleString("es-CR")} c/u
                          </p>
                        </div>
                        <div className="flex items-center gap-1 shrink-0">
                          <button
                            type="button"
                            onClick={() => updateQty(product.id, quantity - 1)}
                            className="flex h-7 w-7 items-center justify-center rounded-lg bg-sand-100 text-sand-700 hover:bg-sand-200 text-sm font-bold"
                          >
                            −
                          </button>
                          <span className="w-6 text-center text-sm font-semibold text-sand-900">
                            {quantity}
                          </span>
                          <button
                            type="button"
                            onClick={() => updateQty(product.id, quantity + 1)}
                            disabled={quantity >= MAX_QTY}
                            className="flex h-7 w-7 items-center justify-center rounded-lg bg-sand-100 text-sand-700 hover:bg-sand-200 text-sm font-bold disabled:opacity-40 disabled:cursor-not-allowed"
                          >
                            +
                          </button>
                        </div>
                        <span className="w-20 text-right text-sm font-bold text-rescue-700 shrink-0">
                          ₡
                          {(product.priceCrc * quantity).toLocaleString(
                            "es-CR",
                          )}
                        </span>
                      </li>
                    ))}
                  </ul>

                  {/* Total */}
                  <div className="flex items-center justify-between border-t border-sand-100 pt-3">
                    <span className="font-semibold text-sand-700">Total</span>
                    <span className="font-display text-xl font-black text-sand-900">
                      ₡{totalCrc().toLocaleString("es-CR")}
                    </span>
                  </div>

                  <Button fullWidth onClick={onCheckout}>
                    Confirmar pedido
                  </Button>
                  <button
                    type="button"
                    onClick={() => {
                      clear();
                      onClose();
                    }}
                    className="w-full text-xs text-sand-400 hover:text-danger-500 py-1"
                  >
                    Vaciar carrito
                  </button>
                </>
              )}
            </div>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  );
}
