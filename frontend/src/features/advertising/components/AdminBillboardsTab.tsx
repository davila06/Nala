import { useRef, useState } from "react";
import { Button } from "@/shared/ui/Button";
import { toast } from "@/shared/lib/toast";
import { Skeleton } from "@/shared/ui/Spinner";
import {
  useAdminBillboards,
  useCreateBillboard,
  useDeleteBillboard,
  useBillboardMetrics,
  useReviewBillboard,
  useSetBillboardStatus,
  useSubmitBillboard,
  useUpdateBillboard,
  useUploadBillboardImage,
} from "@/features/advertising/hooks/useBillboards";
import type {
  BillboardDto,
  BillboardCategory,
  BillboardPlacement,
} from "@/features/advertising/api/billboardsApi";
import { BILLBOARD_PLACEMENTS } from "@/features/advertising/api/billboardsApi";

const PLACEMENTS: readonly BillboardPlacement[] = BILLBOARD_PLACEMENTS;
const PLACEMENT_LABELS: Record<BillboardPlacement, string> = {
  Map: "🗺️ Mapa",
  Dashboard: "🏠 Dashboard",
  Directory: "🗂️ Directorio",
  Feed: "📋 Feed",
  PublicPetProfile: "🏷️ Perfil público de mascota",
  ScanHistory: "📈 Historial de escaneos",
  CaseRoom: "🚨 Centro de comando",
  ClinicDirectory: "🏥 Directorio de clínicas",
  ClinicProfile: "🏥 Perfil de clínica",
  ServiceProviderDirectory: "🧰 Directorio de servicios",
  ServiceProviderProfile: "🧰 Perfil de servicio",
  AdoptionDirectory: "🐾 Directorio de adopciones",
  AdoptionFair: "🎪 Ferias de adopción",
  PetRegistration: "✅ Registro de mascota",
  CollarActivation: "📡 Activación de collar",
};

const STATUS_COLORS: Record<string, string> = {
  Draft: "bg-sand-100 text-sand-700",
  Active: "bg-rescue-100 text-rescue-700",
  Paused: "bg-warn-100 text-warn-700",
  Expired: "bg-danger-100 text-danger-600",
};

