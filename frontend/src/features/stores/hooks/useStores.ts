import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { storesApi } from "../api/storesApi";

export function usePublicStores(enabled = true) {
  return useQuery({
    queryKey: ["stores", "public"],
    queryFn: storesApi.getAll,
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
