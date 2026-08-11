import { useState } from "react";
import { useEmergencyVets } from "@/features/clinics/hooks/useClinics";

// CICT Poison Control — always shown regardless of user location
const POISON_CONTROL = {
  name: "CICT — Control de Intoxicaciones",
  address: "Hospital Nacional de Niños, San José",
  emergencyPhone: "+506 2223-1028",
  isAlways: true,
};

interface EmergencyVetPanelProps {
  /** User's current coordinates — used to sort by distance */
  lat?: number;
  lng?: number;
  /** Collapsed by default; set to false to open on load */
  defaultOpen?: boolean;
}

export function EmergencyVetPanel({ lat, lng, defaultOpen = false }: EmergencyVetPanelProps) {
  const [open, setOpen] = useState(defaultOpen);
  const { data: vets = [] } = useEmergencyVets(lat, lng, 40);

  const totalCount = vets.length + 1; // +1 for CICT

  return (
    <div className="rounded-2xl border border-danger-200 bg-danger-50">
      {/* Header — toggle */}
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-expanded={open}
        className="flex w-full items-center justify-between gap-3 px-4 py-3 text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-danger-400 rounded-2xl"
      >
        <div className="flex items-center gap-2">
          <span className="text-lg" aria-hidden="true">🚨</span>
          <span className="text-sm font-semibold text-danger-800">
            Veterinarias de emergencia
          </span>
          <span className="rounded-full bg-danger-200 px-1.5 py-0.5 text-[10px] font-bold text-danger-700">
            {totalCount}
          </span>
        </div>
        <svg
          viewBox="0 0 16 16"
          fill="currentColor"
          className={`h-4 w-4 text-danger-500 transition-transform ${open ? "rotate-180" : ""}`}
          aria-hidden="true"
        >
          <path d="M4.22 6.22a.75.75 0 0 1 1.06 0L8 8.94l2.72-2.72a.75.75 0 1 1 1.06 1.06l-3.25 3.25a.75.75 0 0 1-1.06 0L4.22 7.28a.75.75 0 0 1 0-1.06Z" />
        </svg>
      </button>

      {open && (
        <ul
          className="divide-y divide-danger-100 px-4 pb-4"
          aria-label="Veterinarias de emergencia cercanas"
        >
          {/* CICT always first */}
          <li className="py-3">
            <div className="flex items-start gap-3">
              <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-danger-100 text-lg" aria-hidden="true">
                ☎️
              </span>
              <div className="min-w-0 flex-1">
                <p className="text-xs font-semibold text-danger-800">{POISON_CONTROL.name}</p>
                <p className="text-xs text-sand-500">{POISON_CONTROL.address}</p>
              </div>
              <a
                href={`tel:${POISON_CONTROL.emergencyPhone}`}
                className="shrink-0 flex items-center gap-1 rounded-xl bg-danger-600 px-3 py-1.5 text-xs font-bold text-white hover:bg-danger-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-danger-400"
                aria-label={`Llamar a ${POISON_CONTROL.name}: ${POISON_CONTROL.emergencyPhone}`}
              >
                <svg viewBox="0 0 16 16" fill="currentColor" className="h-3 w-3" aria-hidden="true">
                  <path fillRule="evenodd" d="M1.885.511a1.745 1.745 0 0 1 2.61.163L6.29 2.98c.329.423.445.974.315 1.494l-.547 2.19a.678.678 0 0 0 .178.643l2.457 2.457a.678.678 0 0 0 .644.178l2.189-.547a1.745 1.745 0 0 1 1.494.315l2.306 1.794c.829.645.905 1.87.163 2.611l-1.034 1.034c-.74.74-1.846 1.065-2.877.702a18.634 18.634 0 0 1-7.01-4.42 18.634 18.634 0 0 1-4.42-7.009c-.362-1.03-.037-2.137.703-2.877L1.885.511Z" clipRule="evenodd" />
                </svg>
                Llamar
              </a>
            </div>
          </li>

          {/* Nearby emergency vets */}
          {vets.length === 0 && (
            <li className="py-3 text-center text-xs text-sand-400">
              No hay veterinarias de emergencia registradas en tu área todavía.
            </li>
          )}
          {vets.map((vet) => {
            const callNumber = vet.emergencyPhone ?? vet.phoneNumber;
            return (
              <li key={vet.id} className="py-3">
                <div className="flex items-start gap-3">
                  {vet.logoUrl ? (
                    <img
                      src={vet.logoUrl}
                      alt={vet.name}
                      className="h-9 w-9 shrink-0 rounded-xl object-cover border border-sand-200"
                    />
                  ) : (
                    <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-trust-100 text-lg" aria-hidden="true">
                      🏥
                    </span>
                  )}
                  <div className="min-w-0 flex-1">
                    <p className="text-xs font-semibold text-sand-900">{vet.name}</p>
                    <p className="text-xs text-sand-500 truncate">{vet.address}</p>
                    {vet.distanceKm != null && (
                      <p className="text-xs text-brand-600 font-medium">
                        📍 {vet.distanceKm.toFixed(1)} km
                      </p>
                    )}
                  </div>
                  {callNumber && (
                    <a
                      href={`tel:${callNumber}`}
                      className="shrink-0 flex items-center gap-1 rounded-xl bg-trust-600 px-3 py-1.5 text-xs font-bold text-white hover:bg-trust-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-trust-400"
                      aria-label={`Llamar a ${vet.name}: ${callNumber}`}
                    >
                      <svg viewBox="0 0 16 16" fill="currentColor" className="h-3 w-3" aria-hidden="true">
                        <path fillRule="evenodd" d="M1.885.511a1.745 1.745 0 0 1 2.61.163L6.29 2.98c.329.423.445.974.315 1.494l-.547 2.19a.678.678 0 0 0 .178.643l2.457 2.457a.678.678 0 0 0 .644.178l2.189-.547a1.745 1.745 0 0 1 1.494.315l2.306 1.794c.829.645.905 1.87.163 2.611l-1.034 1.034c-.74.74-1.846 1.065-2.877.702a18.634 18.634 0 0 1-7.01-4.42 18.634 18.634 0 0 1-4.42-7.009c-.362-1.03-.037-2.137.703-2.877L1.885.511Z" clipRule="evenodd" />
                      </svg>
                      Llamar
                    </a>
                  )}
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
