import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  certificateApi,
  type ClinicVeterinarianDto,
} from "../api/certificateApi";
import { Button, Input } from "@/shared/ui";
import { toast } from "@/shared/lib/toast";

const PERMISSIONS = [
  ["medical:read", "Leer expedientes"],
  ["medical:write", "Agregar registros"],
  ["medical:export", "Exportar expedientes"],
  ["certificates:issue", "Emitir certificados"],
] as const;

function VeterinarianRow({
  veterinarian,
}: {
  veterinarian: ClinicVeterinarianDto;
}) {
  const queryClient = useQueryClient();
  const [permissions, setPermissions] = useState<string[]>(
    veterinarian.permissions ?? [
      "medical:read",
      "medical:write",
      "certificates:issue",
    ],
  );
  const update = useMutation({
    mutationFn: () =>
      certificateApi.setVeterinarianPermissions(veterinarian.id, permissions),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["clinic-veterinarians"],
      });
      toast.success("Permisos actualizados.");
    },
    onError: () => toast.error("No se pudieron actualizar los permisos."),
  });

  return (
    <article className="space-y-3 rounded-2xl border border-sand-200 bg-surface p-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="font-bold text-sand-900">{veterinarian.fullName}</p>
          <p className="text-xs text-sand-500">
            {veterinarian.licenseNumber} · {veterinarian.status}
          </p>
        </div>
        <span
          className={`rounded-full px-2 py-1 text-[11px] font-bold ${veterinarian.isActive ? "bg-rescue-100 text-rescue-700" : "bg-sand-100 text-sand-500"}`}
        >
          {veterinarian.isActive ? "Activo" : "No activo"}
        </span>
      </div>
      <div className="grid gap-2 sm:grid-cols-2">
        {PERMISSIONS.map(([permission, label]) => (
          <label
            key={permission}
            className="flex items-center gap-2 text-xs text-sand-700"
          >
            <input
              type="checkbox"
              checked={permissions.includes(permission)}
              disabled={!veterinarian.isActive}
              onChange={(event) =>
                setPermissions((current) =>
                  event.target.checked
                    ? [...current, permission]
                    : current.filter((item) => item !== permission),
                )
              }
            />
            {label}
          </label>
        ))}
      </div>
      <Button
        disabled={!veterinarian.isActive || update.isPending}
        onClick={() => update.mutate()}
      >
        {update.isPending ? "Guardando..." : "Guardar permisos"}
      </Button>
    </article>
  );
}

export function ClinicOperationsPanel() {
  const { data: veterinarians = [], isLoading } = useQuery({
    queryKey: ["clinic-veterinarians"],
    queryFn: certificateApi.getMyVeterinarians,
  });
  const [selectedVeterinarian, setSelectedVeterinarian] = useState("");
  const [petId, setPetId] = useState("");
  const [startsAt, setStartsAt] = useState("");
  const [durationMinutes, setDurationMinutes] = useState(30);
  const schedule = useMutation({
    mutationFn: () =>
      certificateApi.scheduleAppointment(selectedVeterinarian, {
        petId,
        startsAt: new Date(startsAt).toISOString(),
        durationMinutes,
      }),
    onSuccess: () => {
      toast.success("Cita agendada.");
      setPetId("");
      setStartsAt("");
    },
    onError: () =>
      toast.error("No se pudo agendar. Verifica solapamientos y permisos."),
  });

  if (isLoading)
    return <div className="h-40 animate-pulse rounded-2xl bg-sand-100" />;

  return (
    <section className="space-y-5">
      <header>
        <h2 className="text-lg font-black text-sand-900">Operación clínica</h2>
        <p className="mt-1 text-sm text-sand-500">
          Administra permisos por veterinario y agenda consultas con protección
          contra solapamientos.
        </p>
      </header>
      <div className="space-y-3">
        <h3 className="text-sm font-bold text-sand-800">
          Veterinarios y permisos
        </h3>
        {veterinarians.length === 0 ? (
          <p className="rounded-xl border border-dashed border-sand-300 p-4 text-sm text-sand-500">
            No hay veterinarios registrados.
          </p>
        ) : (
          veterinarians.map((veterinarian) => (
            <VeterinarianRow
              key={veterinarian.id}
              veterinarian={veterinarian}
            />
          ))
        )}
      </div>
      <div className="space-y-3 rounded-2xl border border-brand-200 bg-brand-50 p-4">
        <h3 className="text-sm font-bold text-brand-900">Agendar consulta</h3>
        <select
          value={selectedVeterinarian}
          onChange={(event) => setSelectedVeterinarian(event.target.value)}
          className="field-input w-full"
        >
          <option value="">Selecciona un veterinario</option>
          {veterinarians
            .filter((v) => v.isActive)
            .map((veterinarian) => (
              <option key={veterinarian.id} value={veterinarian.id}>
                {veterinarian.fullName}
              </option>
            ))}
        </select>
        <Input
          value={petId}
          onChange={(event) => setPetId(event.target.value)}
          placeholder="ID de mascota"
          aria-label="ID de mascota"
        />
        <Input
          type="datetime-local"
          value={startsAt}
          onChange={(event) => setStartsAt(event.target.value)}
          aria-label="Inicio de la consulta"
        />
        <label className="block text-xs font-semibold text-sand-600">
          Duración (minutos)
          <input
            type="number"
            min={15}
            max={480}
            step={15}
            value={durationMinutes}
            onChange={(event) => setDurationMinutes(Number(event.target.value))}
            className="field-input mt-1 w-full"
          />
        </label>
        <Button
          disabled={
            !selectedVeterinarian || !petId || !startsAt || schedule.isPending
          }
          onClick={() => schedule.mutate()}
        >
          {schedule.isPending ? "Agendando..." : "Agendar consulta"}
        </Button>
      </div>
    </section>
  );
}
