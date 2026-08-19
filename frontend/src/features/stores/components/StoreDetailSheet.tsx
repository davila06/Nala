import { useState } from "react";
import { Drawer } from "@/shared/ui/Drawer";
import { Button } from "@/shared/ui/Button";
import { Modal } from "@/shared/ui/Modal";
import { toast } from "@/shared/lib/toast";
import { useStoreDetail } from "../hooks/useStores";
import { useCartStore } from "../store/cartStore";
import { CATEGORY_LABELS } from "../api/storesApi";

interface StoreDetailSheetProps {
  storeId: string;
  isOpen: boolean;
  onClose: () => void;
  onCheckout: () => void;
}

export function StoreDetailSheet({
  storeId,
  isOpen,
  onClose,
  onCheckout,
}: StoreDetailSheetProps) {
  const { data, isLoading } = useStoreDetail(storeId);
  const { addItem, totalItems, storeId: cartStoreId } = useCartStore();
  const [addedIds, setAddedIds] = useState<Set<string>>(new Set());
  const [conflictProduct, setConflictProduct] = useState<
    NonNullable<typeof data>["products"][number] | null
  >(null);

  if (!isOpen) return null;

  const confirmAddFromNewStore = () => {
    if (!conflictProduct) return;
    addItem(storeId, data?.store.name ?? "", conflictProduct);
    setAddedIds((prev) => new Set([...prev, conflictProduct.id]));
    toast.success(`${conflictProduct.name} agregado al carrito`);
    setConflictProduct(null);
  };

  const handleAdd = (product: NonNullable<typeof data>["products"][number]) => {
    if (cartStoreId && cartStoreId !== storeId) {
      setConflictProduct(product);
      return;
    }
    addItem(storeId, data?.store.name ?? "", product);
    setAddedIds((prev) => new Set([...prev, product.id]));
    toast.success(`${product.name} agregado al carrito`);
  };

  const cartCount = totalItems();

  return (
    <>
    <Drawer
      isOpen={isOpen}
      onClose={onClose}
      title={data?.store.name ?? "Tienda"}
      side="bottom"
      maxWidth={540}
    >
      <div className="space-y-4 pb-safe">
        {isLoading && (
          <div className="space-y-3 animate-pulse">
            {[...Array(3)].map((_, i) => (
              <div key={i} className="h-16 rounded-xl bg-sand-100" />
            ))}
          </div>
        )}

        {data && (
          <>
            {/* Store info */}
            <div className="text-sm text-sand-500 space-y-0.5">
              <p>📍 {data.store.address}</p>
              {data.store.phoneNumber && <p>📞 {data.store.phoneNumber}</p>}
            </div>

            {/* Products grouped by category */}
            {data.products.length === 0 ? (
              <p className="py-8 text-center text-sm text-sand-400">
                Esta tienda no tiene productos disponibles aún.
              </p>
            ) : (
              (() => {
                const grouped = data.products.reduce<
                  Record<string, typeof data.products>
                >((acc, p) => {
                  (acc[p.category] ??= []).push(p);
                  return acc;
                }, {});
                return Object.entries(grouped).map(([cat, prods]) => (
                  <div key={cat}>
                    <p className="text-xs font-semibold uppercase tracking-wide text-sand-400 mb-2">
                      {CATEGORY_LABELS[cat as keyof typeof CATEGORY_LABELS] ??
                        cat}
                    </p>
                    <ul className="space-y-2">
                      {prods.map((product) => (
                        <li
                          key={product.id}
                          className="flex items-center gap-3 rounded-xl border border-sand-100 bg-surface p-3"
                        >
                          {product.imageUrl && (
                            <img
                              src={product.imageUrl}
                              alt={product.name}
                              className="h-12 w-12 rounded-lg object-cover border border-sand-100 shrink-0"
                            />
                          )}
                          <div className="flex-1 min-w-0">
                            <p className="text-sm font-semibold text-sand-900">
                              {product.name}
                            </p>
                            {product.description && (
                              <p className="text-xs text-sand-500 truncate">
                                {product.description}
                              </p>
                            )}
                            <p className="text-sm font-bold text-rescue-700 mt-0.5">
                              ₡{product.priceCrc.toLocaleString("es-CR")}
                            </p>
                          </div>
                          <button
                            type="button"
                            onClick={() => handleAdd(product)}
                            className={`shrink-0 rounded-xl px-3 py-2 text-xs font-bold transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400 ${
                              addedIds.has(product.id)
                                ? "bg-rescue-100 text-rescue-700"
                                : "bg-rescue-600 text-white hover:bg-rescue-700"
                            }`}
                          >
                            {addedIds.has(product.id) ? "✓" : "+ Agregar"}
                          </button>
                        </li>
                      ))}
                    </ul>
                  </div>
                ));
              })()
            )}

            {/* Checkout CTA */}
            {cartCount > 0 && (
              <div className="sticky bottom-0 bg-surface pt-3 pb-1">
                <Button
                  fullWidth
                  onClick={() => {
                    onClose();
                    onCheckout();
                  }}
                >
                  Ver carrito ({cartCount}{" "}
                  {cartCount === 1 ? "producto" : "productos"})
                </Button>
              </div>
            )}
          </>
        )}
      </div>
    </Drawer>

    {/* Multi-store conflict confirmation */}
    <Modal
      isOpen={!!conflictProduct}
      onClose={() => setConflictProduct(null)}
      title="¿Vaciar carrito?"
    >
      <p className="text-sand-700 text-sm mb-5">
        Tu carrito tiene productos de otra tienda. Si agregas este producto, el
        carrito anterior se vaciará.
      </p>
      <div className="flex gap-3 justify-end">
        <Button variant="ghost" onClick={() => setConflictProduct(null)}>
          Cancelar
        </Button>
        <Button onClick={confirmAddFromNewStore}>Sí, vaciar y agregar</Button>
      </div>
    </Modal>
    </>
  );
}
