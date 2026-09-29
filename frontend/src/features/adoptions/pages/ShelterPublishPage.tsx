import { useEffect, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import { Button } from "@/shared/ui/Button";
import { Input, Select } from "@/shared/ui/Input";
import { usePublishAnimal, useUploadAdoptionPhoto } from "../hooks/useAdoptions";
import type { PetSpecies, PetSize, AgeCategory, PublishAnimalPayload } from "../api/adoptionsApi";
import { SPECIES_LABELS, SIZE_LABELS, AGE_LABELS } from "../api/adoptionsApi";
import { toast } from "@/shared/lib/toast";
import { Modal } from "@/shared/ui/Modal";
import { Alert } from "@/shared/ui/Alert";
import { useAuthStore } from "@/features/auth/store/authStore";

const INITIAL: PublishAnimalPayload = {
  name: "",
  species: "Dog",
  size: "Medium",
  ageCategory: "Young",
  ageMonthsApprox: null,
  story: "",
  requirements: null,
  medicalNotes: null,
  breed: null,
  isVaccinated: false,
  isSterilized: false,
  isMicrochipped: false,
  okWithKids: false,
  okWithDogs: false,
  okWithCats: false,
  needsYard: false,
  refLat: 9.9281,
  refLng: -84.0907,
  refLabel: "San José, Costa Rica",
};

const DRAFT_FIELDS = [
  "name",
  "species",
  "size",
  "ageCategory",
  "ageMonthsApprox",
  "story",
  "requirements",
  "breed",
  "isVaccinated",
  "isSterilized",
  "isMicrochipped",
  "okWithKids",
  "okWithDogs",
  "okWithCats",
  "needsYard",
  "refLabel",
] as const;

function draftFields(value: Partial<PublishAnimalPayload>): Partial<PublishAnimalPayload> {
  const draft: Partial<PublishAnimalPayload> = {};
  for (const field of DRAFT_FIELDS) {
    const candidate = value[field];
    if (
      candidate === null ||
      typeof candidate === "string" ||
      typeof candidate === "number" ||
      typeof candidate === "boolean"
    ) {
      Object.assign(draft, { [field]: candidate });
    }
  }
  return draft;
}

export default function ShelterPublishPage() {
  const navigate = useNavigate();
  const userId = useAuthStore((state) => state.user?.id);
  const draftKey = userId ? `pawtrack:adoption-draft:${userId}` : null;
  const publish = usePublishAnimal();
  const uploadPhoto = useUploadAdoptionPhoto();
  const [form, setForm] = useState<PublishAnimalPayload>(() => {
    if (!draftKey) return INITIAL;
    try {
      const saved = sessionStorage.getItem(draftKey);
      if (!saved) return INITIAL;
      const draft: unknown = JSON.parse(saved);
      if (!draft || typeof draft !== "object" || Array.isArray(draft)) return INITIAL;
      return { ...INITIAL, ...draftFields(draft) };
    } catch {
      return INITIAL;
    }
  });
  const [publishedId, setPublishedId] = useState<string | null>(null);
  const [confirmPublish, setConfirmPublish] = useState(false);
  const [publishError, setPublishError] = useState(false);
  const [photoFiles, setPhotoFiles] = useState<File[]>([]);
  const [uploading, setUploading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (!draftKey || publishedId) return;
    try {
      if (JSON.stringify(draftFields(form)) === JSON.stringify(draftFields(INITIAL)))
        sessionStorage.removeItem(draftKey);
      else sessionStorage.setItem(draftKey, JSON.stringify(draftFields(form)));
    } catch {
      toast.error("No se pudo guardar el borrador en este navegador");
    }
  }, [form, draftKey, publishedId]);

  const set = (partial: Partial<PublishAnimalPayload>) => setForm((f) => ({ ...f, ...partial }));

  const handleSubmit = () => {
    if (!form.name.trim() || !form.story.trim()) {
      toast.error("El nombre y la historia son requeridos");
      return;
    }
    setPublishError(false);
    setConfirmPublish(true);
  };

  const confirmPublication = () => {
    publish.mutate(form, {
      onSuccess: (animal) => {
        if (draftKey) sessionStorage.removeItem(draftKey);
        setConfirmPublish(false);
        toast.success(`¡${animal.name} publicado! Ahora sube hasta 5 fotos.`);
        setPublishedId(animal.id);
      },
      onError: () => setPublishError(true),
    });
  };

  const handleUploadPhotos = async () => {
    if (!publishedId || !photoFiles.length) {
      void navigate("/shelter/dashboard");
      return;
    }
    setUploading(true);
    for (const file of photoFiles.slice(0, 5)) {
      await uploadPhoto.mutateAsync({ animalId: publishedId, file });
    }
    setUploading(false);
    toast.success("Fotos subidas correctamente ✓");
    void navigate("/shelter/dashboard");
  };

  return (
    <>
      <Helmet>
        <title>Publicar animal · PawTrack CR</title>
      </Helmet>

      <div className="mx-auto max-w-xl px-4 py-8 space-y-6">
        <Link to="/shelter/dashboard" className="inline-flex text-sm font-semibold text-brand-600 hover:underline">
          Volver al panel del shelter
        </Link>
        <h1 className="text-xl font-bold text-ink-900">Publicar animal en adopción</h1>
        <div className="flex items-center justify-between gap-3 text-xs text-copy-secondary">
          <span>Borrador de esta sesión; no incluye notas médicas ni fotos.</span>
          <button
            type="button"
            onClick={() => setForm(INITIAL)}
            className="shrink-0 font-semibold text-brand-700 underline focus-visible:outline-2 focus-visible:outline-focus-ring"
          >
            Descartar borrador
          </button>
        </div>

        {/* Basic info */}
        <section className="space-y-4">
          <h2 className="text-sm font-semibold text-ink-700 border-b border-sand-100 pb-2">Información básica</h2>

          <Input
            label="Nombre *"
            value={form.name}
            onChange={(e) => set({ name: e.target.value })}
            maxLength={80}
            placeholder="Max, Luna, etc."
          />

          <div className="grid grid-cols-2 gap-3">
            <Select
              label="Especie"
              required
              id="adoption-species"
              value={form.species}
              onChange={(e) => set({ species: e.target.value as PetSpecies })}
            >
              {(Object.entries(SPECIES_LABELS) as [PetSpecies, string][]).map(([v, l]) => (
                <option key={v} value={v}>
                  {l}
                </option>
              ))}
            </Select>
            <Select
              label="Tamaño"
              required
              id="adoption-size"
              value={form.size}
              onChange={(e) => set({ size: e.target.value as PetSize })}
            >
              {(Object.entries(SIZE_LABELS) as [PetSize, string][]).map(([v, l]) => (
                <option key={v} value={v}>
                  {l}
                </option>
              ))}
            </Select>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <Select
              label="Categoría de edad"
              required
              id="adoption-age-category"
              value={form.ageCategory}
              onChange={(e) => set({ ageCategory: e.target.value as AgeCategory })}
            >
              {(Object.entries(AGE_LABELS) as [AgeCategory, string][]).map(([v, l]) => (
                <option key={v} value={v}>
                  {l}
                </option>
              ))}
            </Select>
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
          <h2 className="text-sm font-semibold text-ink-700 border-b border-sand-100 pb-2">Historia y personalidad</h2>
          <div>
            <label htmlFor="adoption-story" className="block text-xs text-copy-secondary mb-1">
              Historia *
            </label>
            <textarea
              id="adoption-story"
              value={form.story}
              onChange={(e) => set({ story: e.target.value })}
              maxLength={2000}
              rows={5}
              placeholder="Cuéntanos cómo llegó, cómo es su personalidad, qué necesidades especiales tiene…"
              className="w-full rounded-xl border border-sand-200 px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-focus-ring resize-none"
            />
            <p className="text-right text-xs text-copy-muted mt-1">{form.story.length}/2000</p>
          </div>
          <div>
            <label htmlFor="adoption-requirements" className="block text-xs text-copy-secondary mb-1">
              Requisitos para el adoptante
            </label>
            <textarea
              id="adoption-requirements"
              value={form.requirements ?? ""}
              onChange={(e) => set({ requirements: e.target.value || null })}
              maxLength={500}
              rows={3}
              placeholder="Ej: Necesita patio, no apto para niños menores de 5 años…"
              className="w-full rounded-xl border border-sand-200 px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-focus-ring resize-none"
            />
          </div>
          <div>
            <label htmlFor="adoption-medical-notes" className="block text-xs text-copy-secondary mb-1">
              Notas médicas
            </label>
            <textarea
              id="adoption-medical-notes"
              value={form.medicalNotes ?? ""}
              onChange={(e) => set({ medicalNotes: e.target.value || null })}
              maxLength={500}
              rows={2}
              placeholder="Vacunas, tratamientos pendientes, condiciones especiales…"
              className="w-full rounded-xl border border-sand-200 px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-focus-ring resize-none"
            />
          </div>
        </section>

        {/* Characteristics */}
        <section className="space-y-3">
          <h2 className="text-sm font-semibold text-ink-700 border-b border-sand-100 pb-2">Características</h2>
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
                  className="rounded border-sand-300 text-brand-600 focus:ring-focus-ring"
                />
                {label}
              </label>
            ))}
          </div>
        </section>

        {/* Location reference */}
        <section className="space-y-3">
          <h2 className="text-sm font-semibold text-ink-700 border-b border-sand-100 pb-2">Zona de referencia</h2>
          <p className="text-xs text-copy-muted">
            La ubicación exacta no se muestra públicamente — solo la zona de referencia.
          </p>
          <Input
            label="Zona (ej: San José, Escazú)"
            value={form.refLabel ?? ""}
            onChange={(e) => set({ refLabel: e.target.value || null })}
            placeholder="Escazú, San José"
          />
        </section>

        {!publishedId ? (
          <Button
            onClick={handleSubmit}
            disabled={publish.isPending || !form.name.trim() || !form.story.trim()}
            className="w-full"
          >
            {publish.isPending ? "Publicando…" : "Publicar animal"}
          </Button>
        ) : (
          /* Step 2: inline photo upload after successful publish */
          <section className="space-y-4 rounded-2xl border-2 border-dashed border-brand-200 bg-brand-50 p-5">
            <h2 className="text-sm font-semibold text-brand-700">📸 Paso 2 — Fotos (hasta 5)</h2>
            <p className="text-xs text-copy-secondary">Añade fotos para que los adoptantes conozcan mejor al animal.</p>

            <input
              ref={fileInputRef}
              type="file"
              accept="image/*"
              multiple
              className="hidden"
              onChange={(e) => {
                const files = Array.from(e.target.files ?? []).slice(0, 5);
                setPhotoFiles(files);
              }}
            />

            <button
              onClick={() => fileInputRef.current?.click()}
              className="w-full rounded-xl border-2 border-dashed border-sand-300 py-6 text-sm text-copy-secondary hover:border-brand-400 hover:text-brand-600 transition-colors"
            >
              {photoFiles.length > 0 ? `${photoFiles.length} foto(s) seleccionada(s)` : "Toca para seleccionar fotos"}
            </button>

            {photoFiles.length > 0 && (
              <div className="flex flex-wrap gap-2">
                {photoFiles.map((f, i) => (
                  <div key={i} className="relative h-16 w-16 rounded-lg overflow-hidden bg-sand-100">
                    <img src={URL.createObjectURL(f)} alt={f.name} className="h-full w-full object-cover" />
                  </div>
                ))}
              </div>
            )}

            <div className="flex gap-3">
              <Button
                onClick={() => {
                  void handleUploadPhotos();
                }}
                disabled={uploading}
                className="flex-1"
              >
                {uploading ? "Subiendo…" : photoFiles.length ? "Subir y terminar" : "Terminar sin fotos"}
              </Button>
            </div>
          </section>
        )}
      </div>
      <Modal isOpen={confirmPublish} onClose={() => setConfirmPublish(false)} title="Confirmar publicación">
        <p className="text-sm text-copy-secondary">
          La ficha de {form.name.trim()} quedará visible para adopción. Revisa la historia, los requisitos y las notas
          médicas antes de publicar.
        </p>
        {publishError && (
          <Alert variant="error" className="mt-4">
            No se pudo publicar. Revisa tus datos y vuelve a intentarlo.
          </Alert>
        )}
        <div className="mt-5 flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setConfirmPublish(false)}>
            Volver a editar
          </Button>
          <Button loading={publish.isPending} onClick={confirmPublication}>
            Confirmar publicación
          </Button>
        </div>
      </Modal>
    </>
  );
}
