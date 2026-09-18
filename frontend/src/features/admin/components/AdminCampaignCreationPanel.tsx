import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { CalendarPlus } from "lucide-react";
import { Button } from "@/shared/ui/Button";
import { toast } from "@/shared/lib/toast";
import { adoptionsApi } from "@/features/adoptions/api/adoptionsApi";
import { castrationCampaignsApi } from "@/features/castration-campaigns/api/castrationCampaignsApi";

const inputClass = "w-full rounded-lg border border-sand-300 bg-surface px-3 py-2 text-sm";

export function AdminCampaignCreationPanel() {
  const [mode, setMode] = useState<"adoption" | "castration">("adoption");
  const queryClient = useQueryClient();
  const [title, setTitle] = useState("");
  const [venue, setVenue] = useState("");
  const [canton, setCanton] = useState("");
  const [lat, setLat] = useState("");
  const [lng, setLng] = useState("");
  const [startsAt, setStartsAt] = useState("");
  const [endsAt, setEndsAt] = useState("");
  const [clinicId, setClinicId] = useState("");
  const [reservationsOpenAt, setReservationsOpenAt] = useState("");
  const [reservationsCloseAt, setReservationsCloseAt] = useState("");
  const [capacity, setCapacity] = useState("50");
  const [basePrice, setBasePrice] = useState("0");
  const [description, setDescription] = useState("");

  const creation = useMutation({
    mutationFn: async () => {
      if (mode === "adoption")
        return adoptionsApi.createFair({
          title,
          venueLabel: venue,
          lat: Number(lat),
          lng: Number(lng),
          startsAt: new Date(startsAt).toISOString(),
          endsAt: new Date(endsAt).toISOString(),
          description,
          animalIds: [],
        });
      const campaign = await castrationCampaignsApi.create({
        executingClinicId: clinicId,
        title,
        venueLabel: venue,
        canton,
        latitude: Number(lat),
        longitude: Number(lng),
        startsAt: new Date(startsAt).toISOString(),
        endsAt: new Date(endsAt).toISOString(),
        reservationsOpenAt: new Date(reservationsOpenAt).toISOString(),
        reservationsCloseAt: new Date(reservationsCloseAt).toISOString(),
        capacity: Number(capacity),
        basePriceCrc: Number(basePrice),
        consentVersion: "v1",
      });
      await castrationCampaignsApi.submit(campaign.id);
      await castrationCampaignsApi.approve(campaign.id);
      return castrationCampaignsApi.publish(campaign.id);
    },
    onSuccess: () => {
      toast.success(mode === "adoption" ? "Feria de adopción creada" : "Campaña de castración creada y publicada");
      void queryClient.invalidateQueries({ queryKey: ["adoptions", "fairs"] });
      void queryClient.invalidateQueries({ queryKey: ["castration-campaigns"] });
      setTitle("");
      setVenue("");
      setDescription("");
    },
    onError: () => toast.error("No se pudo crear la campaña. Revisa fechas, permisos y datos."),
  });

  return (
    <section className="border-y border-sand-200 py-5 space-y-4">
      <div className="flex items-center gap-2">
        <CalendarPlus className="h-5 w-5 text-brand-600" />
        <h3 className="font-display text-lg font-bold text-sand-900">Crear campaña</h3>
      </div>
      <div className="inline-flex rounded-lg border border-sand-200 p-1">
        <button
          type="button"
          onClick={() => setMode("adoption")}
          className={`rounded-md px-3 py-1.5 text-sm ${mode === "adoption" ? "bg-brand-600 text-white" : "text-sand-600"}`}
        >
          Adopción
        </button>
        <button
          type="button"
          onClick={() => setMode("castration")}
          className={`rounded-md px-3 py-1.5 text-sm ${mode === "castration" ? "bg-brand-600 text-white" : "text-sand-600"}`}
        >
          Castración
        </button>
      </div>
      <form
        className="grid gap-3 sm:grid-cols-2"
        onSubmit={(event) => {
          event.preventDefault();
          creation.mutate();
        }}
      >
        <input
          required
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="Título"
          className={inputClass}
        />
        <input
          required
          value={venue}
          onChange={(e) => setVenue(e.target.value)}
          placeholder="Lugar"
          className={inputClass}
        />
        {mode === "castration" ? (
          <>
            <input
              required
              value={canton}
              onChange={(e) => setCanton(e.target.value)}
              placeholder="Cantón"
              className={inputClass}
            />
            <input
              required
              value={clinicId}
              onChange={(e) => setClinicId(e.target.value)}
              placeholder="ID clínica ejecutora"
              className={inputClass}
            />
          </>
        ) : null}
        <input
          required
          type="number"
          step="any"
          value={lat}
          onChange={(e) => setLat(e.target.value)}
          placeholder="Latitud"
          className={inputClass}
        />
        <input
          required
          type="number"
          step="any"
          value={lng}
          onChange={(e) => setLng(e.target.value)}
          placeholder="Longitud"
          className={inputClass}
        />
        <label className="text-xs text-sand-600">
          Inicio
          <input
            required
            type="datetime-local"
            value={startsAt}
            onChange={(e) => setStartsAt(e.target.value)}
            className={inputClass}
          />
        </label>
        <label className="text-xs text-sand-600">
          Fin
          <input
            required
            type="datetime-local"
            value={endsAt}
            onChange={(e) => setEndsAt(e.target.value)}
            className={inputClass}
          />
        </label>
        {mode === "castration" ? (
          <>
            <label className="text-xs text-sand-600">
              Apertura reservas
              <input
                required
                type="datetime-local"
                value={reservationsOpenAt}
                onChange={(e) => setReservationsOpenAt(e.target.value)}
                className={inputClass}
              />
            </label>
            <label className="text-xs text-sand-600">
              Cierre reservas
              <input
                required
                type="datetime-local"
                value={reservationsCloseAt}
                onChange={(e) => setReservationsCloseAt(e.target.value)}
                className={inputClass}
              />
            </label>
            <input
              required
              min="1"
              type="number"
              value={capacity}
              onChange={(e) => setCapacity(e.target.value)}
              placeholder="Cupos"
              className={inputClass}
            />
            <input
              required
              min="0"
              type="number"
              value={basePrice}
              onChange={(e) => setBasePrice(e.target.value)}
              placeholder="Precio base CRC"
              className={inputClass}
            />
          </>
        ) : (
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Descripción"
            className={`${inputClass} sm:col-span-2`}
          />
        )}
        <div className="sm:col-span-2">
          <Button type="submit" loading={creation.isPending}>
            Crear {mode === "adoption" ? "feria" : "campaña"}
          </Button>
        </div>
      </form>
    </section>
  );
}
