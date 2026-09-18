import { useState } from "react";
import { Helmet } from "react-helmet-async";
import { ShieldCheck } from "lucide-react";
import { useMutation } from "@tanstack/react-query";
import { Button } from "@/shared/ui/Button";
import { toast } from "@/shared/lib/toast";
import { superAdminApi } from "../api/superAdminApi";

export default function SuperAdminPage() {
  const [userId, setUserId] = useState("");
  const [reason, setReason] = useState("");
  const [mfaCode, setMfaCode] = useState("");
  const assignment = useMutation({
    mutationFn: () => superAdminApi.assignRole(userId.trim(), reason.trim(), mfaCode),
    onSuccess: () => {
      toast.success("Rol SuperAdmin asignado y auditado");
      setUserId("");
      setReason("");
      setMfaCode("");
    },
    onError: () => toast.error("No se pudo asignar el rol. Verifica MFA, correo y autorización."),
  });
  const revocation = useMutation({
    mutationFn: () => superAdminApi.revokeRole(userId.trim(), reason.trim(), mfaCode),
    onSuccess: () => {
      toast.success("Rol SuperAdmin revocado; la cuenta conserva rol Admin");
      setUserId("");
      setReason("");
      setMfaCode("");
    },
    onError: () => toast.error("No se pudo revocar. No puedes revocarte ni eliminar el último SuperAdmin."),
  });

  return (
    <main className="mx-auto max-w-2xl px-4 py-8 space-y-6">
      <Helmet>
        <title>Acceso privilegiado · PawTrack CR</title>
      </Helmet>
      <header className="flex gap-3 border-b border-sand-200 pb-5">
        <span className="grid h-11 w-11 place-items-center rounded-lg bg-danger-100 text-danger-700">
          <ShieldCheck className="h-6 w-6" />
        </span>
        <div>
          <h1 className="font-display text-2xl font-bold text-sand-900">Acceso privilegiado</h1>
          <p className="text-sm text-sand-500">Elevaciones excepcionales con MFA y auditoría inmutable.</p>
        </div>
      </header>
      <form
        className="space-y-4 rounded-lg border border-danger-200 bg-surface p-5"
        onSubmit={(event) => {
          event.preventDefault();
          assignment.mutate();
        }}
      >
        <div>
          <label htmlFor="target-user" className="mb-1 block text-sm font-semibold text-sand-800">
            ID del usuario objetivo
          </label>
          <input
            id="target-user"
            required
            value={userId}
            onChange={(e) => setUserId(e.target.value)}
            placeholder="00000000-0000-0000-0000-000000000000"
            className="w-full rounded-lg border border-sand-300 p-2.5 font-mono text-sm"
          />
        </div>
        <div>
          <label htmlFor="elevation-reason" className="mb-1 block text-sm font-semibold text-sand-800">
            Motivo y referencia de aprobación
          </label>
          <textarea
            id="elevation-reason"
            required
            minLength={10}
            maxLength={500}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="Acta, ticket o aprobación del control dual"
            className="min-h-28 w-full rounded-lg border border-sand-300 p-2.5 text-sm"
          />
        </div>
        <div>
          <label htmlFor="elevation-mfa" className="mb-1 block text-sm font-semibold text-sand-800">
            Código MFA actual
          </label>
          <input
            id="elevation-mfa"
            required
            inputMode="numeric"
            pattern="[0-9]{6}"
            maxLength={6}
            autoComplete="one-time-code"
            value={mfaCode}
            onChange={(e) => setMfaCode(e.target.value.replace(/\D/g, ""))}
            className="w-40 rounded-lg border border-sand-300 p-2.5 font-mono text-sm"
          />
        </div>
        <p className="text-xs text-danger-700">
          La cuenta objetivo debe tener correo verificado y MFA activo. La acción no puede deshacerse desde esta
          pantalla.
        </p>
        <div className="flex flex-wrap gap-2">
          <Button type="submit" variant="danger" loading={assignment.isPending}>
            Asignar SuperAdmin
          </Button>
          <Button
            type="button"
            variant="secondary"
            loading={revocation.isPending}
            onClick={() => {
              if (window.confirm("¿Revocar SuperAdmin y conservar solo el rol Admin?")) revocation.mutate();
            }}
          >
            Revocar a Admin
          </Button>
        </div>
      </form>
    </main>
  );
}
