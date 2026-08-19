import { useRef, useState } from "react";
import { Button } from "@/shared/ui/Button";
import { toast } from "@/shared/lib/toast";
import { Skeleton } from "@/shared/ui/Spinner";
import {
  useAdminBillboards,
  useCreateBillboard,
  useSetBillboardStatus,
  useUploadBillboardImage,
} from "@/features/advertising/hooks/useBillboards";
import type {
  BillboardDto,
  BillboardPlacement,
} from "@/features/advertising/api/billboardsApi";

const PLACEMENTS: BillboardPlacement[] = [
  "Map",
  "Dashboard",
  "Directory",
  "Feed",
];
const PLACEMENT_LABELS: Record<BillboardPlacement, string> = {
  Map: "🗺️ Mapa",
  Dashboard: "🏠 Dashboard",
  Directory: "🗂️ Directorio",
  Feed: "📋 Feed",
};

const STATUS_COLORS: Record<string, string> = {
  Draft: "bg-sand-100 text-sand-700",
  Active: "bg-rescue-100 text-rescue-700",
  Paused: "bg-warn-100 text-warn-700",
  Expired: "bg-danger-100 text-danger-600",
};

function BillboardRow({ b }: { b: BillboardDto }) {
  const setStatus = useSetBillboardStatus();
  const uploadImg = useUploadBillboardImage();
  const fileRef = useRef<HTMLInputElement>(null);
  const MAX_BYTES = 5 * 1024 * 1024;

  const handleFile = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;
    if (file.size > MAX_BYTES) {
      toast.error("Máximo 5 MB.");
      return;
    }
    uploadImg.mutate(
      { id: b.id, file },
      {
        onSuccess: () => toast.success("Imagen actualizada"),
        onError: () => toast.error("Error al subir imagen"),
      },
    );
  };

  return (
    <li className="rounded-xl border border-sand-100 bg-surface p-4 space-y-2">
      <div className="flex items-start gap-3">
        {b.imageUrl && (
          <img
            src={b.imageUrl}
            alt={b.title}
            className="h-14 w-20 rounded-lg object-cover shrink-0 border border-sand-200"
          />
        )}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <p className="font-semibold text-ink-900 text-sm truncate">
              {b.title}
            </p>
            <span
              className={`text-[10px] font-bold rounded-full px-2 py-0.5 ${STATUS_COLORS[b.status] ?? ""}`}
            >
              {b.status}
            </span>
            <span className="text-[10px] text-sand-500">
              {PLACEMENT_LABELS[b.placement] ?? b.placement}
            </span>
          </div>
          {b.body && (
            <p className="text-xs text-sand-500 line-clamp-2">{b.body}</p>
          )}
          <p className="text-[10px] text-sand-400">
            {new Date(b.startsAt).toLocaleDateString("es-CR")} →{" "}
            {new Date(b.endsAt).toLocaleDateString("es-CR")}
          </p>
        </div>
      </div>

      <div className="flex gap-2 flex-wrap">
        {b.status !== "Active" && b.status !== "Expired" && (
          <Button
            size="sm"
            onClick={() =>
              setStatus.mutate(
                { id: b.id, status: "active" },
                {
                  onSuccess: () => toast.success("Activada"),
                },
              )
            }
          >
            Activar
          </Button>
        )}
        {b.status === "Active" && (
          <Button
            size="sm"
            variant="secondary"
            onClick={() =>
              setStatus.mutate(
                { id: b.id, status: "paused" },
                {
                  onSuccess: () => toast.success("Pausada"),
                },
              )
            }
          >
            Pausar
          </Button>
        )}
        <Button
          size="sm"
          variant="ghost"
          onClick={() => fileRef.current?.click()}
        >
          {uploadImg.isPending ? "Subiendo…" : "📷 Imagen"}
        </Button>
        <input
          ref={fileRef}
          type="file"
          accept="image/jpeg,image/png,image/webp"
          className="sr-only"
          onChange={handleFile}
        />
      </div>
    </li>
  );
}

