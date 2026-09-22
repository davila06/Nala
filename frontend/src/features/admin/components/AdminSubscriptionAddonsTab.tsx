import { useState } from "react";
import { adminApi, type SubscriptionAddonDto } from "../api/adminApi";
import { useAdminSubscriptions } from "../hooks/useAdmin";

export function AdminSubscriptionAddonsTab() {
  const { data: subscriptions = [] } = useAdminSubscriptions(false);
  const [selectedId, setSelectedId] = useState(subscriptions[0]?.id ?? "");
  const [addons, setAddons] = useState<SubscriptionAddonDto[]>([]);
  const selected = subscriptions.find((subscription) => subscription.id === selectedId);

  async function loadAddons(id: string) {
    setSelectedId(id);
    setAddons(await adminApi.getSubscriptionAddons(id));
  }

  async function deactivate(id: string) {
    const updated = await adminApi.deactivateSubscriptionAddon(id);
    setAddons((current) => current.map((addon) => (addon.id === updated.id ? updated : addon)));
  }

  return (
    <section className="space-y-4">
      <div className="flex flex-wrap items-center gap-3">
        <label className="text-sm font-semibold text-sand-700" htmlFor="addon-subscription">
          Suscripción
        </label>
        <select
          id="addon-subscription"
          value={selectedId}
          onChange={(event) => void loadAddons(event.target.value)}
          className="rounded-xl border border-sand-200 bg-surface px-3 py-2 text-sm"
        >
          <option value="">Selecciona una suscripción</option>
          {subscriptions.map((subscription) => (
            <option key={subscription.id} value={subscription.id}>
              {subscription.tier} · {subscription.id.slice(0, 8)}
            </option>
          ))}
        </select>
        {selected && <span className="text-xs text-sand-500">{selected.status}</span>}
      </div>
      <div className="overflow-hidden rounded-2xl border border-sand-200 bg-surface">
        <table className="w-full text-left text-sm">
          <thead className="bg-surface-warm text-xs uppercase text-sand-500">
            <tr>
              <th className="px-4 py-3">Entitlement</th>
              <th className="px-4 py-3">Unidades</th>
              <th className="px-4 py-3">Vigencia</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody>
            {addons.map((addon) => (
              <tr key={addon.id} className="border-t border-sand-100">
                <td className="px-4 py-3 font-medium">{addon.entitlementKey}</td>
                <td className="px-4 py-3">+{addon.units}</td>
                <td className="px-4 py-3 text-xs text-sand-500">
                  {new Date(addon.startsAt).toLocaleDateString()} - {new Date(addon.expiresAt).toLocaleDateString()}
                </td>
                <td className="px-4 py-3 text-right">
                  {addon.isActive && (
                    <button
                      type="button"
                      onClick={() => void deactivate(addon.id)}
                      className="text-xs font-semibold text-rose-700"
                    >
                      Desactivar
                    </button>
                  )}
                </td>
              </tr>
            ))}
            {addons.length === 0 && (
              <tr>
                <td colSpan={4} className="px-4 py-8 text-center text-sm text-sand-500">
                  No hay add-ons para esta suscripción.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}
