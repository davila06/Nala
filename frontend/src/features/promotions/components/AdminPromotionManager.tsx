import { useState } from "react";
import { toast } from "@/shared/lib/toast";
import { Button, Input, Card } from "@/shared/ui";
import {
  useAdminPromotions,
  useCreatePromotionBatch,
  useTogglePromotion,
} from "../hooks/usePromotion";
import type { PromotionCodeDto, PromotionSpecRequest, PromotionType } from "../api/promotionApi";

// ── Default spec factory ──────────────────────────────────────────────────────

const defaultSpec = (): PromotionSpecRequest => ({
  type: "FreeTier",
  targetTier: "UserPlus",
  maxRedemptions: 1,
  quantity: 1,
});

// ── Single spec form row ──────────────────────────────────────────────────────

function SpecRow({
  spec,
  index,
  onChange,
  onRemove,
  canRemove,
}: {
  spec: PromotionSpecRequest;
  index: number;
  onChange: (s: PromotionSpecRequest) => void;
  onRemove: () => void;
  canRemove: boolean;
}) {
  const update = (patch: Partial<PromotionSpecRequest>) => onChange({ ...spec, ...patch });

  return (
    <div className="rounded-xl border border-sand-200 bg-white p-4 space-y-3">
      <div className="flex items-center justify-between">
        <span className="text-xs font-semibold text-sand-500 uppercase tracking-wide">
          Código {index + 1}
        </span>
        {canRemove && (
          <button type="button" onClick={onRemove}
            className="text-xs text-danger-500 hover:text-danger-700 font-medium">
            Eliminar
          </button>
        )}
      </div>

      <div className="grid grid-cols-2 gap-3">
        {/* Type */}
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">Tipo de beneficio</label>
          <select
            value={spec.type}
            onChange={(e) => {
              const t = e.target.value as PromotionType;
              const patch: Partial<PromotionSpecRequest> = { type: t };
              if (t === "PercentageDiscount") {
                patch.discountPercent = 10;
                patch.freeMonths = undefined;
                patch.targetTier = undefined;
              } else if (t === "FreeTier") {
                patch.freeMonths = undefined;
                patch.discountPercent = undefined;
                patch.targetTier = "UserPlus";
              } else {
                patch.freeMonths = 1;
                patch.discountPercent = undefined;
                patch.targetTier = "UserPlus";
              }
              update(patch);
            }}
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
          >
            <option value="PercentageDiscount">% Descuento (DES__)</option>
            <option value="FreeTier">Cuenta gratis (FREE__)</option>
            <option value="FreeMonths">Meses gratis (MES__)</option>
          </select>
        </div>

        {/* Type-specific config */}
        {spec.type === "PercentageDiscount" && (
          <div>
            <label className="mb-1 block text-xs font-medium text-sand-600">
              Porcentaje de descuento
            </label>
            <select
              value={spec.discountPercent ?? 10}
              onChange={(e) => update({ discountPercent: Number(e.target.value) })}
              className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
            >
              <option value={10}>10% — código DES10XXX</option>
              <option value={15}>15% — código DES15XXX</option>
            </select>
          </div>
        )}

        {spec.type === "FreeTier" && (
          <div>
            <label className="mb-1 block text-xs font-medium text-sand-600">Plan gratuito</label>
            <select
              value={spec.targetTier ?? "UserPlus"}
              onChange={(e) => update({ targetTier: e.target.value })}
              className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
            >
              <option value="UserPlus">Plus — código FREEPLXX</option>
              <option value="UserFamilia">Familia — código FREEFAXX</option>
            </select>
          </div>
        )}

        {spec.type === "FreeMonths" && (
          <>
            <div>
              <label className="mb-1 block text-xs font-medium text-sand-600">Duración</label>
              <select
                value={spec.freeMonths ?? 1}
                onChange={(e) => update({ freeMonths: Number(e.target.value) })}
                className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
              >
                <option value={1}>1 mes — código MES01XXX</option>
                <option value={3}>3 meses — código MES03XXX</option>
                <option value={6}>6 meses — código MES06XXX</option>
              </select>
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-sand-600">Plan</label>
              <select
                value={spec.targetTier ?? "UserPlus"}
                onChange={(e) => update({ targetTier: e.target.value })}
                className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
              >
                <option value="UserPlus">Plus</option>
                <option value="UserFamilia">Familia</option>
              </select>
            </div>
          </>
        )}
      </div>

      <div className="grid grid-cols-3 gap-3">
        {/* Max redemptions */}
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">
            Usos máx. (-1 = ∞)
          </label>
          <Input
            type="number"
            min={-1}
            value={spec.maxRedemptions}
            onChange={(e) => update({ maxRedemptions: Number(e.target.value) })}
          />
        </div>

        {/* Quantity */}
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">
            Cantidad de códigos
          </label>
          <Input
            type="number"
            min={1}
            max={100}
            value={spec.quantity}
            onChange={(e) => update({ quantity: Number(e.target.value) })}
          />
        </div>

        {/* Expiry */}
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">Vence (opcional)</label>
          <Input
            type="date"
            value={spec.expiresAt ? spec.expiresAt.slice(0, 10) : ""}
            onChange={(e) =>
              update({ expiresAt: e.target.value ? new Date(e.target.value).toISOString() : undefined })
            }
          />
        </div>
      </div>

      {/* Admin note */}
      <div>
        <label className="mb-1 block text-xs font-medium text-sand-600">Nota interna</label>
        <Input
          placeholder="Ej: Campaña influencers agosto 2026"
          value={spec.adminNote ?? ""}
          onChange={(e) => update({ adminNote: e.target.value || undefined })}
        />
      </div>

      {/* Code preview */}
      <div className="rounded-lg bg-sand-100 px-3 py-2 text-xs font-mono text-sand-600">
        Formato: {buildCodePreview(spec)}
        {spec.quantity > 1 && ` × ${spec.quantity} códigos únicos`}
      </div>
    </div>
  );
}