function BillboardRow({ b }: { b: BillboardDto }) {
  const setStatus = useSetBillboardStatus();
  const submit = useSubmitBillboard();
  const review = useReviewBillboard();
  const { data: metrics } = useBillboardMetrics(
    b.id,
    b.campaignStatus === "Approved" || b.status === "Active",
  );
  const uploadImg = useUploadBillboardImage();
  const remove = useDeleteBillboard();
  const update = useUpdateBillboard();
  const fileRef = useRef<HTMLInputElement>(null);
  const [rejectionNote, setRejectionNote] = useState("");
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState({
    title: b.title,
    body: b.body ?? "",
    ctaLabel: b.ctaLabel ?? "",
    ctaUrl: b.ctaUrl ?? "",
    startsAt: b.startsAt.slice(0, 16),
    endsAt: b.endsAt.slice(0, 16),
    priority: b.priority,
    advertiserName: b.advertiserName,
    category: b.category,
    targetCanton: b.targetCanton ?? "",
    contractReference: b.contractReference ?? "",
    budgetCrc: b.budgetCrc,
    frequencyCapPerDay: b.frequencyCapPerDay,
    isCategoryExclusive: b.isCategoryExclusive,
    isVip: b.isVip,
  });
  const MAX_BYTES = 5 * 1024 * 1024;
  const daysRemaining = Math.ceil(
    (new Date(b.endsAt).getTime() - Date.now()) / 86_400_000,
  );

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
          <p className="text-[10px] font-medium text-sand-500">
            {b.advertiserName} · {b.category} · {b.campaignStatus}
          </p>
          <p className="text-[10px] text-sand-500">
            {b.targetCanton ?? "Cobertura nacional"} · {b.frequencyCapPerDay}{" "}
            por día · ₡{b.budgetCrc.toLocaleString("es-CR")}
            {b.isCategoryExclusive ? " · Exclusiva" : ""}
            {b.isVip ? " · VIP" : ""}
          </p>
          {daysRemaining <= 7 && b.status === "Active" && (
            <p className="text-[10px] font-semibold text-warn-700">
              Vence en {Math.max(daysRemaining, 0)} días. Renovar o pausar.
            </p>
          )}
          {b.budgetCrc === 0 && (
            <p className="text-[10px] font-semibold text-danger-600">
              Sin presupuesto registrado: no usar para facturación.
            </p>
          )}
          {metrics && (
            <p className="text-[10px] text-sand-500">
              {metrics.impressions} impresiones · {metrics.clicks} clics · CTR{" "}
              {metrics.clickThroughRate}% · {metrics.conversions} conversiones
            </p>
          )}
        </div>
      </div>

      <div className="flex gap-2 flex-wrap">
        <Button
          size="sm"
          variant="ghost"
          onClick={() => setEditing((value) => !value)}
        >
          {editing ? "Cancelar edición" : "Editar"}
        </Button>
        <Button
          size="sm"
          variant="ghost"
          onClick={() => {
            if (
              !window.confirm(
                `Eliminar la campaña ${b.title}? Esta acción no se puede deshacer.`,
              )
            )
              return;
            remove.mutate(b.id, {
              onSuccess: () => toast.success("Campaña eliminada"),
              onError: () => toast.error("No se pudo eliminar"),
            });
          }}
        >
          Eliminar
        </Button>
        {b.campaignStatus === "Draft" && b.imageUrl && (
          <Button
            size="sm"
            variant="secondary"
            onClick={() =>
              submit.mutate(b.id, {
                onSuccess: () => toast.success("Enviada a revisión"),
                onError: () => toast.error("Completa los datos de campaña"),
              })
            }
          >
            Enviar a revisión
          </Button>
        )}
        {b.campaignStatus === "PendingReview" && (
          <>
            <div className="w-full">
              <label
                htmlFor={`billboard-rejection-${b.id}`}
                className="sr-only"
              >
                Motivo de rechazo
              </label>
              <textarea
                id={`billboard-rejection-${b.id}`}
                value={rejectionNote}
                onChange={(event) => setRejectionNote(event.target.value)}
                rows={2}
                placeholder="Motivo de rechazo o ajustes requeridos"
                className="w-full rounded-lg border border-sand-200 px-3 py-2 text-xs"
              />
            </div>
            <Button
              size="sm"
              onClick={() =>
                review.mutate(
                  { id: b.id, approve: true },
                  {
                    onSuccess: () => toast.success("Campaña aprobada"),
                    onError: () =>
                      toast.error(
                        "Requiere otro operador y categoría permitida",
                      ),
                  },
                )
              }
            >
              Aprobar
            </Button>
            <Button
              size="sm"
              variant="secondary"
              onClick={() =>
                review.mutate(
                  {
                    id: b.id,
                    approve: false,
                    note: rejectionNote,
                  },
                  {
                    onSuccess: () => toast.success("Campaña rechazada"),
                    onError: () => toast.error("Describe el motivo de rechazo"),
                  },
                )
              }
            >
              Rechazar
            </Button>
          </>
        )}
        {b.campaignStatus === "Approved" &&
          b.status !== "Active" &&
          b.status !== "Expired" && (
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
      {editing && (
        <div className="grid grid-cols-2 gap-2 border-t border-sand-100 pt-3">
          <input
            value={draft.title}
            onChange={(e) => setDraft({ ...draft, title: e.target.value })}
            aria-label="Editar título"
            className="col-span-2 rounded-lg border border-sand-200 px-3 py-2 text-sm"
          />
          <textarea
            value={draft.body}
            onChange={(e) => setDraft({ ...draft, body: e.target.value })}
            aria-label="Editar descripción"
            className="col-span-2 rounded-lg border border-sand-200 px-3 py-2 text-sm"
          />
          <input
            value={draft.ctaLabel}
            onChange={(e) => setDraft({ ...draft, ctaLabel: e.target.value })}
            placeholder="CTA"
            className="rounded-lg border border-sand-200 px-3 py-2 text-sm"
          />
          <input
            value={draft.ctaUrl}
            onChange={(e) => setDraft({ ...draft, ctaUrl: e.target.value })}
            placeholder="https://"
            className="rounded-lg border border-sand-200 px-3 py-2 text-sm"
          />
          <input
            value={draft.advertiserName}
            onChange={(e) =>
              setDraft({ ...draft, advertiserName: e.target.value })
            }
            placeholder="Anunciante"
            className="rounded-lg border border-sand-200 px-3 py-2 text-sm"
          />
          <select
            value={draft.category}
            onChange={(e) =>
              setDraft({
                ...draft,
                category: e.target.value as BillboardCategory,
              })
            }
            className="rounded-lg border border-sand-200 px-3 py-2 text-sm"
          >
            <option value="PetCare">Cuidado</option>
            <option value="EmergencyVeterinary">Emergencia veterinaria</option>
            <option value="RecoveryService">Recuperación</option>
            <option value="GpsAndIdentification">GPS e identificación</option>
            <option value="PetInsurance">Seguro</option>
            <option value="FoodAndNutrition">Alimentación</option>
            <option value="AdoptionSupport">Adopción responsable</option>
          </select>
          <input
            value={draft.targetCanton}
            onChange={(e) =>
              setDraft({ ...draft, targetCanton: e.target.value })
            }
            placeholder="Cantón"
            className="rounded-lg border border-sand-200 px-3 py-2 text-sm"
          />
          <input
            value={draft.contractReference}
            onChange={(e) =>
              setDraft({ ...draft, contractReference: e.target.value })
            }
            placeholder="Contrato"
            className="rounded-lg border border-sand-200 px-3 py-2 text-sm"
          />
          <input
            type="number"
            min="0"
            value={draft.budgetCrc}
            onChange={(e) =>
              setDraft({ ...draft, budgetCrc: Number(e.target.value) })
            }
            aria-label="Presupuesto CRC"
            className="rounded-lg border border-sand-200 px-3 py-2 text-sm"
          />
          <input
            type="number"
            min="1"
            max="10"
            value={draft.frequencyCapPerDay}
            onChange={(e) =>
              setDraft({ ...draft, frequencyCapPerDay: Number(e.target.value) })
            }
            aria-label="Frecuencia diaria"
            className="rounded-lg border border-sand-200 px-3 py-2 text-sm"
          />
          <input
            type="datetime-local"
            value={draft.startsAt}
            onChange={(e) => setDraft({ ...draft, startsAt: e.target.value })}
            className="rounded-lg border border-sand-200 px-3 py-2 text-sm"
          />
          <input
            type="datetime-local"
            value={draft.endsAt}
            onChange={(e) => setDraft({ ...draft, endsAt: e.target.value })}
            className="rounded-lg border border-sand-200 px-3 py-2 text-sm"
          />
          <label className="col-span-2 flex items-center gap-2 text-xs text-sand-700">
            <input
              type="checkbox"
              checked={draft.isCategoryExclusive}
              onChange={(e) =>
                setDraft({ ...draft, isCategoryExclusive: e.target.checked })
              }
            />{" "}
            Exclusividad de categoría
          </label>
          <label className="col-span-2 flex items-center gap-2 text-xs font-semibold text-warn-700">
            <input
              type="checkbox"
              checked={draft.isVip}
              onChange={(e) => setDraft({ ...draft, isVip: e.target.checked })}
            />
            Servicio VIP: prioridad destacada sobre campañas estándar
          </label>
          <Button
            size="sm"
            className="col-span-2"
            loading={update.isPending}
            onClick={() =>
              update.mutate(
                {
                  id: b.id,
                  data: {
                    ...draft,
                    body: draft.body || undefined,
                    ctaLabel: draft.ctaLabel || undefined,
                    ctaUrl: draft.ctaUrl || undefined,
                    targetCanton: draft.targetCanton || undefined,
                    contractReference: draft.contractReference || undefined,
                    startsAt: new Date(draft.startsAt).toISOString(),
                    endsAt: new Date(draft.endsAt).toISOString(),
                  },
                },
                {
                  onSuccess: () => {
                    toast.success(
                      "Campaña actualizada; requiere nueva revisión",
                    );
                    setEditing(false);
                  },
                  onError: () => toast.error("No se pudo actualizar"),
                },
              )
            }
          >
            Guardar cambios
          </Button>
        </div>
      )}
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
    advertiserName: "",
    category: "PetCare" as BillboardCategory,
    targetCanton: "",
    contractReference: "",
    budgetCrc: 0,
    frequencyCapPerDay: 1,
    isCategoryExclusive: false,
    isVip: false,
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
          <label
            htmlFor="billboard-advertiser"
            className="mb-1 block text-xs font-medium text-sand-600"
          >
            Anunciante *
          </label>
          <input
            id="billboard-advertiser"
            {...field("advertiserName")}
            placeholder="Nombre comercial del anunciante"
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
          />
        </div>
        <div>
          <label
            htmlFor="billboard-category"
            className="mb-1 block text-xs font-medium text-sand-600"
          >
            Categoría *
          </label>
          <select
            id="billboard-category"
            {...field("category")}
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm"
          >
            <option value="PetCare">Cuidado</option>
            <option value="EmergencyVeterinary">
              Veterinaria de emergencia
            </option>
            <option value="RecoveryService">Recuperación</option>
            <option value="GpsAndIdentification">GPS e identificación</option>
            <option value="PetInsurance">Seguro</option>
            <option value="FoodAndNutrition">Alimentación</option>
            <option value="AdoptionSupport">Adopción responsable</option>
          </select>
        </div>
        <div>
          <label
            htmlFor="billboard-canton"
            className="mb-1 block text-xs font-medium text-sand-600"
          >
            Cantón objetivo
          </label>
          <input
            id="billboard-canton"
            {...field("targetCanton")}
            placeholder="Ej. Heredia"
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm"
          />
        </div>
        <div className="col-span-2">
          <label
            htmlFor="billboard-title"
            className="mb-1 block text-xs font-medium text-sand-600"
          >
            Título *
          </label>
          <input
            id="billboard-title"
            {...field("title")}
            placeholder="Cuida a tu mascota con..."
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
          />
        </div>
        <div className="col-span-2">
          <label
            htmlFor="billboard-body"
            className="mb-1 block text-xs font-medium text-sand-600"
          >
            Descripción
          </label>
          <textarea
            id="billboard-body"
            {...field("body")}
            rows={2}
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
          />
        </div>
        <div>
          <label
            htmlFor="billboard-placement"
            className="mb-1 block text-xs font-medium text-sand-600"
          >
            Ubicación *
          </label>
          <select
            id="billboard-placement"
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
          <label
            htmlFor="billboard-priority"
            className="mb-1 block text-xs font-medium text-sand-600"
          >
            Prioridad (0-100)
          </label>
          <input
            type="number"
            min="0"
            max="100"
            id="billboard-priority"
            {...field("priority")}
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
          />
        </div>
        <div>
          <label
            htmlFor="billboard-budget"
            className="mb-1 block text-xs font-medium text-sand-600"
          >
            Presupuesto CRC
          </label>
          <input
            type="number"
            min="0"
            id="billboard-budget"
            {...field("budgetCrc")}
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm"
          />
        </div>
        <div>
          <label
            htmlFor="billboard-frequency"
            className="mb-1 block text-xs font-medium text-sand-600"
          >
            Máx. por día
          </label>
          <input
            type="number"
            min="1"
            max="10"
            id="billboard-frequency"
            {...field("frequencyCapPerDay")}
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm"
          />
        </div>
        <div className="col-span-2">
          <label
            htmlFor="billboard-contract"
            className="mb-1 block text-xs font-medium text-sand-600"
          >
            Contrato / orden de compra
          </label>
          <input
            id="billboard-contract"
            {...field("contractReference")}
            placeholder="CTR-2026-001"
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm"
          />
        </div>
        <label className="col-span-2 flex items-center gap-2 text-sm text-sand-700">
          <input
            type="checkbox"
            checked={form.isCategoryExclusive}
            onChange={(event) =>
              setForm((current) => ({
                ...current,
                isCategoryExclusive: event.target.checked,
              }))
            }
            className="rounded border-sand-300"
          />
          Exclusividad de categoría para este placement y período
        </label>
        <label className="col-span-2 flex items-center gap-2 text-sm font-semibold text-warn-700">
          <input
            type="checkbox"
            checked={form.isVip}
            onChange={(event) =>
              setForm((current) => ({
                ...current,
                isVip: event.target.checked,
              }))
            }
            className="rounded border-sand-300"
          />
          Servicio VIP: se destaca sobre campañas estándar sin bloquear otras
          campañas
        </label>
        <div>
          <label
            htmlFor="billboard-start"
            className="mb-1 block text-xs font-medium text-sand-600"
          >
            Inicio
          </label>
          <input
            type="datetime-local"
            id="billboard-start"
            {...field("startsAt")}
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm"
          />
        </div>
        <div>
          <label
            htmlFor="billboard-end"
            className="mb-1 block text-xs font-medium text-sand-600"
          >
            Fin
          </label>
          <input
            type="datetime-local"
            id="billboard-end"
            {...field("endsAt")}
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm"
          />
        </div>
        <div>
          <label
            htmlFor="billboard-cta-label"
            className="mb-1 block text-xs font-medium text-sand-600"
          >
            CTA texto
          </label>
          <input
            id="billboard-cta-label"
            {...field("ctaLabel")}
            placeholder="Ver más →"
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
          />
        </div>
        <div>
          <label
            htmlFor="billboard-cta-url"
            className="mb-1 block text-xs font-medium text-sand-600"
          >
            CTA URL
          </label>
          <input
            id="billboard-cta-url"
            {...field("ctaUrl")}
            placeholder="https://..."
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
          />
        </div>
      </div>
      <section
        className="border-t border-sand-200 pt-3"
        aria-label="Vista previa de la valla"
      >
        <p className="mb-2 text-xs font-semibold text-sand-600">
          Vista previa móvil
        </p>
        <div className="max-w-xs overflow-hidden rounded-xl border border-sand-200 bg-surface shadow-sm">
          <div className="px-4 py-3 space-y-1.5">
            <span className="text-[9px] font-bold uppercase tracking-widest text-sand-400">
              Publicidad
            </span>
            <p className="text-sm font-semibold text-ink-900">
              {form.title || "Título de campaña"}
            </p>
            {form.body && <p className="text-xs text-sand-600">{form.body}</p>}
            {form.ctaLabel && (
              <span className="inline-block rounded-xl bg-brand-500 px-4 py-1.5 text-xs font-semibold text-white">
                {form.ctaLabel}
              </span>
            )}
          </div>
        </div>
      </section>
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
  const [page, setPage] = useState(1);
  const { data, isLoading } = useAdminBillboards(page);
  const [showForm, setShowForm] = useState(false);
  const hasNextPage = (data?.pageNumber ?? 1) < (data?.totalPages ?? 1);

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
          {Array.from({ length: 3 }).map((_, i) => (
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

      {/* Pagination */}
      {!isLoading && (page > 1 || hasNextPage) && (
        <div className="flex items-center justify-center gap-4 pt-2">
          <button
            type="button"
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1}
            className="rounded-xl border border-sand-200 px-4 py-2 text-sm text-sand-700 hover:bg-sand-50 disabled:opacity-40 disabled:cursor-not-allowed"
          >
            ← Anterior
          </button>
          <span className="text-xs text-sand-500">
            {page} / {data?.totalPages ?? 1}
          </span>
          <button
            type="button"
            onClick={() => setPage((p) => p + 1)}
            disabled={!hasNextPage}
            className="rounded-xl border border-sand-200 px-4 py-2 text-sm text-sand-700 hover:bg-sand-50 disabled:opacity-40 disabled:cursor-not-allowed"
          >
            Siguiente →
          </button>
        </div>
      )}
    </div>
  );
}