function CreateBillboardForm({ onClose }: { onClose: () => void }) {
  const create = useCreateBillboard();
  const [form, setForm] = useState({
    title: "",
    body: "",
    placement: "Map" as BillboardPlacement,
    startsAt: new Date().toISOString().slice(0, 16),
    endsAt: new Date(Date.now() + 7 * 86_400_000).toISOString().slice(0, 16),
    ctaLabel: "",
    ctaUrl: "",
    priority: 0,
  });

  const handleSubmit = () => {
    create.mutate(
      {
        ...form,
        body: form.body || undefined,
        ctaLabel: form.ctaLabel || undefined,
        ctaUrl: form.ctaUrl || undefined,
        startsAt: new Date(form.startsAt).toISOString(),
        endsAt: new Date(form.endsAt).toISOString(),
      },
      {
        onSuccess: () => {
          toast.success("Valla creada");
          onClose();
        },
        onError: () => toast.error("No se pudo crear"),
      },
    );
  };

  const field = <K extends keyof typeof form>(k: K) => ({
    value: form[k] as string,
    onChange: (
      e: React.ChangeEvent<
        HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
      >,
    ) =>
      setForm((f) => ({
        ...f,
        [k]: k === "priority" ? Number(e.target.value) : e.target.value,
      })),
  });

  return (
    <div className="rounded-2xl border border-brand-200 bg-brand-50 p-4 space-y-3">
      <h3 className="text-sm font-semibold text-brand-800">
        Nueva valla publicitaria
      </h3>
      <div className="grid grid-cols-2 gap-3">
        <div className="col-span-2">
          <label className="mb-1 block text-xs font-medium text-sand-600">
            Título *
          </label>
          <input
            {...field("title")}
            placeholder="Cuida a tu mascota con..."
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
          />
        </div>
        <div className="col-span-2">
          <label className="mb-1 block text-xs font-medium text-sand-600">
            Descripción
          </label>
          <textarea
            {...field("body")}
            rows={2}
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
          />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">
            Ubicación *
          </label>
          <select
            {...field("placement")}
            className="w-full rounded-xl border border-sand-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
          >
            {PLACEMENTS.map((p) => (
              <option key={p} value={p}>
                {PLACEMENT_LABELS[p]}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">
            Prioridad (0-100)
          </label>
          <input
            type="number"
            min="0"
            max="100"
            {...field("priority")}
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
          />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">
            Inicio
          </label>
          <input
            type="datetime-local"
            {...field("startsAt")}
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm"
          />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">
            Fin
          </label>
          <input
            type="datetime-local"
            {...field("endsAt")}
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm"
          />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">
            CTA texto
          </label>
          <input
            {...field("ctaLabel")}
            placeholder="Ver más →"
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
          />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">
            CTA URL
          </label>
          <input
            {...field("ctaUrl")}
            placeholder="https://..."
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
          />
        </div>
      </div>
      <div className="flex gap-2">
        <Button onClick={handleSubmit} loading={create.isPending} size="sm">
          Crear valla
        </Button>
        <Button variant="secondary" onClick={onClose} size="sm">
          Cancelar
        </Button>
      </div>
    </div>
  );
}

export function AdminBillboardsTab() {
  const { data, isLoading } = useAdminBillboards();
  const [showForm, setShowForm] = useState(false);

  return (
    <div className="space-y-5">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-bold text-ink-900">
          🪧 Vallas publicitarias
        </h2>
        {!showForm && (
          <Button size="sm" onClick={() => setShowForm(true)}>
            + Nueva valla
          </Button>
        )}
      </div>

      {showForm && <CreateBillboardForm onClose={() => setShowForm(false)} />}

      {isLoading && (
        <div className="space-y-3">
          {[...Array(3)].map((_, i) => (
            <Skeleton key={i} className="h-24 rounded-xl" />
          ))}
        </div>
      )}

      {!isLoading && (data?.items ?? []).length === 0 && (
        <p className="text-center py-10 text-sm text-sand-400">
          No hay vallas. Crea la primera para monetizar ubicaciones del mapa.
        </p>
      )}

      <ul className="space-y-3">
        {(data?.items ?? []).map((b) => (
          <BillboardRow key={b.id} b={b} />
        ))}
      </ul>
    </div>
  );
}
