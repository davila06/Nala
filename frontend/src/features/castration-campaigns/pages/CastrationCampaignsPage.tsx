import { useDeferredValue, useState } from "react";
import { Helmet } from "react-helmet-async";
import { CalendarDays, MapPin, Search, Stethoscope } from "lucide-react";
import { Skeleton } from "@/shared/ui/Spinner";
import { CastrationReservationPanel } from "../components/CastrationReservationPanel";
import { useCastrationCampaigns } from "../hooks/useCastrationCampaigns";

export default function CastrationCampaignsPage() {
  const [canton, setCanton] = useState("");
  const deferredCanton = useDeferredValue(canton);
  const { data, isLoading, isError } = useCastrationCampaigns(deferredCanton, 1);

  return (
    <>
      <Helmet>
        <title>Campañas de castración · PawTrack CR</title>
        <meta name="description" content="Jornadas verificadas de castración y esterilización animal en Costa Rica." />
      </Helmet>
      <main className="mx-auto max-w-5xl px-4 py-8 space-y-6">
        <header className="border-b border-sand-200 pb-6">
          <div className="flex items-center gap-3">
            <span className="grid h-11 w-11 place-items-center rounded-lg bg-rescue-100 text-rescue-700">
              <Stethoscope aria-hidden="true" className="h-6 w-6" />
            </span>
            <div>
              <h1 className="font-display text-2xl font-bold text-sand-900">Campañas de castración</h1>
              <p className="text-sm text-sand-600">
                Jornadas verificadas con cupos, clínica responsable y consentimiento informado.
              </p>
            </div>
          </div>
        </header>

        <label className="relative block max-w-md">
          <Search className="absolute left-3 top-2.5 h-4 w-4 text-sand-400" aria-hidden="true" />
          <span className="sr-only">Filtrar por cantón</span>
          <input
            value={canton}
            onChange={(event) => setCanton(event.target.value)}
            placeholder="Filtrar por cantón"
            className="w-full rounded-lg border border-sand-300 bg-surface py-2 pl-9 pr-3 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-100"
          />
        </label>

        {isLoading ? (
          <div className="grid gap-4 md:grid-cols-2">
            {[1, 2, 3, 4].map((item) => (
              <Skeleton key={item} className="h-52 rounded-lg" />
            ))}
          </div>
        ) : isError ? (
          <p className="rounded-lg border border-danger-200 bg-danger-50 p-4 text-sm text-danger-700">
            No fue posible cargar las campañas.
          </p>
        ) : data?.items.length === 0 ? (
          <p className="border-y border-sand-200 py-12 text-center text-sm text-sand-500">
            No hay campañas con cupos disponibles para este cantón.
          </p>
        ) : (
          <div className="grid gap-4 md:grid-cols-2">
            {data?.items.map((campaign) => (
              <article key={campaign.id} className="rounded-lg border border-sand-200 bg-surface p-5 shadow-sm">
                <div className="flex items-start justify-between gap-3">
                  <h2 className="font-display text-lg font-bold text-sand-900">{campaign.title}</h2>
                  <span className="shrink-0 rounded-full bg-rescue-100 px-2.5 py-1 text-xs font-bold text-rescue-700">
                    {campaign.availableCapacity} cupos
                  </span>
                </div>
                <div className="mt-4 space-y-2 text-sm text-sand-600">
                  <p className="flex gap-2">
                    <MapPin className="h-4 w-4 shrink-0" aria-hidden="true" />
                    {campaign.venueLabel}, {campaign.canton}
                  </p>
                  <p className="flex gap-2">
                    <CalendarDays className="h-4 w-4 shrink-0" aria-hidden="true" />
                    {new Date(campaign.startsAt).toLocaleString("es-CR", { dateStyle: "long", timeStyle: "short" })}
                  </p>
                </div>
                <div className="mt-5 border-t border-sand-200 pt-4">
                  <p className="text-xs text-sand-500">Costo base</p>
                  <p className="text-lg font-black text-brand-700">₡{campaign.basePriceCrc.toLocaleString("es-CR")}</p>
                  <p className="text-xs text-sand-500">Se agrega 13% IVA cuando se solicita Factura Electrónica.</p>
                </div>
                <CastrationReservationPanel campaign={campaign} />
              </article>
            ))}
          </div>
        )}
      </main>
    </>
  );
}
