import { useState } from "react";
import { toast } from "@/shared/lib/toast";
import { Button, Input, Card } from "@/shared/ui";
import { PlanGate } from "@/features/pets/components/PlanGate";
import { useMyEntitlements } from "@/features/pets/hooks/useSubscription";
import { useMyFamily, useCreateFamilyAccount, useInviteFamilyMember, useRemoveFamilyMember } from "../hooks/useFamily";
import { useAuthStore } from "@/features/auth/store/authStore";
import type { FamilyMemberDto } from "../api/familyApi";

function MemberRow({
  member,
  currentUserId,
  isOwner,
}: {
  member: FamilyMemberDto;
  currentUserId: string | undefined;
  isOwner: boolean;
}) {
  const remove = useRemoveFamilyMember();
  const isSelf = member.userId === currentUserId;

  return (
    <li className="flex items-center justify-between gap-3 rounded-xl border border-sand-100 bg-surface-warm px-4 py-3">
      <div className="min-w-0">
        <p className="truncate text-sm font-semibold text-sand-900">
          {member.name}
          {isSelf && (
            <span className="ml-2 rounded-full bg-brand-100 px-2 py-0.5 text-xs font-medium text-brand-700">Tú</span>
          )}
        </p>
        <p className="truncate text-xs text-sand-500">{member.email}</p>
      </div>
      <div className="flex shrink-0 items-center gap-2">
        <span
          className={`rounded-full px-2 py-0.5 text-xs font-semibold ${
            member.role === "Owner" ? "bg-trust-100 text-trust-700" : "bg-sand-100 text-sand-600"
          }`}
        >
          {member.role === "Owner" ? "Titular" : "Miembro"}
        </span>
        {isOwner && !isSelf && (
          <button
            type="button"
            disabled={remove.isPending}
            onClick={() => {
              remove.mutate(member.userId, {
                onSuccess: () => toast.success("Miembro eliminado"),
                onError: () => toast.error("No se pudo eliminar"),
              });
            }}
            className="rounded-lg border border-danger-200 px-2 py-1 text-xs font-semibold text-danger-600 hover:bg-danger-50 disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-danger-400"
          >
            Quitar
          </button>
        )}
      </div>
    </li>
  );
}