function buildCodePreview(s: PromotionSpecRequest): string {
  if (s.type === "PercentageDiscount")
    return `DES${String(s.discountPercent ?? 10).padStart(2, "0")}___`;
  if (s.type === "FreeTier")
    return `FREE${s.targetTier === "UserFamilia" ? "FA" : "PL"}__`;
  const m = String(s.freeMonths ?? 1).padStart(2, "0");
  return `MES${m}___`;
}

// ── Code list row ─────────────────────────────────────────────────────────────

function CodeRow({ code }: { code: PromotionCodeDto }) {
  const toggle = useTogglePromotion();
  const pct = code.usagePercent =
    code.maxRedemptions === -1 ? null : Math.round((code.redeemedCount / code.maxRedemptions) * 100);

  return (
    <tr className="border-b border-sand-100 hover:bg-sand-50">
      <td className="py-2 px-3 font-mono text-sm font-semibold text-sand-800">{code.code}</td>
      <td className="py-2 px-3 text-xs text-sand-600">{typeLabel(code)}</td>
      <td className="py-2 px-3 text-xs text-sand-600">
        {code.redeemedCount}/{code.maxRedemptions === -1 ? "∞" : code.maxRedemptions}
        {pct !== null && (
          <div className="mt-1 h-1 rounded-full bg-sand-200 w-16">
            <div className="h-1 rounded-full bg-brand-500" style={{ width: `${pct}%` }} />
          </div>
        )}
      </td>
      <td className="py-2 px-3 text-xs text-sand-500">
        {code.expiresAt ? new Date(code.expiresAt).toLocaleDateString("es-CR") : "—"}
      </td>
      <td className="py-2 px-3">
        <span className={`rounded-full px-2 py-0.5 text-xs font-semibold ${
          code.isActive ? "bg-green-100 text-green-700" : "bg-sand-100 text-sand-500"
        }`}>
          {code.isActive ? "Activo" : "Inactivo"}
        </span>
      </td>
      <td className="py-2 px-3 text-xs text-sand-400 max-w-[120px] truncate">{code.adminNote ?? "—"}</td>
      <td className="py-2 px-3">
        <button
          type="button"
          disabled={toggle.isPending}
          onClick={() => toggle.mutate({ id: code.id, activate: !code.isActive }, {
            onSuccess: () => toast.success(code.isActive ? "Código desactivado" : "Código activado"),
          })}
          className="text-xs font-medium text-brand-600 hover:text-brand-800 disabled:opacity-50"
        >
          {code.isActive ? "Desactivar" : "Activar"}
        </button>
      </td>
    </tr>
  );
}

function typeLabel(c: PromotionCodeDto): string {
  if (c.type === "PercentageDiscount") return `${c.discountPercent}% desc.`;
  if (c.type === "FreeTier") return `Gratis ${tierShort(c.targetTier)} (1 mes)`;
  return `${c.freeMonths} mes${c.freeMonths !== 1 ? "es" : ""} ${tierShort(c.targetTier)}`;
}
function tierShort(t: string | null) {
  if (t === "UserPlus") return "Plus";
  if (t === "UserFamilia") return "Familia";
  return t ?? "";
}

