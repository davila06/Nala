import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Helmet } from "react-helmet-async";
import { apiClient } from "@/shared/lib/apiClient";
import { Skeleton } from "@/shared/ui/Spinner";

interface PassportVerificationResult {
  id: string;
  petName: string;
  petSpecies: string;
  clinicName: string;
  verificationCode: string;
  issuedAt: string;
  validUntil: string | null;
  isRevoked: boolean;
  isValid: boolean;
}

function useVerifyPassport(code: string) {
  return useQuery({
    queryKey: ["verify-passport", code],
    queryFn: () =>
      apiClient
        .get<PassportVerificationResult>(`/certificates/verify/${code}`)
        .then((r) => r.data),
    enabled: code.length === 8,
    retry: false,
  });
}

const SPECIES_EMOJI: Record<string, string> = {
  Dog: "🐶", Cat: "🐱", Bird: "🐦", Rabbit: "🐰", Other: "🐾",
};

export default function PassportVerificationPage() {
  const { code = "" } = useParams<{ code: string }>();
  const { data, isLoading, isError } = useVerifyPassport(code.toUpperCase());

  return (
    <>
      <Helmet>
        <title>Verificar Pasaporte Veterinario — PawTrack CR</title>
        <meta name="description" content="Verifica la autenticidad de un pasaporte veterinario emitido por PawTrack CR." />
      </Helmet>

      <div className="min-h-dvh bg-sand-50 flex items-center justify-center px-4 py-12">
        <div className="w-full max-w-md space-y-6">
          {/* Header */}
          <div className="text-center space-y-1">
            <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-trust-100 text-3xl" aria-hidden="true">
              📋
            </div>
            <h1 className="font-display text-xl font-bold text-sand-900">Verificación de Pasaporte</h1>
            <p className="text-sm text-sand-500">Código: <span className="font-mono font-semibold text-sand-800">{code.toUpperCase()}</span></p>
          </div>

          {isLoading && (
            <div className="space-y-3">
              <Skeleton className="h-40 rounded-2xl" />
              <Skeleton className="h-12 rounded-xl" />
            </div>
          )}

          {isError && (
            <div className="rounded-2xl border border-danger-200 bg-danger-50 p-6 text-center space-y-2">
              <div className="text-4xl" aria-hidden="true">❌</div>
              <p className="font-semibold text-danger-800">Pasaporte no encontrado</p>
              <p className="text-sm text-danger-600">
                El código <span className="font-mono">{code.toUpperCase()}</span> no corresponde a ningún pasaporte emitido en PawTrack CR.
              </p>
            </div>
          )}

          {data && (
            <div className={`rounded-2xl border-2 p-6 space-y-4 ${
              data.isValid && !data.isRevoked
                ? "border-rescue-300 bg-rescue-50"
                : "border-danger-300 bg-danger-50"
            }`}>
              {/* Status badge */}
              <div className="flex items-center gap-2">
                <span className="text-2xl" aria-hidden="true">
                  {data.isRevoked ? "🚫" : data.isValid ? "✅" : "⚠️"}
                </span>
                <span className={`font-display text-lg font-bold ${
                  data.isRevoked ? "text-danger-800" : data.isValid ? "text-rescue-800" : "text-warn-800"
                }`}>
                  {data.isRevoked ? "Pasaporte revocado" : data.isValid ? "Pasaporte válido" : "Pasaporte expirado"}
                </span>
              </div>

              {/* Pet info */}
              <div className="flex items-center gap-3 rounded-xl border border-sand-200 bg-surface p-4">
                <span className="text-3xl" aria-hidden="true">
                  {SPECIES_EMOJI[data.petSpecies] ?? "🐾"}
                </span>
                <div>
                  <p className="font-semibold text-sand-900">{data.petName}</p>
                  <p className="text-xs text-sand-500">{data.petSpecies}</p>
                </div>
              </div>

              {/* Details */}
              <dl className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <dt className="text-sand-500">Clínica emisora</dt>
                  <dd className="font-medium text-sand-900 text-right">{data.clinicName}</dd>
                </div>
                <div className="flex justify-between">
                  <dt className="text-sand-500">Emitido el</dt>
                  <dd className="font-medium text-sand-900">
                    {new Date(data.issuedAt).toLocaleDateString("es-CR")}
                  </dd>
                </div>
                {data.validUntil && (
                  <div className="flex justify-between">
                    <dt className="text-sand-500">Válido hasta</dt>
                    <dd className={`font-medium ${data.isValid ? "text-rescue-700" : "text-danger-700"}`}>
                      {new Date(data.validUntil).toLocaleDateString("es-CR")}
                    </dd>
                  </div>
                )}
              </dl>

              <p className="text-center text-xs text-sand-400 pt-2">
                Verificado mediante PawTrack CR · pawtrack.cr
              </p>
            </div>
          )}
        </div>
      </div>
    </>
  );
}
