import { useState } from "react";
import { Helmet } from "react-helmet-async";
import {
  useStoreLocations,
  useCreateStoreLocation,
  useUpdateStoreLocation,
  useSetLocationActive,
} from "../hooks/useStores";
import { StoreLocationDto } from "../api/storesApi";

interface LocationFormProps {
  initial?: StoreLocationDto;
  onSave: (data: {
    name: string;
    address: string;
    lat: number;
    lng: number;
    phoneNumber?: string;
  }) => void;
  onCancel: () => void;
  loading: boolean;
}

function LocationForm({
  initial,
  onSave,
  onCancel,
  loading,
}: LocationFormProps) {
  const [name, setName] = useState(initial?.name ?? "");
  const [address, setAddress] = useState(initial?.address ?? "");
  const [lat, setLat] = useState(String(initial?.lat ?? "9.93"));
  const [lng, setLng] = useState(String(initial?.lng ?? "-84.08"));
  const [phone, setPhone] = useState(initial?.phoneNumber ?? "");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave({
      name: name.trim(),
      address: address.trim(),
      lat: parseFloat(lat),
      lng: parseFloat(lng),
      phoneNumber: phone.trim() || undefined,
    });
  };

  const field =
    "rounded-lg border border-sand-200 bg-white px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-brand-400";

  return (
    <form
      onSubmit={handleSubmit}
      className="space-y-3 rounded-2xl border border-sand-200 bg-surface p-4"
    >
      <div>
        <label className="block text-xs font-semibold text-sand-700 mb-1">
          Nombre de la sede *
        </label>
        <input
          className={field}
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          maxLength={150}
          placeholder="Ej: Sucursal Centro"
        />
      </div>
      <div>
        <label className="block text-xs font-semibold text-sand-700 mb-1">
          Dirección *
        </label>
        <input
          className={field}
          value={address}
          onChange={(e) => setAddress(e.target.value)}
          required
          maxLength={300}
          placeholder="Ej: Av. Central, San José"
        />
      </div>
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label className="block text-xs font-semibold text-sand-700 mb-1">
            Latitud *
          </label>
          <input
            className={field}
            type="number"
            step="any"
            value={lat}
            onChange={(e) => setLat(e.target.value)}
            required
          />
        </div>
        <div>
          <label className="block text-xs font-semibold text-sand-700 mb-1">
            Longitud *
          </label>
          <input
            className={field}
            type="number"
            step="any"
            value={lng}
            onChange={(e) => setLng(e.target.value)}
            required
          />
        </div>
      </div>
      <div>
        <label className="block text-xs font-semibold text-sand-700 mb-1">
          Teléfono
        </label>
        <input
          className={field}
          value={phone}
          onChange={(e) => setPhone(e.target.value)}
          maxLength={30}
          placeholder="Ej: 2222-3333"
        />
      </div>
      <div className="flex gap-2 justify-end pt-1">
        <button
          type="button"
          onClick={onCancel}
          className="rounded-lg border border-sand-200 px-4 py-2 text-sm text-sand-600 hover:bg-sand-50"
        >
          Cancelar
        </button>
        <button
          type="submit"
          disabled={loading}
          className="rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
        >
          {loading ? "Guardando…" : "Guardar sede"}
        </button>
      </div>
    </form>
  );
}

export default function StoreLocationsPage() {
  const { data: locations, isLoading, error } = useStoreLocations();
  const create = useCreateStoreLocation();
  const update = useUpdateStoreLocation();
  const setActive = useSetLocationActive();
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<StoreLocationDto | null>(null);

  const isPartnerError =
    error && String(error).toLowerCase().includes("partner");

  return (
    <div className="mx-auto max-w-2xl px-4 py-8 space-y-6 animate-fade-in-up">
      <Helmet>
        <title>Sedes — PawTrack CR</title>
      </Helmet>

      <div className="flex items-center justify-between gap-4">
        <h1 className="font-display text-xl font-bold text-sand-900">
          🏪 Sedes
        </h1>
        {!showForm && !editing && !isPartnerError && (
          <button
            onClick={() => setShowForm(true)}
            className="rounded-xl bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700"
          >
            + Nueva sede
          </button>
        )}
      </div>

      {isPartnerError && (
        <div className="rounded-2xl border border-warn-200 bg-warn-50 p-6 text-center">
          <p className="font-semibold text-warn-700">
            Función exclusiva de Tienda Partner
          </p>
          <p className="mt-1 text-sm text-warn-600">
            Las sedes múltiples están disponibles con el plan Tienda Partner
            (₡25,000/mes).
          </p>
        </div>
      )}

      {isLoading && (
        <div className="space-y-3">
          {[...Array(2)].map((_, i) => (
            <div
              key={i}
              className="h-20 animate-pulse rounded-2xl bg-sand-100"
            />
          ))}
        </div>
      )}

      {showForm && (
        <LocationForm
          onSave={(data) =>
            create.mutate(data, { onSuccess: () => setShowForm(false) })
          }
          onCancel={() => setShowForm(false)}
          loading={create.isPending}
        />
      )}

      {editing && (
        <LocationForm
          initial={editing}
          onSave={(data) =>
            update.mutate(
              { id: editing.id, ...data },
              { onSuccess: () => setEditing(null) },
            )
          }
          onCancel={() => setEditing(null)}
          loading={update.isPending}
        />
      )}

      {locations && locations.length === 0 && !showForm && (
        <p className="py-8 text-center text-sm text-sand-400">
          No hay sedes registradas. Agrega la primera.
        </p>
      )}

      {locations && locations.length > 0 && !editing && (
        <ul className="space-y-3">
          {locations.map((loc) => (
            <li
              key={loc.id}
              className="rounded-2xl border border-sand-100 bg-surface p-4"
            >
              <div className="flex items-start justify-between gap-3">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap">
                    <p className="font-semibold text-sand-900">{loc.name}</p>
                    {loc.isPrimary && (
                      <span className="rounded-full bg-brand-100 px-2 py-0.5 text-[10px] font-bold text-brand-700">
                        Principal
                      </span>
                    )}
                    {!loc.isActive && (
                      <span className="rounded-full bg-sand-100 px-2 py-0.5 text-[10px] font-bold text-sand-500">
                        Inactiva
                      </span>
                    )}
                  </div>
                  <p className="mt-0.5 text-sm text-sand-500">{loc.address}</p>
                  {loc.phoneNumber && (
                    <p className="text-xs text-sand-400">{loc.phoneNumber}</p>
                  )}
                </div>
                <div className="flex gap-2 shrink-0">
                  <button
                    onClick={() => setEditing(loc)}
                    className="rounded-lg border border-sand-200 px-3 py-1.5 text-xs font-semibold text-sand-600 hover:bg-sand-50"
                  >
                    Editar
                  </button>
                  {!loc.isPrimary && (
                    <button
                      onClick={() =>
                        setActive.mutate({ id: loc.id, active: !loc.isActive })
                      }
                      disabled={setActive.isPending}
                      className={`rounded-lg border px-3 py-1.5 text-xs font-semibold disabled:opacity-60 ${
                        loc.isActive
                          ? "border-danger-200 text-danger-600 hover:bg-danger-50"
                          : "border-rescue-200 text-rescue-600 hover:bg-rescue-50"
                      }`}
                    >
                      {loc.isActive ? "Desactivar" : "Activar"}
                    </button>
                  )}
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
