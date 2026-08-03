import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "@/shared/lib/toast";
import { Button, Input, Card } from "@/shared/ui";
import { clinicAccessApi } from "@/features/medical/api/clinicAccessApi";

// ── Code display ──────────────────────────────────────────────────────────────

function ClinicGeneratedCodeDisplay({
  rawCode,
  expiresAt,
  onClose,
}: {
  rawCode: string;
  expiresAt: string;
  onClose: () => void;
}) {
  const expires = new Date(expiresAt);
  return (
    <div className="rounded-2xl border-2 border-trust-400 bg-trust-50 p-5 space-y-3">
      <p className="text-center text-xs font-semibold uppercase tracking-wide text-trust-600">
        Muestra este código al propietario
      </p>
      <div className="flex items-center justify-center gap-3">
        <span className="font-mono text-3xl font-black tracking-[0.3em] text-sand-900">
          {rawCode}
        </span>
        <button
          type="button"
          onClick={() => {
            void navigator.clipboard.writeText(rawCode);
            toast.success("Código copiado");
          }}
          className="rounded-lg bg-trust-200 px-2 py-1 text-xs font-semibold text-trust-800 hover:bg-trust-300"
        >
          Copiar
        </button>
      </div>
      <p className="text-center text-xs text-sand-500">
        Válido hasta {expires.toLocaleString("es-CR")}
      </p>
      <p className="rounded-xl bg-trust-100 px-3 py-2 text-xs text-trust-800">
        El propietario lo ingresa en PawTrack → perfil de la mascota → tab
        Salud → "Veterinarias autorizadas" → "Tengo un código".
      </p>
      <Button variant="secondary" onClick={onClose} className="w-full">
        Listo
      </Button>
    </div>
  );
}

// ── Authorized patients list ──────────────────────────────────────────────────

function AuthorizedPatientsList({
  onSelectPet,
}: {
  onSelectPet: (petId: string, petName: string) => void;
}) {
  const { data: pets, isLoading } = useQuery({
    queryKey: ["clinic-authorized-pets"],
    queryFn: clinicAccessApi.getAuthorizedPets,
    staleTime: 30_000,
  });

  if (isLoading)
    return (
      <div className="animate-pulse space-y-2">
        <div className="h-12 rounded-xl bg-sand-100" />
        <div className="h-12 rounded-xl bg-sand-100" />
      </div>
    );

  if (!pets?.length)
    return (
      <Card padding="sm">
        <p className="text-center text-xs text-sand-400">
          Ningún propietario ha autorizado acceso permanente todavía.
        </p>
      </Card>
    );

  return (
    <ul className="space-y-2">
      {pets.map((p) => (
        <li key={p.grantId}>
          <button
            type="button"
            onClick={() => onSelectPet(p.petId, p.petName)}
            className="w-full flex items-center gap-3 rounded-xl border border-sand-100 bg-surface-warm px-4 py-3 text-left hover:bg-sand-50 transition-colors"
          >
            {p.photoUrl && (
              <img
                src={p.photoUrl}
                alt={p.petName}
                className="h-8 w-8 rounded-full object-cover border border-sand-200 shrink-0"
              />
            )}
            <div className="min-w-0 flex-1">
              <p className="text-sm font-semibold text-sand-900 truncate">
                {p.petName}
              </p>
              <p className="text-xs text-sand-500">
                {p.species} · acceso desde{" "}
                {new Date(p.grantedAt).toLocaleDateString("es-CR")}
              </p>
            </div>
            <span className="shrink-0 text-xs text-brand-600 font-semibold">
              Ver →
            </span>
          </button>
        </li>
      ))}
    </ul>
  );
}

// ── Main panel ────────────────────────────────────────────────────────────────

