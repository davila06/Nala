import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useRedeemCollarHandoverCode } from "../hooks/useCollar";

export default function CollarHandoverRedeemPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const [handoverCodeId, setHandoverCodeId] = useState(
    searchParams.get("id") ?? "",
  );
  const [pin, setPin] = useState("");
  const [releasedSerial, setReleasedSerial] = useState<string | null>(null);

  const redeem = useRedeemCollarHandoverCode();

  const handleRedeem = () => {
    redeem.mutate(
      { handoverCodeId, pin },
      { onSuccess: (data) => setReleasedSerial(data.serial) },
    );
  };

  if (releasedSerial) {
    return (
      <div className="mx-auto max-w-md px-4 py-8 space-y-4">
        <div className="rounded-2xl bg-green-50 p-5 text-center">
          <span className="text-4xl" aria-hidden="true">
            ✅
          </span>
          <h1 className="mt-2 text-lg font-bold text-green-800">
            ¡Collar liberado!
          </h1>
          <p className="mt-1 text-sm text-green-700">
            El serial <strong>{releasedSerial}</strong> ya está disponible para
            que lo actives en tu mascota.
          </p>
        </div>
        <button
          type="button"
          onClick={() => navigate(`/collars/activate?serial=${releasedSerial}`)}
          className="w-full rounded-xl bg-brand-600 px-4 py-3 text-sm font-bold text-white hover:bg-brand-700 transition-colors"
        >
          Activar en mi mascota →
        </button>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-md px-4 py-8">
      <h1 className="mb-6 text-xl font-bold text-sand-900">
        Recibir collar transferido
      </h1>
      <div className="space-y-4">
        <p className="text-sm text-sand-600">
          Ingresa el código de transferencia y el PIN de 6 dígitos que te
          compartió el propietario anterior.
        </p>
        <input
          type="text"
          value={handoverCodeId}
          onChange={(e) => setHandoverCodeId(e.target.value.trim())}
          placeholder="ID del código de transferencia"
          className="w-full rounded-xl border border-sand-200 bg-surface px-4 py-3 text-sm outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400"
        />
        <input
          type="text"
          value={pin}
          onChange={(e) =>
            setPin(e.target.value.replace(/\D/g, "").slice(0, 6))
          }
          placeholder="PIN de 6 dígitos"
          maxLength={6}
          className="w-full rounded-xl border border-sand-200 bg-surface px-4 py-3 text-center font-mono text-lg tracking-widest outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400"
        />
        {redeem.isError && (
          <p className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">
            {String(redeem.error)}
          </p>
        )}
        <button
          type="button"
          disabled={redeem.isPending || !handoverCodeId || pin.length !== 6}
          onClick={handleRedeem}
          className="w-full rounded-xl bg-brand-600 px-4 py-3 text-sm font-bold text-white disabled:opacity-40 hover:bg-brand-700 transition-colors"
        >
          {redeem.isPending ? "Verificando…" : "Recibir collar →"}
        </button>
      </div>
    </div>
  );
}
