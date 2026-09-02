import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import { collarApi } from "../api/collarApi";
import { usePets } from "../hooks/usePets";
import { ScanInput } from "@/features/clinics/components/ScanInput";

/** Extract a PT-XXXX-NNNNNNN serial from either a raw serial or a URL containing it. */
function extractSerial(raw: string): string {
  const match = raw.match(/PT-[0-9A-Fa-f]{4}-\d{7}/i);
  return match ? match[0].toUpperCase() : raw.trim().toUpperCase();
}

export default function ActivateCollarTagPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const [serial, setSerial] = useState(searchParams.get("serial") ?? "");
  const [serialInput, setSerialInput] = useState(
    searchParams.get("serial") ?? "",
  );
  const [selectedPetId, setSelectedPetId] = useState("");
  const [step, setStep] = useState<
    "serial" | "pet" | "confirm" | "key" | "done"
  >("serial");
  const [rawKey, setRawKey] = useState<string | null>(null);
  const [keyCopied, setKeyCopied] = useState(false);

  const { data: pets } = usePets();

  const checkSerial = useQuery({
    queryKey: ["collarSerial", serial],
    queryFn: () => collarApi.checkSerial(serial),
    enabled: serial.length > 0,
    retry: false,
  });

  const activate = useMutation({
    mutationFn: () => collarApi.activate(serial, selectedPetId),
    onSuccess: (data) => {
      setRawKey(data.collarApiKey);
      setStep("key");
    },
  });

  const handleCheckSerial = (raw: string = serialInput) => {
    const normalized = extractSerial(raw);
    setSerial(normalized);
    setStep("pet");
  };

  const handleCopyKey = () => {
    if (!rawKey) return;
    navigator.clipboard.writeText(rawKey);
    setKeyCopied(true);
    setTimeout(() => setKeyCopied(false), 2000);
  };

  const isSerialAvailable = checkSerial.data?.available === true;

  return (
    <div className="mx-auto max-w-md px-4 py-8">
      <h1 className="mb-6 text-xl font-bold text-sand-900">
        Activar CollarTag PawTrack
      </h1>

      {/* Step 1 — Serial via QR scan or manual input */}
      {step === "serial" && (
        <div className="space-y-4">
          <p className="text-sm text-sand-600">
            Escanea el QR del collar o ingresa el serial manualmente (ej.
            PT-A3F9-0001234).
          </p>
          <ScanInput
            onScan={(value) => handleCheckSerial(value)}
            isLoading={false}
          />
          <div className="flex items-center gap-2">
            <hr className="flex-1 border-sand-200" />
            <span className="text-[10px] text-sand-400">
              o escribe el serial
            </span>
            <hr className="flex-1 border-sand-200" />
          </div>
          <input
            type="text"
            value={serialInput}
            onChange={(e) => setSerialInput(e.target.value.toUpperCase())}
            placeholder="PT-XXXX-0000000"
            className="w-full rounded-xl border border-sand-200 bg-surface px-4 py-3 font-mono text-sm uppercase tracking-widest focus:outline-none focus:ring-2 focus:ring-brand-400"
            maxLength={15}
          />
          <button
            type="button"
            disabled={serialInput.length < 13}
            onClick={() => handleCheckSerial()}
            className="w-full rounded-xl bg-brand-600 px-4 py-3 text-sm font-bold text-white disabled:opacity-40 hover:bg-brand-700 transition-colors"
          >
            Verificar serial →
          </button>
        </div>
      )}

      {/* Step 2 — Select pet */}
      {step === "pet" && (
        <div className="space-y-4">
          {checkSerial.isLoading && (
            <p className="text-sm text-sand-500">Verificando serial…</p>
          )}
          {checkSerial.isError && (
            <p className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">
              Serial no encontrado. Revisa el número e intenta de nuevo.
            </p>
          )}
          {checkSerial.data && !isSerialAvailable && (
            <p className="rounded-xl bg-amber-50 px-4 py-3 text-sm text-amber-800">
              Este serial ya está activado ({checkSerial.data.status}). Si es
              tuyo, desvincula el collar anterior primero.
            </p>
          )}
          {checkSerial.data && isSerialAvailable && (
            <>
              <p className="rounded-xl bg-green-50 px-4 py-3 text-sm text-green-800">
                ✅ Serial válido y disponible: <strong>{serial}</strong>
              </p>
              <p className="text-sm font-semibold text-sand-800">
                ¿A cuál mascota quieres vincularlo?
              </p>
              <div className="space-y-2">
                {pets?.map((pet) => (
                  <button
                    key={pet.id}
                    type="button"
                    onClick={() => {
                      setSelectedPetId(pet.id);
                      setStep("confirm");
                    }}
                    className="flex w-full items-center gap-3 rounded-xl border border-sand-200 bg-surface px-4 py-3 text-left text-sm hover:border-brand-400 hover:bg-brand-50 transition-colors"
                  >
                    {pet.photoUrl && (
                      <img
                        src={pet.photoUrl}
                        alt={pet.name}
                        className="h-10 w-10 rounded-full object-cover"
                      />
                    )}
                    <span className="font-semibold text-sand-900">
                      {pet.name}
                    </span>
                  </button>
                ))}
              </div>
            </>
          )}
          <button
            type="button"
            onClick={() => setStep("serial")}
            className="text-sm text-sand-400 underline"
          >
            ← Cambiar serial
          </button>
        </div>
      )}

      {/* Step 3 — Confirm */}
      {step === "confirm" && (
        <div className="space-y-4">
          <p className="text-sm text-sand-700">
            Se vinculará el collar <strong>{serial}</strong> a{" "}
            <strong>{pets?.find((p) => p.id === selectedPetId)?.name}</strong>.
            Cualquier collar GPS activo de esa mascota se desconectará.
          </p>
          {activate.isError && (
            <p className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">
              {String(activate.error)}
            </p>
          )}
          <button
            type="button"
            disabled={activate.isPending}
            onClick={() => activate.mutate()}
            className="w-full rounded-xl bg-brand-600 px-4 py-3 text-sm font-bold text-white disabled:opacity-40 hover:bg-brand-700 transition-colors"
          >
            {activate.isPending ? "Activando…" : "Activar CollarTag →"}
          </button>
          <button
            type="button"
            onClick={() => setStep("pet")}
            className="text-sm text-sand-400 underline"
          >
            ← Cambiar mascota
          </button>
        </div>
      )}

      {/* Step 4 — Show raw key once */}
      {step === "key" && rawKey && (
        <div className="space-y-4">
          <div className="rounded-2xl border-2 border-amber-400 bg-amber-50 p-5">
            <p className="text-sm font-bold text-amber-800">
              ⚠️ Guarda esta clave — solo se muestra una vez
            </p>
            <p className="mt-1 text-xs text-amber-700">
              Si configuras el firmware tú mismo, necesitarás esta clave para
              que el collar reporte su posición al servidor.
            </p>
            <div className="mt-3 flex items-center gap-2">
              <code className="flex-1 break-all rounded-lg bg-amber-100 px-3 py-2 text-xs font-mono text-amber-900">
                {rawKey}
              </code>
              <button
                type="button"
                onClick={handleCopyKey}
                className="shrink-0 rounded-lg bg-amber-200 px-3 py-2 text-xs font-bold text-amber-900 hover:bg-amber-300 transition-colors"
              >
                {keyCopied ? "✓" : "Copiar"}
              </button>
            </div>
          </div>
          <button
            type="button"
            onClick={() => {
              setStep("done");
              navigate(`/pets/${selectedPetId}?tab=gps&activated=true`);
            }}
            className="w-full rounded-xl bg-brand-600 px-4 py-3 text-sm font-bold text-white hover:bg-brand-700 transition-colors"
          >
            Continuar al perfil GPS →
          </button>
        </div>
      )}
    </div>
  );
}
