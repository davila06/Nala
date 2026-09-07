import { useState } from "react";
import { Helmet } from "react-helmet-async";
import { Button, Input } from "@/shared/ui";
import { toast } from "@/shared/lib/toast";
import { Skeleton } from "@/shared/ui/Spinner";
import {
  SERVICE_MODALITY_LABELS,
  type ServiceModality,
} from "../api/serviceProvidersApi";
import {
  useAddServiceAvailabilityBlock,
  useAddServiceAvailabilityRule,
  useAddProviderService,
  useDeactivateServiceAvailabilityBlock,
  useDeactivateServiceAvailabilityRule,
  useMyProviderServices,
  useSetProviderServiceStatus,
  useServiceAvailabilityBlocks,
  useServiceAvailabilityRules,
  useUpdateProviderService,
} from "../hooks/useServiceProviders";

const modalities = Object.entries(SERVICE_MODALITY_LABELS) as [
  ServiceModality,
  string,
][];
const weekdays = [
  "Domingo",
  "Lunes",
  "Martes",
  "Miercoles",
  "Jueves",
  "Viernes",
  "Sabado",
];

export default function ProviderServicesPage() {
  const { data: services = [], isLoading } = useMyProviderServices();
  const add = useAddProviderService();
  const addBlock = useAddServiceAvailabilityBlock();
  const addAvailability = useAddServiceAvailabilityRule();
  const [showForm, setShowForm] = useState(false);
  const [availability, setAvailability] = useState({
    serviceId: "",
    dayOfWeek: "1",
    startsAtLocalTime: "09:00",
    endsAtLocalTime: "17:00",
  });
  const { data: rules = [] } = useServiceAvailabilityRules(
    availability.serviceId,
  );
  const deactivateRule = useDeactivateServiceAvailabilityRule(
    availability.serviceId,
  );
  const setServiceStatus = useSetProviderServiceStatus();
  const updateService = useUpdateProviderService();
  const [editServiceId, setEditServiceId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState({
    name: "",
    description: "",
    modality: "AtProviderLocation" as ServiceModality,
    durationMinutes: "60",
    priceCrc: "",
    capacity: "1",
  });
  const [block, setBlock] = useState({
    serviceId: "",
    startsAt: "",
    endsAt: "",
    reason: "",
  });
  const { data: blocks = [] } = useServiceAvailabilityBlocks(block.serviceId);
  const deactivateBlock = useDeactivateServiceAvailabilityBlock(
    block.serviceId,
  );
  const [form, setForm] = useState({
    name: "",
    description: "",
    modality: "AtProviderLocation" as ServiceModality,
    durationMinutes: "60",
    priceCrc: "",
    capacity: "1",
  });
  const setField = (key: keyof typeof form, value: string) =>
    setForm((current) => ({ ...current, [key]: value }));
  const submit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!form.name.trim() || !form.description.trim() || form.priceCrc === "")
      return toast.error("Completa nombre, descripcion y precio.");
    add.mutate(
      {
        name: form.name,
        description: form.description,
        modality: form.modality,
        durationMinutes: Number(form.durationMinutes),
        priceCrc: Number(form.priceCrc),
        capacity: Number(form.capacity),
      },
      {
        onSuccess: () => {
          toast.success("Servicio publicado");
          setShowForm(false);
          setForm({
            name: "",
            description: "",
            modality: "AtProviderLocation",
            durationMinutes: "60",
            priceCrc: "",
            capacity: "1",
          });
        },
        onError: () => toast.error("No se pudo publicar el servicio."),
      },
    );
  };
  const submitAvailability = (event: React.FormEvent) => {
    event.preventDefault();
    if (!availability.serviceId) return toast.error("Selecciona un servicio.");
    if (availability.endsAtLocalTime <= availability.startsAtLocalTime)
      return toast.error("La hora final debe ser posterior a la inicial.");
    addAvailability.mutate(
      {
        serviceId: availability.serviceId,
        dayOfWeek: Number(availability.dayOfWeek),
        startsAtLocalTime: availability.startsAtLocalTime,
        endsAtLocalTime: availability.endsAtLocalTime,
      },
      {
        onSuccess: () => toast.success("Horario agregado"),
        onError: () => toast.error("No se pudo guardar el horario."),
      },
    );
  };
  const startEditing = (service: (typeof services)[number]) => {
    setEditServiceId(service.id);
    setEditForm({
      name: service.name,
      description: service.description,
      modality: service.modality,
      durationMinutes: String(service.durationMinutes),
      priceCrc: String(service.priceCrc),
      capacity: String(service.capacity),
    });
  };
  const submitEdit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!editServiceId || !editForm.name.trim() || !editForm.description.trim())
      return;
    updateService.mutate(
      {
        serviceId: editServiceId,
        name: editForm.name,
        description: editForm.description,
        modality: editForm.modality,
        durationMinutes: Number(editForm.durationMinutes),
        priceCrc: Number(editForm.priceCrc),
        capacity: Number(editForm.capacity),
      },
      {
        onSuccess: () => {
          toast.success("Servicio actualizado");
          setEditServiceId(null);
        },
        onError: () => toast.error("No se pudo actualizar el servicio."),
      },
    );
  };
  const submitBlock = (event: React.FormEvent) => {
    event.preventDefault();
    if (
      !block.serviceId ||
      !block.startsAt ||
      !block.endsAt ||
      !block.reason.trim()
    )
      return toast.error("Completa servicio, rango y motivo.");
    if (block.endsAt <= block.startsAt)
      return toast.error("El cierre debe terminar despues de iniciar.");
    addBlock.mutate(
      {
        serviceId: block.serviceId,
        startsAt: `${block.startsAt}:00-06:00`,
        endsAt: `${block.endsAt}:00-06:00`,
        reason: block.reason,
      },
      {
        onSuccess: () => {
          toast.success("Cierre agregado");
          setBlock((current) => ({
            ...current,
            startsAt: "",
            endsAt: "",
            reason: "",
          }));
        },
        onError: () => toast.error("No se pudo crear el cierre."),
      },
    );
  };
  if (isLoading)
    return (
      <div className="mx-auto max-w-3xl p-8">
        <Skeleton className="h-48 rounded-xl" />
      </div>
    );
  return (
    <main className="mx-auto max-w-3xl space-y-5 px-4 py-8">
      <Helmet>
        <title>Mis servicios · PawTrack CR</title>
      </Helmet>
      <div className="flex items-center justify-between">
        <div>
          <h1 className="font-display text-2xl font-semibold text-ink-900">
            Mis servicios
          </h1>
          <p className="text-sm text-sand-600">
            Configura lo que los clientes veran en tu perfil.
          </p>
        </div>
        {!showForm ? (
          <Button size="sm" onClick={() => setShowForm(true)}>
            Agregar servicio
          </Button>
        ) : null}
      </div>
      {showForm ? (
        <form
          onSubmit={submit}
          className="space-y-3 rounded-xl border border-brand-200 bg-brand-50 p-4"
        >
          <label className="block text-sm font-medium text-sand-700">
            Nombre
            <Input
              value={form.name}
              onChange={(event) => setField("name", event.target.value)}
              required
            />
          </label>
          <label className="block text-sm font-medium text-sand-700">
            Modalidad
            <select
              value={form.modality}
              onChange={(event) => setField("modality", event.target.value)}
              className="mt-1 w-full rounded-lg border border-sand-200 bg-surface px-3 py-2"
            >
              {modalities.map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
          </label>
          <label className="block text-sm font-medium text-sand-700">
            Descripcion
            <textarea
              rows={3}
              value={form.description}
              onChange={(event) => setField("description", event.target.value)}
              className="mt-1 w-full rounded-lg border border-sand-200 bg-surface px-3 py-2"
              required
            />
          </label>
          <div className="grid gap-3 sm:grid-cols-3">
            <label className="text-sm font-medium text-sand-700">
              Duracion (min)
              <Input
                type="number"
                min="15"
                max="1440"
                value={form.durationMinutes}
                onChange={(event) =>
                  setField("durationMinutes", event.target.value)
                }
              />
            </label>
            <label className="text-sm font-medium text-sand-700">
              Precio CRC
              <Input
                type="number"
                min="0"
                value={form.priceCrc}
                onChange={(event) => setField("priceCrc", event.target.value)}
              />
            </label>
            <label className="text-sm font-medium text-sand-700">
              Capacidad
              <Input
                type="number"
                min="1"
                max="100"
                value={form.capacity}
                onChange={(event) => setField("capacity", event.target.value)}
              />
            </label>
          </div>
          <div className="flex gap-2">
            <Button type="submit" size="sm" loading={add.isPending}>
              Publicar
            </Button>
            <Button
              type="button"
              size="sm"
              variant="secondary"
              onClick={() => setShowForm(false)}
            >
              Cancelar
            </Button>
          </div>
        </form>
      ) : null}
      {services.length ? (
        <form
          onSubmit={submitAvailability}
          className="space-y-3 rounded-xl border border-trust-200 bg-trust-50 p-4"
        >
          <div>
            <h2 className="font-semibold text-ink-900">
              Disponibilidad semanal
            </h2>
            <p className="text-sm text-sand-600">
              Los clientes solo podran solicitar horarios dentro de estas
              franjas.
            </p>
          </div>
          <div className="grid gap-3 sm:grid-cols-4">
            <label className="text-sm font-medium text-sand-700">
              Servicio
              <select
                value={availability.serviceId}
                onChange={(event) =>
                  setAvailability((current) => ({
                    ...current,
                    serviceId: event.target.value,
                  }))
                }
                className="mt-1 w-full rounded-lg border border-sand-200 bg-surface px-3 py-2"
              >
                <option value="">Selecciona</option>
                {services.map((service) => (
                  <option key={service.id} value={service.id}>
                    {service.name}
                  </option>
                ))}
              </select>
            </label>
            <label className="text-sm font-medium text-sand-700">
              Dia
              <select
                value={availability.dayOfWeek}
                onChange={(event) =>
                  setAvailability((current) => ({
                    ...current,
                    dayOfWeek: event.target.value,
                  }))
                }
                className="mt-1 w-full rounded-lg border border-sand-200 bg-surface px-3 py-2"
              >
                {weekdays.map((day, index) => (
                  <option key={day} value={index}>
                    {day}
                  </option>
                ))}
              </select>
            </label>
            <label className="text-sm font-medium text-sand-700">
              Desde
              <Input
                type="time"
                value={availability.startsAtLocalTime}
                onChange={(event) =>
                  setAvailability((current) => ({
                    ...current,
                    startsAtLocalTime: event.target.value,
                  }))
                }
              />
            </label>
            <label className="text-sm font-medium text-sand-700">
              Hasta
              <Input
                type="time"
                value={availability.endsAtLocalTime}
                onChange={(event) =>
                  setAvailability((current) => ({
                    ...current,
                    endsAtLocalTime: event.target.value,
                  }))
                }
              />
            </label>
          </div>
          <Button type="submit" size="sm" loading={addAvailability.isPending}>
            Agregar horario
          </Button>
          {availability.serviceId ? (
            <ul className="space-y-2 border-t border-trust-100 pt-3">
              {rules.map((rule) => (
                <li
                  key={rule.id}
                  className="flex items-center justify-between gap-3 text-sm text-sand-700"
                >
                  <span>
                    {weekdays[rule.dayOfWeek]} ·{" "}
                    {rule.startsAtLocalTime.slice(0, 5)}–
                    {rule.endsAtLocalTime.slice(0, 5)}{" "}
                    {!rule.isActive ? "(inactivo)" : ""}
                  </span>
                  {rule.isActive ? (
                    <button
                      type="button"
                      className="text-xs font-semibold text-danger-600 hover:underline"
                      disabled={deactivateRule.isPending}
                      onClick={() =>
                        deactivateRule.mutate(rule.id, {
                          onSuccess: () => toast.success("Horario desactivado"),
                          onError: () =>
                            toast.error("No se pudo desactivar el horario."),
                        })
                      }
                    >
                      Desactivar
                    </button>
                  ) : null}
                </li>
              ))}
            </ul>
          ) : null}
        </form>
      ) : null}
      {services.length ? (
        <form
          onSubmit={submitBlock}
          className="space-y-3 rounded-xl border border-warn-200 bg-warn-50 p-4"
        >
          <div>
            <h2 className="font-semibold text-ink-900">
              Cierres excepcionales
            </h2>
            <p className="text-sm text-sand-600">
              Bloquea fechas u horas puntuales sin cambiar tu horario semanal.
            </p>
          </div>
          <div className="grid gap-3 sm:grid-cols-4">
            <label className="text-sm font-medium text-sand-700">
              Servicio
              <select
                value={block.serviceId}
                onChange={(event) =>
                  setBlock((current) => ({
                    ...current,
                    serviceId: event.target.value,
                  }))
                }
                className="mt-1 w-full rounded-lg border border-sand-200 bg-surface px-3 py-2"
              >
                <option value="">Selecciona</option>
                {services.map((service) => (
                  <option key={service.id} value={service.id}>
                    {service.name}
                  </option>
                ))}
              </select>
            </label>
            <label className="text-sm font-medium text-sand-700">
              Desde
              <Input
                type="datetime-local"
                value={block.startsAt}
                onChange={(event) =>
                  setBlock((current) => ({
                    ...current,
                    startsAt: event.target.value,
                  }))
                }
              />
            </label>
            <label className="text-sm font-medium text-sand-700">
              Hasta
              <Input
                type="datetime-local"
                value={block.endsAt}
                onChange={(event) =>
                  setBlock((current) => ({
                    ...current,
                    endsAt: event.target.value,
                  }))
                }
              />
            </label>
            <label className="text-sm font-medium text-sand-700">
              Motivo
              <Input
                value={block.reason}
                onChange={(event) =>
                  setBlock((current) => ({
                    ...current,
                    reason: event.target.value,
                  }))
                }
                placeholder="Feriado"
              />
            </label>
          </div>
          <Button type="submit" size="sm" loading={addBlock.isPending}>
            Bloquear horario
          </Button>
          {block.serviceId && blocks.length > 0 ? (
            <ul className="space-y-1 border-t border-warn-200 pt-3 text-sm text-sand-700">
              {blocks
                .filter((item) => item.isActive)
                .map((item) => (
                  <li
                    key={item.id}
                    className="flex items-center justify-between gap-3"
                  >
                    <span>
                      {new Date(item.startsAt).toLocaleString("es-CR", {
                        dateStyle: "medium",
                        timeStyle: "short",
                      })}{" "}
                      -{" "}
                      {new Date(item.endsAt).toLocaleTimeString("es-CR", {
                        timeStyle: "short",
                      })}
                      : {item.reason}
                    </span>
                    <button
                      type="button"
                      className="text-xs font-semibold text-danger-600 hover:underline"
                      disabled={deactivateBlock.isPending}
                      onClick={() =>
                        deactivateBlock.mutate(item.id, {
                          onSuccess: () => toast.success("Cierre desactivado"),
                          onError: () =>
                            toast.error("No se pudo desactivar el cierre."),
                        })
                      }
                    >
                      Reabrir horario
                    </button>
                  </li>
                ))}
            </ul>
          ) : null}
        </form>
      ) : null}
      {services.length ? (
        <ul className="space-y-3">
          {services.map((service) => (
            <li
              key={service.id}
              className="rounded-xl border border-sand-100 bg-surface p-4"
            >
              {editServiceId === service.id ? (
                <form onSubmit={submitEdit} className="space-y-3">
                  <label className="block text-sm font-medium text-sand-700">
                    Nombre
                    <Input
                      value={editForm.name}
                      onChange={(event) =>
                        setEditForm((current) => ({
                          ...current,
                          name: event.target.value,
                        }))
                      }
                      required
                    />
                  </label>
                  <label className="block text-sm font-medium text-sand-700">
                    Modalidad
                    <select
                      value={editForm.modality}
                      onChange={(event) =>
                        setEditForm((current) => ({
                          ...current,
                          modality: event.target.value as ServiceModality,
                        }))
                      }
                      className="mt-1 w-full rounded-lg border border-sand-200 bg-surface px-3 py-2"
                    >
                      {modalities.map(([value, label]) => (
                        <option key={value} value={value}>
                          {label}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label className="block text-sm font-medium text-sand-700">
                    Descripcion
                    <textarea
                      rows={3}
                      value={editForm.description}
                      onChange={(event) =>
                        setEditForm((current) => ({
                          ...current,
                          description: event.target.value,
                        }))
                      }
                      className="mt-1 w-full rounded-lg border border-sand-200 px-3 py-2"
                      required
                    />
                  </label>
                  <div className="grid gap-3 sm:grid-cols-3">
                    <label className="text-sm font-medium text-sand-700">
                      Duracion
                      <Input
                        type="number"
                        min="15"
                        max="1440"
                        value={editForm.durationMinutes}
                        onChange={(event) =>
                          setEditForm((current) => ({
                            ...current,
                            durationMinutes: event.target.value,
                          }))
                        }
                      />
                    </label>
                    <label className="text-sm font-medium text-sand-700">
                      Precio CRC
                      <Input
                        type="number"
                        min="0"
                        value={editForm.priceCrc}
                        onChange={(event) =>
                          setEditForm((current) => ({
                            ...current,
                            priceCrc: event.target.value,
                          }))
                        }
                      />
                    </label>
                    <label className="text-sm font-medium text-sand-700">
                      Capacidad
                      <Input
                        type="number"
                        min="1"
                        max="100"
                        value={editForm.capacity}
                        onChange={(event) =>
                          setEditForm((current) => ({
                            ...current,
                            capacity: event.target.value,
                          }))
                        }
                      />
                    </label>
                  </div>
                  <div className="flex gap-2">
                    <Button
                      type="submit"
                      size="sm"
                      loading={updateService.isPending}
                    >
                      Guardar
                    </Button>
                    <Button
                      type="button"
                      size="sm"
                      variant="secondary"
                      onClick={() => setEditServiceId(null)}
                    >
                      Cancelar
                    </Button>
                  </div>
                </form>
              ) : (
                <>
                  <div className="flex flex-wrap items-start justify-between gap-2">
                    <div>
                      <p className="font-semibold text-ink-900">
                        {service.name}
                      </p>
                      <p className="text-sm text-brand-600">
                        {SERVICE_MODALITY_LABELS[service.modality]} ·{" "}
                        {service.durationMinutes} min · capacidad{" "}
                        {service.capacity}
                      </p>
                    </div>
                    <p className="font-semibold text-rescue-700">
                      CRC {service.priceCrc.toLocaleString("es-CR")}
                    </p>
                  </div>
                  <p className="mt-2 text-sm text-sand-600">
                    {service.description}
                  </p>
                  <div className="mt-3 flex flex-wrap gap-2">
                    {service.status !== "Archived" ? (
                      <Button
                        size="sm"
                        variant="secondary"
                        onClick={() => startEditing(service)}
                      >
                        Editar
                      </Button>
                    ) : null}
                    {service.status === "Published" ? (
                      <Button
                        size="sm"
                        variant="secondary"
                        loading={setServiceStatus.isPending}
                        onClick={() =>
                          setServiceStatus.mutate(
                            { serviceId: service.id, status: "Paused" },
                            {
                              onSuccess: () =>
                                toast.success("Servicio pausado"),
                              onError: () =>
                                toast.error("No se pudo pausar el servicio."),
                            },
                          )
                        }
                      >
                        Pausar
                      </Button>
                    ) : null}
                    {service.status === "Paused" ? (
                      <Button
                        size="sm"
                        loading={setServiceStatus.isPending}
                        onClick={() =>
                          setServiceStatus.mutate(
                            { serviceId: service.id, status: "Published" },
                            {
                              onSuccess: () =>
                                toast.success("Servicio publicado"),
                              onError: () =>
                                toast.error("No se pudo publicar el servicio."),
                            },
                          )
                        }
                      >
                        Publicar
                      </Button>
                    ) : null}
                    {service.status !== "Archived" ? (
                      <Button
                        size="sm"
                        variant="danger"
                        loading={setServiceStatus.isPending}
                        onClick={() =>
                          setServiceStatus.mutate(
                            { serviceId: service.id, status: "Archived" },
                            {
                              onSuccess: () =>
                                toast.success("Servicio archivado"),
                              onError: () =>
                                toast.error("No se pudo archivar el servicio."),
                            },
                          )
                        }
                      >
                        Archivar
                      </Button>
                    ) : null}
                  </div>
                </>
              )}
            </li>
          ))}
        </ul>
      ) : (
        <p className="py-10 text-center text-sm text-sand-500">
          Aun no has publicado servicios.
        </p>
      )}
    </main>
  );
}
