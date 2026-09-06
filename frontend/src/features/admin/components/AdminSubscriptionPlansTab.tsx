import { useState } from "react";
import type { SubscriptionTier } from "@/features/pets/api/subscriptionApi";
import type { SubscriptionPlanDto } from "../api/adminApi";
import {
  useCreateSubscriptionPlan,
  useDeleteSubscriptionPlan,
  useSubscriptionPlans,
  useUpdateSubscriptionPlan,
} from "../hooks/useAdmin";
import { toast } from "@/shared/lib/toast";

const PLAN_TIERS: SubscriptionTier[] = [
  "UserPlus",
  "UserFamilia",
  "ClinicPlus",
  "ClinicPartner",
  "StorePlus",
  "StorePartner",
  "ShelterPlus",
  "MuniBasica",
  "MuniFull",
  "MuniRedRegional",
];

type FormState = {
  tier: SubscriptionTier;
  displayName: string;
  description: string;
  monthlyPriceCrc: string;
  annualPriceCrc: string;
};

const emptyForm: FormState = {
  tier: "UserPlus",
  displayName: "",
  description: "",
  monthlyPriceCrc: "",
  annualPriceCrc: "",
};

function toPayload(form: FormState) {
  return {
    tier: form.tier,
    displayName: form.displayName,
    description: form.description,
    monthlyPriceCrc: form.monthlyPriceCrc ? Number(form.monthlyPriceCrc) : null,
    annualPriceCrc: form.annualPriceCrc ? Number(form.annualPriceCrc) : null,
  };
}

