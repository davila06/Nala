import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "@/shared/lib/toast";
import { Button, Input, Card } from "@/shared/ui";
import { clinicAccessApi } from "@/features/medical/api/clinicAccessApi";
import type { ClinicAccessGrantDto } from "@/features/medical/api/clinicAccessApi";

// ── Code display with auto-copy and countdown ─────────────────────────────────

function GeneratedCodeDisplay({
  rawCode,
  expiresAt,
  initiatedBy,
  onClose,
}: {
  rawCode: string;
  expiresAt: string;
  initiatedBy: "Owner" | "Clinic";
  onClose: () => void;
}) {
  const expires = new Date(expiresAt);
  const hoursLeft = Math.max(
    0,
    Math.round((expires.getTime() - Date.now()) / 3_600_000),
  );

  return (
    <div className="rounded-2xl border-2 border-trust-400 bg-trust-50 p-5 space-y-4">
      <div className="text-center space-y-1">
        <p className="text-xs font-semibold uppercase tracking-wide text-trust-600">
          {initiatedBy === "Owner"
            ? "Entrega este código a tu veterinaria"
            : "Muestra este código al propietario"}
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
        <p className="text-xs text-sand-500">
          Válido por ~{hoursLeft}h · vence el{" "}
          {expires.toLocaleString("es-CR")}
        </p>
      </div>

      <div className="rounded-xl bg-trust-100 px-3 py-2 text-xs text-trust-800 space-y-1">
        <p className="font-semibold">¿Qué hacer con este código?</p>
        {initiatedBy === "Owner" ? (
          <p>
            Díselo en voz alta o muéstralo en tu pantalla a la recepcionista.
            La clínica lo ingresará en su portal para activar el acceso
            permanente al expediente.
          </p>
        ) : (
          <p>
            El propietario lo ingresa en la app bajo el perfil de la
            mascota → "Veterinarias autorizadas" → "Tengo un código".
          </p>
        )}
      </div>

      <Button variant="secondary" onClick={onClose} className="w-full">
        Listo
      </Button>
    </div>
  );
}

// ── Grant row ─────────────────────────────────────────────────────────────────

function GrantRow({
  grant,
  petId,
}: {
  grant: ClinicAccessGrantDto;
  petId: string;
}) {
  const qc = useQueryClient();
  const revoke = useMutation({
    mutationFn: () => clinicAccessApi.revokeAccess(petId, grant.clinicId),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["pet-clinic-grants", petId] });
      toast.success("Acceso revocado");
    },
    onError: () => toast.error("No se pudo revocar"),
  });
  const [confirmRevoke, setConfirmRevoke] = useState(false);

  const statusBadge = grant.isActive
    ? { label: "Activo", cls: "bg-rescue-100 text-rescue-700" }
    : grant.isPending
      ? { label: "Pendiente", cls: "bg-warn-100 text-warn-700" }
      : { label: "Expirado", cls: "bg-sand-100 text-sand-500" };

  return (
    <li className="flex items-center justify-between gap-3 rounded-xl border border-sand-100 bg-surface-warm px-4 py-3">
      <div className="min-w-0">
        <p className="truncate text-sm font-semibold text-sand-900">
          {grant.clinicName}
        </p>
        <p className="text-xs text-sand-500">
          {grant.isActive && grant.acceptedAt
            ? `Acceso desde ${new Date(grant.acceptedAt).toLocaleDateString("es-CR")}`
            : grant.isPending
              ? `Código expira ${new Date(grant.codeExpiresAt).toLocaleString("es-CR")}`
              : "Código expirado sin activar"}
        </p>
      </div>
      <div className="flex shrink-0 items-center gap-2">
        <span className={`rounded-full px-2 py-0.5 text-xs font-semibold ${statusBadge.cls}`}>
          {statusBadge.label}
        </span>
        {grant.isActive && !confirmRevoke && (
          <button
            type="button"
            onClick={() => setConfirmRevoke(true)}
            className="rounded-lg border border-danger-200 px-2 py-1 text-xs font-semibold text-danger-600 hover:bg-danger-50"
          >
            Revocar
          </button>
        )}
        {grant.isActive && confirmRevoke && (
          <div className="flex gap-1">
            <button
              type="button"
              disabled={revoke.isPending}
              onClick={() => revoke.mutate()}
              className="rounded-lg bg-danger-600 px-2 py-1 text-xs font-bold text-white hover:bg-danger-700 disabled:opacity-50"
            >
              Sí
            </button>
            <button
              type="button"
              onClick={() => setConfirmRevoke(false)}
              className="rounded-lg border border-sand-300 px-2 py-1 text-xs font-semibold text-sand-600"
            >
              No
            </button>
          </div>
        )}
      </div>
    </li>
  );
}

// ── Main component ────────────────────────────────────────────────────────────

