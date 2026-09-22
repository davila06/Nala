import { useEffect, useState } from "react";
import { apiClient } from "@/shared/lib/apiClient";

interface WidgetDomainDto {
  id: string;
  clinicId: string;
  domain: string;
  isActive: boolean;
  createdAt: string;
}

export function AdminClinicWidgetDomainsTab() {
  const [clinicId, setClinicId] = useState("");
  const [domain, setDomain] = useState("");
  const [domains, setDomains] = useState<WidgetDomainDto[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!clinicId) return;
    void apiClient
      .get<WidgetDomainDto[]>(`/admin/clinics/${clinicId}/widget-domains`)
      .then((response) => setDomains(response.data));
  }, [clinicId]);

  async function addDomain() {
    if (!clinicId || !domain.trim()) return;
    try {
      const response = await apiClient.post<WidgetDomainDto>(`/admin/clinics/${clinicId}/widget-domains`, { domain });
      setDomains((current) => [...current, response.data]);
      setDomain("");
      setError(null);
    } catch {
      setError("No se pudo autorizar el dominio.");
    }
  }

  async function removeDomain(id: string) {
    await apiClient.delete(`/admin/clinics/${clinicId}/widget-domains/${id}`);
    setDomains((current) => current.map((item) => (item.id === id ? { ...item, isActive: false } : item)));
  }

  return (
    <section className="space-y-4">
      <div className="flex flex-wrap gap-2">
        <input
          aria-label="ID de clínica"
          value={clinicId}
          onChange={(event) => setClinicId(event.target.value)}
          placeholder="Clinic ID"
          className="rounded-xl border border-sand-200 px-3 py-2 text-sm"
        />
        <input
          aria-label="Dominio del widget"
          value={domain}
          onChange={(event) => setDomain(event.target.value)}
          placeholder="https://example.com"
          className="rounded-xl border border-sand-200 px-3 py-2 text-sm"
        />
        <button
          type="button"
          onClick={() => void addDomain()}
          className="rounded-xl bg-brand-600 px-4 py-2 text-sm font-semibold text-white"
        >
          Autorizar
        </button>
      </div>
      {error && <p className="text-sm text-rose-700">{error}</p>}
      <div className="space-y-2">
        {domains.map((item) => (
          <div
            key={item.id}
            className="flex items-center justify-between rounded-xl border border-sand-200 bg-surface px-4 py-3 text-sm"
          >
            <span className={item.isActive ? "text-sand-800" : "text-sand-400 line-through"}>{item.domain}</span>
            {item.isActive && (
              <button
                type="button"
                onClick={() => void removeDomain(item.id)}
                className="text-xs font-semibold text-rose-700"
              >
                Revocar
              </button>
            )}
          </div>
        ))}
      </div>
    </section>
  );
}
