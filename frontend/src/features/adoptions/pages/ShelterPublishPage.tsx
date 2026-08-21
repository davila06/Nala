import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import { Button } from "@/shared/ui/Button";
import { Input } from "@/shared/ui/Input";
import { usePublishAnimal } from "../hooks/useAdoptions";
import type { PetSpecies, PetSize, AgeCategory, PublishAnimalPayload } from "../api/adoptionsApi";
import { SPECIES_LABELS, SIZE_LABELS, AGE_LABELS } from "../api/adoptionsApi";
import { toast } from "@/shared/lib/toast";

const INITIAL: PublishAnimalPayload = {
  name: "", species: "Dog", size: "Medium", ageCategory: "Young",
  ageMonthsApprox: null, story: "", requirements: null, medicalNotes: null, breed: null,
  isVaccinated: false, isSterilized: false, isMicrochipped: false,
  okWithKids: false, okWithDogs: false, okWithCats: false, needsYard: false,
  refLat: 9.9281, refLng: -84.0907, refLabel: "San José, Costa Rica",
};

export default function ShelterPublishPage() {
  const navigate = useNavigate();
  const publish = usePublishAnimal();
  const [form, setForm] = useState<PublishAnimalPayload>(INITIAL);

  const set = (partial: Partial<PublishAnimalPayload>) =>
    setForm((f) => ({ ...f, ...partial }));

  const handleSubmit = () => {
    if (!form.name.trim() || !form.story.trim()) {
      toast.error("El nombre y la historia son requeridos");
      return;
    }
    publish.mutate(form, {
      onSuccess: (animal) => {
        toast.success(`¡${animal.name} publicado! Ahora puedes subir fotos.`);
        navigate("/shelter/dashboard");
      },
    });
  };

  return (
    <>
      <Helmet><title>Publicar animal · PawTrack CR</title></Helmet>

      <div className="mx-auto max-w-xl px-4 py-8 space-y-6">
        <h1 className="text-xl font-bold text-ink-900">Publicar animal en adopción</h1>

        {/* Basic info */}
        <section className="space-y-4">
          <h2 className="text-sm font-semibold text-ink-700 border-b border-sand-100 pb-2">
            Información básica
          </h2>

          <Input
            label="Nombre *"
            value={form.name}
            onChange={(e) => set({ name: e.target.value })}
            maxLength={80}
            placeholder="Max, Luna, etc."
          />

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs text-sand-500 mb-1">Especie *</label>
              <select
                value={form.species}
                onChange={(e) => set({ species: e.target.value as PetSpecies })}
                className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
              >
                {(Object.entries(SPECIES_LABELS) as [PetSpecies, string][]).map(([v, l]) => (
                  <option key={v} value={v}>{l}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs text-sand-500 mb-1">Tamaño *</label>
              <select
                value={form.size}
                onChange={(e) => set({ size: e.target.value as PetSize })}
                className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
              >
                {(Object.entries(SIZE_LABELS) as [PetSize, string][]).map(([v, l]) => (
                  <option key={v} value={v}>{l}</option>
                ))}
              </select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs text-sand-500 mb-1">Categoría de edad *</label>
              <select
                value={form.ageCategory}
                onChange={(e) => set({ ageCategory: e.target.value as AgeCategory })}
                className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
              >
                {(Object.entries(AGE_LABELS) as [AgeCategory, string][]).map(([v, l]) => (
                  <option key={v} value={v}>{l}</option>
                ))}
              </select>
            </div>
            <Input
              label="Raza (opcional)"
              value={form.breed ?? ""}
              onChange={(e) => set({ breed: e.target.value || null })}
              placeholder="Ej: Labrador"
            />
          </div>
        </section>

        {/* Story */}
        <section className="space-y-3">
          <h2 className="text-sm font-semibold text-ink-700 border-b border-sand-100 pb-2">
            Historia y personalidad
          </h2>
          <div>
            <label className="block text-xs text-sand-500 mb-1">Historia *</label>
            <textarea
              value={form.story}
              onChange={(e) => set({ story: e.target.value })}
              maxLength={2000}
              rows={5}
              placeholder="Cuéntanos cómo llegó, cómo es su personalidad, qué necesidades especiales tiene…"
              className="w-full rounded-xl border border-sand-200 px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400 resize-none"
            />
            <p className="text-right text-xs text-sand-400 mt-1">{form.story.length}/2000</p>
          </div>
          <div>
            <label className="block text-xs text-sand-500 mb-1">Requisitos para el adoptante</label>
            <textarea
              value={form.requirements ?? ""}
              onChange={(e) => set({ requirements: e.target.value || null })}
              maxLength={500}
              rows={3}
              placeholder="Ej: Necesita patio, no apto para niños menores de 5 años…"
              className="w-full rounded-xl border border-sand-200 px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400 resize-none"
            />
          </div>
          <div>
            <label className="block text-xs text-sand-500 mb-1">Notas médicas</label>
            <textarea
              value={form.medicalNotes ?? ""}
              onChange={(e) => set({ medicalNotes: e.target.value || null })}
              maxLength={500}
              rows={2}
              placeholder="Vacunas, tratamientos pendientes, condiciones especiales…"
              className="w-full rounded-xl border border-sand-200 px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400 resize-none"
            />
          </div>
        </section>

        {/* Characteristics */}
        <section className="space-y-3">
          <h2 className="text-sm font-semibold text-ink-700 border-b border-sand-100 pb-2">
            Características
          </h2>
          <div className="grid grid-cols-2 gap-2">
            {(
              [
                ["isVaccinated", "Vacunado"],
                ["isSterilized", "Castrado"],
                ["isMicrochipped", "Tiene microchip"],
                ["okWithKids", "OK con niños"],
                ["okWithDogs", "OK con perros"],
                ["okWithCats", "OK con gatos"],
                ["needsYard", "Necesita patio"],
              ] as [keyof PublishAnimalPayload, string][]
            ).map(([key, label]) => (
              <label key={key} className="flex items-center gap-2 text-sm text-ink-700 cursor-pointer">
                <input
                  type="checkbox"
                  checked={!!form[key]}
                  onChange={(e) => set({ [key]: e.target.checked })}
                  className="rounded border-sand-300 text-brand-500 focus:ring-brand-400"
                />
                {label}
              </label>
            ))}
          </div>
        </section>

        {/* Location reference */}
        <section className="space-y-3">
          <h2 className="text-sm font-semibold text-ink-700 border-b border-sand-100 pb-2">
            Zona de referencia
          </h2>
          <p className="text-xs text-sand-400">
            La ubicación exacta no se muestra públicamente — solo la zona de referencia.
          </p>
          <Input
            label="Zona (ej: San José, Escazú)"
            value={form.refLabel ?? ""}
            onChange={(e) => set({ refLabel: e.target.value || null })}
            placeholder="Escazú, San José"
          />
        </section>

        <Button
          onClick={handleSubmit}
          disabled={publish.isPending || !form.name.trim() || !form.story.trim()}
          className="w-full"
        >
          {publish.isPending ? "Publicando…" : "Publicar animal"}
        </Button>
      </div>
    </>
  );
}