export function PetClinicAccessManager({
  petId,
  availableClinics,
}: {
  petId: string;
  availableClinics?: { id: string; name: string }[];
}) {
  const { data: grants, isLoading } = useQuery({
    queryKey: ["pet-clinic-grants", petId],
    queryFn: () => clinicAccessApi.getGrantsForPet(petId),
    staleTime: 30_000,
  });

  const qc = useQueryClient();
  const generateCode = useMutation({
    mutationFn: (clinicId: string) =>
      clinicAccessApi.ownerGenerateCode(petId, clinicId),
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { detail?: string } } })
        ?.response?.data?.detail;
      toast.error(msg ?? "No se pudo generar el código");
    },
  });

  const acceptCode = useMutation({
    mutationFn: (code: string) => clinicAccessApi.ownerAcceptCode(petId, code),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["pet-clinic-grants", petId] });
      toast.success("¡Acceso activado para la clínica!");
      setAcceptInput("");
    },
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { detail?: string } } })
        ?.response?.data?.detail;
      toast.error(msg ?? "Código inválido o expirado");
    },
  });

  const [selectedClinicId, setSelectedClinicId] = useState(
    availableClinics?.[0]?.id ?? "",
  );
  const [generatedCode, setGeneratedCode] =
    useState<{ rawCode: string; expiresAt: string } | null>(null);
  const [showAcceptForm, setShowAcceptForm] = useState(false);
  const [acceptInput, setAcceptInput] = useState("");

  const activeGrants = grants?.filter((g) => g.isActive) ?? [];
  const pendingGrants = grants?.filter((g) => g.isPending && !g.isActive) ?? [];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="font-display text-sm font-semibold text-sand-800">
          🏥 Veterinarias autorizadas
        </h3>
        <button
          type="button"
          onClick={() => setShowAcceptForm((v) => !v)}
          className="rounded-lg border border-sand-300 px-3 py-1.5 text-xs font-semibold text-sand-700 hover:bg-sand-100"
        >
          Tengo un código
        </button>
      </div>

      {/* Accept clinic code form */}
      {showAcceptForm && (
        <div className="rounded-xl border border-trust-200 bg-trust-50 p-3 space-y-2">
          <p className="text-xs font-semibold text-trust-800">
            Ingresa el código que te dio la clínica
          </p>
          <div className="flex gap-2">
            <Input
              value={acceptInput}
              onChange={(e) => setAcceptInput(e.target.value.toUpperCase())}
              maxLength={8}
              placeholder="XXXXXXXX"
              className="flex-1 font-mono tracking-widest"
              aria-label="Código de la clínica"
            />
            <Button
              size="sm"
              loading={acceptCode.isPending}
              disabled={acceptInput.length !== 8}
              onClick={() => acceptCode.mutate(acceptInput)}
            >
              Activar
            </Button>
          </div>
        </div>
      )}

      {/* Generated code display */}
      {generatedCode && (
        <GeneratedCodeDisplay
          rawCode={generatedCode.rawCode}
          expiresAt={generatedCode.expiresAt}
          initiatedBy="Owner"
          onClose={() => setGeneratedCode(null)}
        />
      )}

      {/* Generate code for a specific clinic */}
      {availableClinics && availableClinics.length > 0 && !generatedCode && (
        <div className="rounded-xl border border-sand-200 bg-sand-50 p-3 space-y-2">
          <p className="text-xs font-semibold text-sand-700">
            Autorizar una clínica específica
          </p>
          <div className="flex gap-2">
            <select
              value={selectedClinicId}
              onChange={(e) => setSelectedClinicId(e.target.value)}
              className="flex-1 rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
            >
              {availableClinics.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
            <Button
              size="sm"
              loading={generateCode.isPending}
              disabled={!selectedClinicId}
              onClick={() => {
                generateCode.mutate(selectedClinicId, {
                  onSuccess: (dto) =>
                    setGeneratedCode({
                      rawCode: dto.rawCode,
                      expiresAt: dto.expiresAt,
                    }),
                });
              }}
            >
              Generar código
            </Button>
          </div>
        </div>
      )}

      {/* Active grants list */}
      {isLoading ? (
        <div className="animate-pulse space-y-2">
          <div className="h-12 rounded-xl bg-sand-100" />
          <div className="h-12 rounded-xl bg-sand-100" />
        </div>
      ) : activeGrants.length > 0 ? (
        <ul className="space-y-2">
          {activeGrants.map((g) => (
            <GrantRow key={g.id} grant={g} petId={petId} />
          ))}
        </ul>
      ) : (
        <Card padding="sm">
          <p className="text-center text-xs text-sand-400">
            Ninguna veterinaria tiene acceso permanente aún.
          </p>
        </Card>
      )}

      {/* Pending grants (code generated but not yet accepted) */}
      {pendingGrants.length > 0 && (
        <details>
          <summary className="cursor-pointer text-xs font-semibold text-sand-400 hover:text-sand-600">
            {pendingGrants.length} código{pendingGrants.length !== 1 ? "s" : ""} pendiente
            {pendingGrants.length !== 1 ? "s" : ""}
          </summary>
          <ul className="mt-2 space-y-2">
            {pendingGrants.map((g) => (
              <GrantRow key={g.id} grant={g} petId={petId} />
            ))}
          </ul>
        </details>
      )}
    </div>
  );
}