export function ClinicAccessPanel({
  currentPetId,
  onSelectPet,
}: {
  currentPetId?: string | null;
  onSelectPet: (petId: string, petName: string) => void;
}) {
  const qc = useQueryClient();
  const [tab, setTab] = useState<"list" | "generate" | "accept">("list");
  const [generatedCode, setGeneratedCode] = useState<{
    rawCode: string;
    expiresAt: string;
  } | null>(null);
  const [acceptInput, setAcceptInput] = useState("");

  const generateCode = useMutation({
    mutationFn: () => {
      if (!currentPetId) throw new Error("No hay mascota seleccionada");
      return clinicAccessApi.clinicGenerateCode(currentPetId);
    },
    onSuccess: (dto) => setGeneratedCode({ rawCode: dto.rawCode, expiresAt: dto.expiresAt }),
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { detail?: string } } })
        ?.response?.data?.detail;
      toast.error(msg ?? "No se pudo generar el código");
    },
  });

  const acceptCode = useMutation({
    mutationFn: () => clinicAccessApi.clinicAcceptCode(acceptInput),
    onSuccess: (grant) => {
      void qc.invalidateQueries({ queryKey: ["clinic-authorized-pets"] });
      toast.success("¡Acceso activado! Ya puedes ver el expediente.");
      setAcceptInput("");
      setTab("list");
      onSelectPet(grant.petId, grant.clinicName);
    },
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { detail?: string } } })
        ?.response?.data?.detail;
      toast.error(msg ?? "Código inválido o expirado");
    },
  });

  return (
    <div className="space-y-4">
      {/* Sub-tabs */}
      <div className="flex gap-1 rounded-xl bg-surface-warm p-1">
        {(["list", "generate", "accept"] as const).map((t) => (
          <button
            key={t}
            type="button"
            onClick={() => setTab(t)}
            className={[
              "flex-1 rounded-lg py-1.5 text-xs font-bold transition-colors",
              tab === t
                ? "bg-surface text-sand-900 shadow-sm"
                : "text-sand-500 hover:text-sand-700",
            ].join(" ")}
          >
            {t === "list"
              ? "📋 Pacientes"
              : t === "generate"
                ? "🔑 Generar código"
                : "✅ Ingresar código"}
          </button>
        ))}
      </div>

      {tab === "list" && (
        <AuthorizedPatientsList onSelectPet={onSelectPet} />
      )}

      {tab === "generate" && (
        <div className="space-y-3">
          {generatedCode ? (
            <ClinicGeneratedCodeDisplay
              rawCode={generatedCode.rawCode}
              expiresAt={generatedCode.expiresAt}
              onClose={() => { setGeneratedCode(null); setTab("list"); }}
            />
          ) : (
            <div className="space-y-3">
              <p className="text-sm text-sand-600">
                Genera un código para que el propietario de{" "}
                <strong>la mascota actualmente escaneada</strong> lo ingrese en
                su app y te dé acceso permanente al expediente.
              </p>
              {currentPetId ? (
                <Button
                  onClick={() => generateCode.mutate()}
                  loading={generateCode.isPending}
                  className="w-full"
                >
                  Generar código de acceso permanente
                </Button>
              ) : (
                <div className="rounded-xl border border-warn-200 bg-warn-50 p-3 text-xs text-warn-800">
                  Primero escanea una mascota en la pestaña Escanear.
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {tab === "accept" && (
        <div className="space-y-3">
          <p className="text-sm text-sand-600">
            Ingresa el código que el propietario te mostró para activar acceso
            permanente al expediente de su mascota.
          </p>
          <div className="flex gap-2">
            <Input
              value={acceptInput}
              onChange={(e) => setAcceptInput(e.target.value.toUpperCase())}
              maxLength={8}
              placeholder="XXXXXXXX"
              className="flex-1 font-mono tracking-widest text-center"
              aria-label="Código del propietario"
            />
            <Button
              loading={acceptCode.isPending}
              disabled={acceptInput.length !== 8}
              onClick={() => acceptCode.mutate()}
            >
              Activar
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