export function AdminSubscriptionPlansTab() {
  const { data, isLoading, isError } = useSubscriptionPlans();
  const createPlan = useCreateSubscriptionPlan();
  const updatePlan = useUpdateSubscriptionPlan();
  const deletePlan = useDeleteSubscriptionPlan();
  const [form, setForm] = useState<FormState>(emptyForm);
  const [editing, setEditing] = useState<SubscriptionPlanDto | null>(null);

  const startEdit = (plan: SubscriptionPlanDto) => {
    setEditing(plan);
    setForm({
      tier: plan.tier,
      displayName: plan.displayName,
      description: plan.description,
      monthlyPriceCrc: plan.monthlyPriceCrc?.toString() ?? "",
      annualPriceCrc: plan.annualPriceCrc?.toString() ?? "",
    });
  };

  const reset = () => {
    setEditing(null);
    setForm(emptyForm);
  };

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    try {
      if (editing) {
        await updatePlan.mutateAsync({
          id: editing.id,
          payload: { ...toPayload(form), version: editing.version },
        });
        toast.success("Plan actualizado");
      } else {
        await createPlan.mutateAsync(toPayload(form));
        toast.success("Plan creado");
      }
      reset();
    } catch {
      toast.error("No se pudo guardar el plan");
    }
  };

  const deactivate = async (plan: SubscriptionPlanDto) => {
    if (!window.confirm(`¿Desactivar ${plan.displayName}?`)) return;
    try {
      await deletePlan.mutateAsync({ id: plan.id, version: plan.version });
      toast.success("Plan desactivado");
      if (editing?.id === plan.id) reset();
    } catch {
      toast.error("El plan cambió o no pudo desactivarse");
    }
  };

  if (isLoading)
    return <p className="text-sm text-sand-500">Cargando planes...</p>;
  if (isError)
    return (
      <p className="text-sm text-danger-700">
        No se pudieron cargar los planes.
      </p>
    );

  return (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_360px]">
      <section className="space-y-3">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-lg font-bold text-sand-900">
              Planes y precios
            </h2>
            <p className="text-xs text-sand-500">
              Catálogo administrado con control de versión.
            </p>
          </div>
        </div>
        <div className="overflow-x-auto rounded-2xl border border-sand-200 bg-surface">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-sand-200 text-xs text-sand-500">
              <tr>
                <th className="px-4 py-3">Plan</th>
                <th className="px-4 py-3">Mensual</th>
                <th className="px-4 py-3">Anual</th>
                <th className="px-4 py-3">Estado</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody>
              {data?.map((plan) => (
                <tr
                  key={plan.id}
                  className="border-b border-sand-100 last:border-0"
                >
                  <td className="px-4 py-3">
                    <strong className="block text-sand-900">
                      {plan.displayName}
                    </strong>
                    <span className="text-xs text-sand-500">{plan.tier}</span>
                  </td>
                  <td className="px-4 py-3">
                    {plan.monthlyPriceCrc
                      ? `₡${plan.monthlyPriceCrc.toLocaleString("es-CR")}`
                      : "-"}
                  </td>
                  <td className="px-4 py-3">
                    {plan.annualPriceCrc
                      ? `₡${plan.annualPriceCrc.toLocaleString("es-CR")}`
                      : "-"}
                  </td>
                  <td className="px-4 py-3">
                    {plan.isActive ? "Activo" : "Inactivo"}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button
                      type="button"
                      className="mr-3 text-xs font-semibold text-rescue-700"
                      onClick={() => startEdit(plan)}
                    >
                      Editar
                    </button>
                    {plan.isActive && (
                      <button
                        type="button"
                        className="text-xs font-semibold text-danger-700"
                        onClick={() => void deactivate(plan)}
                      >
                        Desactivar
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <form
        onSubmit={(event) => void submit(event)}
        className="space-y-3 rounded-2xl border border-sand-200 bg-surface p-4 shadow-sm"
      >
        <div className="flex items-center justify-between">
          <h3 className="font-bold text-sand-900">
            {editing ? "Editar plan" : "Nuevo plan"}
          </h3>
          {editing && (
            <button
              type="button"
              className="text-xs text-sand-500"
              onClick={reset}
            >
              Cancelar
            </button>
          )}
        </div>
        <label className="block text-xs font-semibold text-sand-600">
          Tier
          <select
            disabled={Boolean(editing)}
            value={form.tier}
            onChange={(event) =>
              setForm({ ...form, tier: event.target.value as SubscriptionTier })
            }
            className="mt-1 w-full rounded-lg border border-sand-300 px-3 py-2 text-sm"
          >
            {PLAN_TIERS.map((tier) => (
              <option key={tier} value={tier}>
                {tier}
              </option>
            ))}
          </select>
        </label>
        <label className="block text-xs font-semibold text-sand-600">
          Nombre visible
          <input
            required
            maxLength={120}
            value={form.displayName}
            onChange={(event) =>
              setForm({ ...form, displayName: event.target.value })
            }
            className="mt-1 w-full rounded-lg border border-sand-300 px-3 py-2 text-sm"
          />
        </label>
        <label className="block text-xs font-semibold text-sand-600">
          Descripción
          <textarea
            required
            maxLength={2000}
            value={form.description}
            onChange={(event) =>
              setForm({ ...form, description: event.target.value })
            }
            className="mt-1 min-h-20 w-full rounded-lg border border-sand-300 px-3 py-2 text-sm"
          />
        </label>
        <div className="grid grid-cols-2 gap-3">
          <label className="block text-xs font-semibold text-sand-600">
            Mensual (CRC)
            <input
              type="number"
              min="1"
              value={form.monthlyPriceCrc}
              onChange={(event) =>
                setForm({ ...form, monthlyPriceCrc: event.target.value })
              }
              className="mt-1 w-full rounded-lg border border-sand-300 px-3 py-2 text-sm"
            />
          </label>
          <label className="block text-xs font-semibold text-sand-600">
            Anual (CRC)
            <input
              type="number"
              min="1"
              value={form.annualPriceCrc}
              onChange={(event) =>
                setForm({ ...form, annualPriceCrc: event.target.value })
              }
              className="mt-1 w-full rounded-lg border border-sand-300 px-3 py-2 text-sm"
            />
          </label>
        </div>
        <button
          type="submit"
          disabled={createPlan.isPending || updatePlan.isPending}
          className="w-full rounded-lg bg-rescue-600 px-4 py-2 text-sm font-bold text-white disabled:opacity-60"
        >
          {editing ? "Guardar cambios" : "Crear plan"}
        </button>
      </form>
    </div>
  );
}