function ExistingFamily({
  family,
  maxMembers,
}: {
  family: NonNullable<ReturnType<typeof useMyFamily>["data"]>;
  maxMembers: number | null;
}) {
  const invite = useInviteFamilyMember();
  const currentUserId = useAuthStore((s) => s.user?.id);
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteResult, setInviteResult] = useState<{
    link: string;
    email: string;
  } | null>(null);

  const isOwner = family.members.find((m) => m.userId === currentUserId)?.role === "Owner";
  const occupiedSeats = family.members.length + family.pendingInvitations;
  const hasAvailableSeat = maxMembers !== null && occupiedSeats < maxMembers;
  const canInvite = isOwner && hasAvailableSeat && family.pendingInvitations < family.pendingInvitationLimit;

  const handleInvite = () => {
    if (!inviteEmail.trim()) return;
    invite.mutate(inviteEmail.trim(), {
      onSuccess: (inv) => {
        const link = `${window.location.origin}/familia/invitacion/${inv.token}`;
        setInviteResult({ link, email: inviteEmail.trim() });
        setInviteEmail("");
        toast.success(`Invitación enviada a ${inviteEmail.trim()}`);
      },
      onError: () => toast.error("No se pudo enviar la invitación"),
    });
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="font-display text-base font-semibold text-sand-800">👨‍👩‍👧 {family.name}</h3>
        <span className="text-xs text-sand-500">
          {maxMembers === null ? "Límite no disponible" : `${occupiedSeats}/${maxMembers} miembros`}
        </span>
      </div>

      {family.pendingInvitations > 0 && (
        <p className="text-xs text-sand-500" role="status">
          {family.pendingInvitations} invitaciones pendientes ocupan espacio.
        </p>
      )}

      <ul className="space-y-2">
        {family.members.map((m) => (
          <MemberRow key={m.userId} member={m} currentUserId={currentUserId} isOwner={isOwner ?? false} />
        ))}
      </ul>

      {canInvite && (
        <div className="space-y-2 rounded-2xl border border-sand-200 bg-sand-50 p-4">
          <p className="text-sm font-semibold text-sand-700">Invitar miembro</p>
          <div className="flex gap-2">
            <Input
              type="email"
              placeholder="correo@ejemplo.com"
              value={inviteEmail}
              onChange={(e) => setInviteEmail(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") handleInvite();
              }}
              aria-label="Correo del nuevo miembro"
              className="flex-1"
            />
            <Button size="sm" onClick={handleInvite} loading={invite.isPending} disabled={!inviteEmail.trim()}>
              Invitar
            </Button>
          </div>

          {inviteResult && (
            <div className="rounded-xl border border-trust-200 bg-trust-50 p-3">
              <p className="mb-1 text-xs font-semibold text-trust-700">✅ Envía este enlace a {inviteResult.email}:</p>
              <div className="flex items-center gap-2">
                <code className="min-w-0 flex-1 truncate rounded bg-trust-100 px-2 py-1 text-xs text-trust-900">
                  {inviteResult.link}
                </code>
                <button
                  type="button"
                  onClick={() => {
                    void navigator.clipboard.writeText(inviteResult.link);
                    toast.success("¡Copiado!");
                  }}
                  className="shrink-0 rounded-lg bg-trust-600 px-2 py-1 text-xs font-semibold text-white hover:bg-trust-700"
                >
                  Copiar
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {isOwner && maxMembers !== null && !hasAvailableSeat && (
        <p className="text-xs text-sand-600" role="status">
          No hay espacios disponibles. Quita un miembro o revisa tu plan para invitar a otra persona.
        </p>
      )}

      {isOwner &&
        maxMembers !== null &&
        hasAvailableSeat &&
        family.pendingInvitations >= family.pendingInvitationLimit && (
          <p className="text-xs text-sand-600" role="status">
            Espera a que se acepten o expiren las invitaciones pendientes antes de enviar otra.
          </p>
        )}
    </div>
  );
}

function CreateFamilyForm({ maxMembers }: { maxMembers: number | null }) {
  const create = useCreateFamilyAccount();
  const [name, setName] = useState("");

  const handleCreate = () => {
    if (!name.trim()) return;
    create.mutate(name.trim(), {
      onSuccess: () => toast.success("¡Cuenta familiar creada!"),
      onError: (err: unknown) =>
        toast.error(
          (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail ??
            "No se pudo crear la cuenta",
        ),
    });
  };

  return (
    <div className="rounded-2xl border border-sand-200 bg-sand-50 p-4 space-y-3">
      <p className="text-sm text-sand-600">
        {maxMembers === null
          ? "No se pudo consultar el límite de miembros. El servidor verificará el cupo al crear la cuenta."
          : `Con el plan Familia puedes agregar hasta ${Math.max(0, maxMembers - 1)} miembros adicionales, incluido el titular. Los miembros comparten acceso al historial médico según los permisos actuales.`}
      </p>
      <div className="flex gap-2">
        <Input
          placeholder="Nombre de la familia (ej. Familia García)"
          value={name}
          onChange={(e) => setName(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") handleCreate();
          }}
          aria-label="Nombre de la cuenta familiar"
          className="flex-1"
        />
        <Button size="sm" onClick={handleCreate} loading={create.isPending} disabled={!name.trim()}>
          Crear
        </Button>
      </div>
    </div>
  );
}

export function FamilyManagementSection() {
  const { data: family, isLoading, isError } = useMyFamily();
  const {
    data: entitlementSnapshot,
    isLoading: isLoadingEntitlements,
    isError: isEntitlementsError,
  } = useMyEntitlements();
  const maxMembers =
    isLoadingEntitlements || isEntitlementsError
      ? null
      : (entitlementSnapshot?.entitlements.MaxFamilyMembers?.numericValue ?? null);

  return (
    <PlanGate requires="Familia">
      <Card padding="md" className="space-y-4">
        <h2 className="font-display text-lg font-semibold text-sand-900">Cuenta familiar</h2>

        {isLoading && (
          <div className="animate-pulse space-y-2">
            <div className="h-10 rounded-xl bg-sand-100" />
            <div className="h-10 rounded-xl bg-sand-100" />
          </div>
        )}

        {!isLoading && (isError || !family) && <CreateFamilyForm maxMembers={maxMembers} />}

        {!isLoading && family && <ExistingFamily family={family} maxMembers={maxMembers} />}
      </Card>
    </PlanGate>
  );
}
