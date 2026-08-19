import { useState } from "react";
import { Helmet } from "react-helmet-async";
import { Button, Input } from "@/shared/ui";
import { toast } from "@/shared/lib/toast";
import { Skeleton } from "@/shared/ui/Spinner";
import { useMyStoreProducts, useAddProduct, useUpdateProduct, useDeleteProduct } from "../hooks/useStores";
import { CATEGORY_LABELS } from "../api/storesApi";
import type { StoreProductDto, ProductCategory } from "../api/storesApi";

const CATEGORIES = Object.keys(CATEGORY_LABELS) as ProductCategory[];

function ProductForm({
  initial,
  onSave,
  onCancel,
  loading,
}: {
  initial?: StoreProductDto;
  onSave: (data: { name: string; description?: string; category: string; priceCrc: number; isAvailable: boolean }) => void;
  onCancel: () => void;
  loading: boolean;
}) {
  const [name, setName] = useState(initial?.name ?? "");
  const [description, setDescription] = useState(initial?.description ?? "");
  const [category, setCategory] = useState<ProductCategory>(initial?.category ?? "Other");
  const [priceCrc, setPriceCrc] = useState(String(initial?.priceCrc ?? ""));
  const [isAvailable, setIsAvailable] = useState(initial?.isAvailable ?? true);

  const handleSubmit = () => {
    if (!name.trim() || !priceCrc) { toast.error("Nombre y precio son requeridos."); return; }
    onSave({ name: name.trim(), description: description.trim() || undefined, category, priceCrc: Number(priceCrc), isAvailable });
  };

  return (
    <div className="rounded-2xl border border-brand-200 bg-brand-50 p-4 space-y-3">
      <h3 className="text-sm font-semibold text-brand-800">{initial ? "Editar producto" : "Nuevo producto"}</h3>
      <div className="grid grid-cols-2 gap-3">
        <div className="col-span-2">
          <label className="mb-1 block text-xs font-medium text-sand-600">Nombre *</label>
          <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Alimento Premium 3kg" />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">Categoría *</label>
          <select value={category} onChange={(e) => setCategory(e.target.value as ProductCategory)}
            className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400">
            {CATEGORIES.map((c) => <option key={c} value={c}>{CATEGORY_LABELS[c]}</option>)}
          </select>
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">Precio ₡ *</label>
          <Input type="number" value={priceCrc} onChange={(e) => setPriceCrc(e.target.value)} placeholder="3500" min="0" />
        </div>
        <div className="col-span-2">
          <label className="mb-1 block text-xs font-medium text-sand-600">Descripción</label>
          <textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={2}
            className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm placeholder:text-sand-400 focus:outline-none focus:ring-2 focus:ring-brand-400" />
        </div>
        {initial && (
          <div className="col-span-2 flex items-center gap-2">
            <input type="checkbox" id="avail" checked={isAvailable} onChange={(e) => setIsAvailable(e.target.checked)} className="h-4 w-4" />
            <label htmlFor="avail" className="text-sm text-sand-700">Disponible para pedidos</label>
          </div>
        )}
      </div>
      <div className="flex gap-2">
        <Button onClick={handleSubmit} loading={loading} size="sm">Guardar</Button>
        <Button variant="secondary" onClick={onCancel} size="sm">Cancelar</Button>
      </div>
    </div>
  );
}

export default function StoreProductsPage() {
  const { data: products, isLoading } = useMyStoreProducts();
  const add    = useAddProduct();
  const update = useUpdateProduct();
  const del    = useDeleteProduct();
  const [showForm, setShowForm] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);

  if (isLoading) return <div className="mx-auto max-w-2xl px-4 py-10"><Skeleton className="h-48 rounded-2xl" /></div>;

  const editing = editId ? products?.find((p) => p.id === editId) : undefined;

  return (
    <div className="mx-auto max-w-2xl px-4 py-8 space-y-5 animate-fade-in-up">
      <Helmet><title>Mis productos — PawTrack CR</title></Helmet>

      <div className="flex items-center justify-between">
        <h1 className="font-display text-xl font-bold text-sand-900">Mis productos</h1>
        {!showForm && !editId && (
          <Button size="sm" onClick={() => setShowForm(true)}>+ Agregar</Button>
        )}
      </div>

      {showForm && (
        <ProductForm
          loading={add.isPending}
          onCancel={() => setShowForm(false)}
          onSave={(data) => add.mutate(data, {
            onSuccess: () => { toast.success("Producto agregado"); setShowForm(false); },
            onError: () => toast.error("No se pudo guardar"),
          })}
        />
      )}

      {(products ?? []).length === 0 && !showForm ? (
        <p className="py-10 text-center text-sm text-sand-400">No tienes productos aún. Agrega el primero.</p>
      ) : (
        <ul className="space-y-3">
          {(products ?? []).map((product) => (
            <li key={product.id} className="rounded-xl border border-sand-100 bg-surface">
              {editId === product.id ? (
                <div className="p-4">
                  <ProductForm
                    initial={product}
                    loading={update.isPending}
                    onCancel={() => setEditId(null)}
                    onSave={(data) => update.mutate({ id: product.id, ...data }, {
                      onSuccess: () => { toast.success("Producto actualizado"); setEditId(null); },
                      onError: () => toast.error("No se pudo actualizar"),
                    })}
                  />
                </div>
              ) : (
                <div className="flex items-start gap-3 p-4">
                  {product.imageUrl && (
                    <img src={product.imageUrl} alt={product.name} className="h-14 w-14 rounded-xl object-cover border border-sand-200 shrink-0" />
                  )}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <p className="font-semibold text-sand-900 truncate">{product.name}</p>
                      {!product.isAvailable && (
                        <span className="rounded-full bg-sand-200 px-2 py-0.5 text-[10px] font-semibold text-sand-600">No disponible</span>
                      )}
                    </div>
                    <p className="text-xs text-sand-500">{CATEGORY_LABELS[product.category]}</p>
                    <p className="text-sm font-bold text-rescue-700 mt-0.5">₡{product.priceCrc.toLocaleString("es-CR")}</p>
                  </div>
                  <div className="flex items-center gap-1 shrink-0">
                    <button type="button" onClick={() => setEditId(product.id)}
                      className="rounded-lg p-2 text-sand-400 hover:bg-sand-100 hover:text-brand-600">
                      <svg viewBox="0 0 16 16" fill="currentColor" className="h-4 w-4" aria-hidden="true">
                        <path d="M11.013 1.427a1.75 1.75 0 0 1 2.474 0l1.086 1.086a1.75 1.75 0 0 1 0 2.474l-8.61 8.61c-.21.21-.47.364-.756.445l-3.251.93a.75.75 0 0 1-.927-.928l.929-3.25c.081-.286.235-.547.445-.758l8.61-8.61Z" />
                      </svg>
                    </button>
                    <button type="button"
                      onClick={() => del.mutate(product.id, {
                        onSuccess: () => toast.success("Producto eliminado"),
                        onError: () => toast.error("No se pudo eliminar"),
                      })}
                      className="rounded-lg p-2 text-sand-300 hover:bg-danger-50 hover:text-danger-500">
                      <svg viewBox="0 0 16 16" fill="currentColor" className="h-4 w-4" aria-hidden="true">
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
    </div>
  );
}
