import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { storesApi } from "../api/storesApi";

// pageSize=500 for the map (all pins visible); default 50 for directory use
export function usePublicStores(enabled = true, pageSize = 50) {
  return useQuery({
    queryKey: ["stores", "public", pageSize],
    queryFn: () => storesApi.getAll(pageSize),
    staleTime: 5 * 60_000,
    enabled,
  });
}

export function useStoreDetail(id: string, enabled = true) {
  return useQuery({
    queryKey: ["stores", id],
    queryFn: () => storesApi.getDetail(id),
    enabled: !!id && enabled,
    staleTime: 2 * 60_000,
  });
}

export function useMyStore() {
  return useQuery({
    queryKey: ["my-store"],
    queryFn: storesApi.getMine,
    retry: false,
  });
}

export function useMyStoreProducts() {
  return useQuery({
    queryKey: ["my-store-products"],
    queryFn: storesApi.getProducts,
    staleTime: 60_000,
  });
}

export function useUpdateStoreProfile() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: storesApi.updateProfile,
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["my-store"] }),
  });
}

export function useAddProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: storesApi.addProduct,
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["my-store-products"] }),
  });
}

export function useUpdateProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      ...data
    }: {
      id: string;
      name: string;
      description?: string;
      category: string;
      priceCrc: number;
      isAvailable: boolean;
    }) => storesApi.updateProduct(id, data),
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["my-store-products"] }),
  });
}

export function useDeleteProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: storesApi.deleteProduct,
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["my-store-products"] }),
  });
}

export function useRegisterStore() {
  return useMutation({ mutationFn: storesApi.register });
}

export function useUploadProductImage() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ productId, file }: { productId: string; file: File }) =>
      storesApi.uploadProductImage(productId, file),
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["my-store-products"] }),
  });
}

// ── Analytics (StorePlus+) ────────────────────────────────────────────────────

export function useStoreAnalytics(year?: number, month?: number) {
  return useQuery({
    queryKey: ["my-store-analytics", year, month],
    queryFn: () => storesApi.getAnalytics(year, month),
    retry: false,
    staleTime: 5 * 60_000,
  });
}

// ── Locations / sedes (StorePartner) ─────────────────────────────────────────

export function useStoreLocations() {
  return useQuery({
    queryKey: ["my-store-locations"],
    queryFn: storesApi.getLocations,
    retry: false,
    staleTime: 2 * 60_000,
  });
}

export function useCreateStoreLocation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: storesApi.createLocation,
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["my-store-locations"] }),
  });
}

export function useUpdateStoreLocation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      ...data
    }: {
      id: string;
      name: string;
      address: string;
      lat: number;
      lng: number;
      phoneNumber?: string;
    }) => storesApi.updateLocation(id, data),
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["my-store-locations"] }),
  });
}

export function useSetLocationActive() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, active }: { id: string; active: boolean }) =>
      storesApi.setLocationActive(id, active),
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["my-store-locations"] }),
  });
}