// ── Main admin component ──────────────────────────────────────────────────────

export function AdminPromotionManager() {
  const { data: codes, isLoading } = useAdminPromotions();
  const createBatch = useCreatePromotionBatch();
  const [specs, setSpecs] = useState<PromotionSpecRequest[]>([defaultSpec()]);
  const [showForm, setShowForm] = useState(false);
  const [lastCreated, setLastCreated] = useState<PromotionCodeDto[]>([]);

  const updateSpec = (i: number, s: PromotionSpecRequest) =>
    setSpecs((prev) => prev.map((x, idx) => (idx === i ? s : x)));

  const handleCreate = () => {
    createBatch.mutate(specs, {
      onSuccess: (created) => {
        setLastCreated(created);
        setSpecs([defaultSpec()]);
        setShowForm(false);
        toast.success(`${created.length} código${created.length !== 1 ? "s" : ""} creado${created.length !== 1 ? "s" : ""}`);
      },
      onError: () => toast.error("No se pudieron crear los códigos"),
    });
  };

  const totalCodes = specs.reduce((s, x) => s + x.quantity, 0);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="font-display text-base font-semibold text-sand-800">
          🎁 Códigos de promoción
        </h2>
        <Button size="sm" onClick={() => { setShowForm((v) => !v); setLastCreated([]); }}>
          {showForm ? "Cancelar" : "+ Crear códigos"}
        </Button>
      </div>

      {/* Creation form */}
      {showForm && (
        <Card padding="md">
          <div className="space-y-4">
            <h3 className="text-sm font-semibold text-sand-700">Nueva tanda de códigos</h3>
            {specs.map((s, i) => (
              <SpecRow
                key={i}
                spec={s}
                index={i}
                onChange={(updated) => updateSpec(i, updated)}
                onRemove={() => setSpecs((prev) => prev.filter((_, idx) => idx !== i))}
                canRemove={specs.length > 1}
              />
            ))}
            <button
              type="button"
              onClick={() => setSpecs((prev) => [...prev, defaultSpec()])}
              className="text-sm font-medium text-brand-600 hover:text-brand-800"
            >
              + Agregar otro tipo de código
            </button>
            <div className="flex items-center justify-between pt-2 border-t border-sand-100">
              <span className="text-xs text-sand-500">
                Se generarán <strong>{totalCodes}</strong> código{totalCodes !== 1 ? "s" : ""} únicos
              </span>
              <Button onClick={handleCreate} loading={createBatch.isPending} disabled={totalCodes < 1}>
                Generar {totalCodes} código{totalCodes !== 1 ? "s" : ""}
              </Button>
            </div>
          </div>
        </Card>
      )}

      {/* Last created batch */}
      {lastCreated.length > 0 && (
        <Card padding="md">
          <p className="text-sm font-semibold text-green-700 mb-3">
            ✅ {lastCreated.length} código{lastCreated.length !== 1 ? "s" : ""} generado{lastCreated.length !== 1 ? "s" : ""}
          </p>
          <div className="flex flex-wrap gap-2">
            {lastCreated.map((c) => (
              <span key={c.id}
                className="rounded-lg border border-sand-200 bg-sand-50 px-3 py-1.5 font-mono text-sm font-semibold text-sand-800">
                {c.code}
              </span>
            ))}
          </div>
          <p className="mt-2 text-xs text-sand-400">
            Copiá estos códigos ahora — no se muestran de nuevo en esta lista.
          </p>
        </Card>
      )}

      {/* Code list */}
      {isLoading ? (
        <div className="animate-pulse space-y-2">
          {[1, 2, 3].map((i) => <div key={i} className="h-10 rounded-xl bg-sand-100" />)}
        </div>
      ) : codes && codes.length > 0 ? (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-sand-200 text-left">
                {["Código", "Beneficio", "Uso", "Vence", "Estado", "Nota", ""].map((h) => (
                  <th key={h} className="py-2 px-3 text-xs font-semibold text-sand-500 uppercase tracking-wide">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {codes.map((c) => <CodeRow key={c.id} code={c} />)}
            </tbody>
          </table>
        </div>
      ) : (
        <Card padding="sm">
          <p className="text-center text-sm text-sand-400">No hay códigos creados aún.</p>
        </Card>
      )}
    </div>
  );
}

// Extend DTO locally for UI only
declare module "../api/promotionApi" {
  interface PromotionCodeDto {
    usagePercent?: number | null;
  }
}
