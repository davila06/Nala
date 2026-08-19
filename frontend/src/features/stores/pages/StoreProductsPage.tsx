import { useRef, useState } from "react";
import { Helmet } from "react-helmet-async";
import { Button, Input } from "@/shared/ui";
import { toast } from "@/shared/lib/toast";
import { Skeleton } from "@/shared/ui/Spinner";
import {
  useMyStoreProducts,
  useAddProduct,
  useUpdateProduct,
  useDeleteProduct,
  useUploadProductImage,
} from "../hooks/useStores";
import { CATEGORY_LABELS } from "../api/storesApi";
import type { StoreProductDto, ProductCategory } from "../api/storesApi";

const ACCEPTED = "image/jpeg,image/png,image/webp";
const MAX_BYTES = 5 * 1024 * 1024; // 5 MB

const CATEGORIES = Object.keys(CATEGORY_LABELS) as ProductCategory[];

function ProductForm({
  initial,
  onSave,
  onCancel,
  loading,
}: {
  initial?: StoreProductDto;
  onSave: (data: {
    name: string;
    description?: string;
    category: string;
    priceCrc: number;
    isAvailable: boolean;
  }) => void;
  onCancel: () => void;
  loading: boolean;
}) {
  const [name, setName] = useState(initial?.name ?? "");
  const [description, setDescription] = useState(initial?.description ?? "");
  const [category, setCategory] = useState<ProductCategory>(
    initial?.category ?? "Other",
  );
  const [priceCrc, setPriceCrc] = useState(String(initial?.priceCrc ?? ""));
  const [isAvailable, setIsAvailable] = useState(initial?.isAvailable ?? true);

  const handleSubmit = () => {
    if (!name.trim() || !priceCrc) {
      toast.error("Nombre y precio son requeridos.");
      return;
    }
    onSave({
      name: name.trim(),
      description: description.trim() || undefined,
      category,
      priceCrc: Number(priceCrc),
      isAvailable,
    });
  };

  return (
    <div className="rounded-2xl border border-brand-200 bg-brand-50 p-4 space-y-3">
      <h3 className="text-sm font-semibold text-brand-800">
        {initial ? "Editar producto" : "Nuevo producto"}
      </h3>
      <div className="grid grid-cols-2 gap-3">
        <div className="col-span-2">
          <label className="mb-1 block text-xs font-medium text-sand-600">
            Nombre *
          </label>
          <Input
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Alimento Premium 3kg"
          />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">
            Categoría *
          </label>
          <select
            value={category}
            onChange={(e) => setCategory(e.target.value as ProductCategory)}
            className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
          >
            {CATEGORIES.map((c) => (
              <option key={c} value={c}>
                {CATEGORY_LABELS[c]}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">
            Precio ₡ *
          </label>
          <Input
            type="number"
            value={priceCrc}
            onChange={(e) => setPriceCrc(e.target.value)}
            placeholder="3500"
            min="0"
          />
        </div>
        <div className="col-span-2">
          <label className="mb-1 block text-xs font-medium text-sand-600">
            Descripción
          </label>
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={2}
            className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm placeholder:text-sand-400 focus:outline-none focus:ring-2 focus:ring-brand-400"
          />
        </div>
        {initial && (
          <div className="col-span-2 flex items-center gap-2">
            <input
              type="checkbox"
              id="avail"
              checked={isAvailable}
              onChange={(e) => setIsAvailable(e.target.checked)}
              className="h-4 w-4"
            />
            <label htmlFor="avail" className="text-sm text-sand-700">
              Disponible para pedidos
            </label>
          </div>
        )}
      </div>
      <div className="flex gap-2">
        <Button onClick={handleSubmit} loading={loading} size="sm">
          Guardar
        </Button>
        <Button variant="secondary" onClick={onCancel} size="sm">
          Cancelar
        </Button>
      </div>
    </div>
  );
}

export default function StoreProductsPage() {
  const { data: products, isLoading } = useMyStoreProducts();
  const add = useAddProduct();
  const update = useUpdateProduct();
  const del = useDeleteProduct();
  const uploadImage = useUploadProductImage();
  const [showForm, setShowForm] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [uploadingId, setUploadingId] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const pendingProductId = useRef<string | null>(null);

  const handleImageClick = (productId: string) => {
    pendingProductId.current = productId;
    fileInputRef.current?.click();
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    const productId = pendingProductId.current;
    e.target.value = "";
    if (!file || !productId) return;
    if (!file.type.startsWith("image/")) {
      toast.error("Solo se aceptan imágenes JPEG, PNG o WebP.");
      return;
    }
    if (file.size > MAX_BYTES) {
      toast.error("La imagen no puede superar 5 MB.");
      return;
    }
    setUploadingId(productId);
    uploadImage.mutate(
      { productId, file },
      {
        onSuccess: () => {
          toast.success("Imagen actualizada");
          setUploadingId(null);
        },
        onError: () => {
          toast.error("No se pudo subir la imagen.");
          setUploadingId(null);
        },
      },
    );
  };

  if (isLoading)
    return (
      <div className="mx-auto max-w-2xl px-4 py-10">
        <Skeleton className="h-48 rounded-2xl" />
      </div>
    );

  return (
    <div className="mx-auto max-w-2xl px-4 py-8 space-y-5 animate-fade-in-up">
      <Helmet>
        <title>Mis productos — PawTrack CR</title>
      </Helmet>

      <div className="flex items-center justify-between">
        <h1 className="font-display text-xl font-bold text-sand-900">
          Mis productos
        </h1>
        {!showForm && !editId && (
          <Button size="sm" onClick={() => setShowForm(true)}>
            + Agregar
          </Button>
        )}
      </div>

      {showForm && (
        <ProductForm
          loading={add.isPending}
          onCancel={() => setShowForm(false)}
          onSave={(data) =>
            add.mutate(data, {
              onSuccess: () => {
                toast.success("Producto agregado");
                setShowForm(false);
              },
              onError: () => toast.error("No se pudo guardar"),
            })
          }
        />
      )}

      {(products ?? []).length === 0 && !showForm ? (
        <p className="py-10 text-center text-sm text-sand-400">
          No tienes productos aún. Agrega el primero.
        </p>
      ) : (
        <ul className="space-y-3">
          {(products ?? []).map((product) => (
            <li
              key={product.id}
              className="rounded-xl border border-sand-100 bg-surface"
            >
              {editId === product.id ? (
                <div className="p-4">
                  <ProductForm
                    initial={product}
                    loading={update.isPending}
                    onCancel={() => setEditId(null)}
                    onSave={(data) =>
                      update.mutate(
                        { id: product.id, ...data },
                        {
                          onSuccess: () => {
                            toast.success("Producto actualizado");
                            setEditId(null);
                          },
                          onError: () => toast.error("No se pudo actualizar"),
                        },
                      )
                    }
                  />
                </div>
              ) : (
                <div className="flex items-start gap-3 p-4">
                  {/* Product image with upload overlay */}
                  <button
                    type="button"
                    onClick={() => handleImageClick(product.id)}
                    title="Cambiar imagen"
                    className="group relative h-14 w-14 shrink-0 rounded-xl overflow-hidden border border-sand-200 bg-sand-100 flex items-center justify-center"
                  >
                    {product.imageUrl ? (
                      <img
                        src={product.imageUrl}
                        alt={product.name}
                        className="h-full w-full object-cover"
                      />
                    ) : (
                      <span className="text-xl select-none">📦</span>
                    )}
                    {uploadingId === product.id ? (
                      <div className="absolute inset-0 flex items-center justify-center bg-black/50">
                        <svg
                          className="h-5 w-5 animate-spin text-white"
                          fill="none"
                          viewBox="0 0 24 24"
                        >
                          <circle
                            className="opacity-25"
                            cx="12"
                            cy="12"
                            r="10"
                            stroke="currentColor"
                            strokeWidth="4"
                          />
                          <path
                            className="opacity-75"
                            fill="currentColor"
                            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
                          />
                        </svg>
                      </div>
                    ) : (
                      <div className="absolute inset-0 flex items-center justify-center bg-black/0 group-hover:bg-black/40 transition-colors">
                        <svg
                          viewBox="0 0 16 16"
                          fill="white"
                          className="h-5 w-5 opacity-0 group-hover:opacity-100 transition-opacity"
                          aria-hidden="true"
                        >
                          <path d="M8 1.5a.75.75 0 0 1 .75.75V7h4.75a.75.75 0 0 1 0 1.5H8.75v4.75a.75.75 0 0 1-1.5 0V8.5H2.5a.75.75 0 0 1 0-1.5h4.75V2.25A.75.75 0 0 1 8 1.5Z" />
                        </svg>
                      </div>
                    )}
                  </button>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <p className="font-semibold text-sand-900 truncate">
                        {product.name}
                      </p>
                      {!product.isAvailable && (
                        <span className="rounded-full bg-sand-200 px-2 py-0.5 text-[10px] font-semibold text-sand-600">
                          No disponible
                        </span>
                      )}
                    </div>
                    <p className="text-xs text-sand-500">
                      {CATEGORY_LABELS[product.category]}
                    </p>
                    <p className="text-sm font-bold text-rescue-700 mt-0.5">
                      ₡{product.priceCrc.toLocaleString("es-CR")}
                    </p>
                  </div>
                  <div className="flex items-center gap-1 shrink-0">
                    <button
                      type="button"
                      onClick={() => setEditId(product.id)}
                      className="rounded-lg p-2 text-sand-400 hover:bg-sand-100 hover:text-brand-600"
                    >
                      <svg
                        viewBox="0 0 16 16"
                        fill="currentColor"
                        className="h-4 w-4"
                        aria-hidden="true"
                      >
                        <path d="M11.013 1.427a1.75 1.75 0 0 1 2.474 0l1.086 1.086a1.75 1.75 0 0 1 0 2.474l-8.61 8.61c-.21.21-.47.364-.756.445l-3.251.93a.75.75 0 0 1-.927-.928l.929-3.25c.081-.286.235-.547.445-.758l8.61-8.61Z" />
                      </svg>
                    </button>
                    <button
                      type="button"
                      onClick={() =>
                        del.mutate(product.id, {
                          onSuccess: () => toast.success("Producto eliminado"),
                          onError: () => toast.error("No se pudo eliminar"),
                        })
                      }
                      className="rounded-lg p-2 text-sand-300 hover:bg-danger-50 hover:text-danger-500"
                    >
                      <svg
                        viewBox="0 0 16 16"
                        fill="currentColor"
                        className="h-4 w-4"
                        aria-hidden="true"
                      >
                        <path d="M11 1.75V3h2.25a.75.75 0 0 1 0 1.5H2.75a.75.75 0 0 1 0-1.5H5V1.75C5 .784 5.784 0 6.75 0h2.5C10.216 0 11 .784 11 1.75ZM4.496 6.675l.66 6.6a.25.25 0 0 0 .249.225h5.19a.25.25 0 0 0 .249-.225l.66-6.6a.75.75 0 0 1 1.492.149l-.66 6.6A1.748 1.748 0 0 1 10.595 15h-5.19a1.75 1.75 0 0 1-1.741-1.575l-.66-6.6a.75.75 0 1 1 1.492-.15ZM6.5 1.75V3h3V1.75a.25.25 0 0 0-.25-.25h-2.5a.25.25 0 0 0-.25.25Z" />
                      </svg>
                    </button>
                  </div>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}

      {/* Hidden file input shared across all product image buttons */}
      <input
        ref={fileInputRef}
        type="file"
        accept={ACCEPTED}
        className="sr-only"
        onChange={handleFileChange}
        aria-hidden="true"
      />
    </div>
  );
}
