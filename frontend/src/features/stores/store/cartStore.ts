import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { StoreProductDto } from "../api/storesApi";

export interface CartItem {
  product: StoreProductDto;
  quantity: number;
}

interface CartState {
  storeId: string | null;
  storeName: string;
  items: CartItem[];
  addItem: (
    storeId: string,
    storeName: string,
    product: StoreProductDto,
    qty?: number,
  ) => void;
  removeItem: (productId: string) => void;
  updateQty: (productId: string, qty: number) => void;
  clear: () => void;
  totalCrc: () => number;
  totalItems: () => number;
}

export const useCartStore = create<CartState>()(
  persist(
    (set, get) => ({
      storeId: null,
      storeName: "",
      items: [],

      addItem: (storeId, storeName, product, qty = 1) =>
        set((state) => {
          // If adding from a different store, clear cart first
          if (state.storeId && state.storeId !== storeId) {
            return { storeId, storeName, items: [{ product, quantity: qty }] };
          }
          const existing = state.items.find((i) => i.product.id === product.id);
          if (existing) {
            return {
              items: state.items.map((i) =>
                i.product.id === product.id
                  ? { ...i, quantity: i.quantity + qty }
                  : i,
              ),
            };
          }
          return {
            storeId,
            storeName,
            items: [...state.items, { product, quantity: qty }],
          };
        }),

      removeItem: (productId) =>
        set((state) => {
          const remaining = state.items.filter((i) => i.product.id !== productId);
          return {
            items: remaining,
            storeId: remaining.length === 0 ? null : state.storeId,
            storeName: remaining.length === 0 ? "" : state.storeName,
          };
        }),

      updateQty: (productId, qty) =>
        set((state) => ({
          items:
            qty <= 0
              ? state.items.filter((i) => i.product.id !== productId)
              : state.items.map((i) =>
                  i.product.id === productId ? { ...i, quantity: qty } : i,
                ),
        })),

      clear: () => set({ storeId: null, storeName: "", items: [] }),

      totalCrc: () =>
        get().items.reduce((s, i) => s + i.product.priceCrc * i.quantity, 0),

      totalItems: () => get().items.reduce((s, i) => s + i.quantity, 0),
    }),
    { name: "pawtrack-cart" },
  ),
);
