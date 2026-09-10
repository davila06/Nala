import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import { Button } from "@/shared/ui/Button";
import { Alert } from "@/shared/ui/Alert";
import { usePets } from "@/features/pets/hooks/usePets";
import { useSubmitOwnerAdoption } from "../hooks/useAdoptions";
import type { AgeCategory, PetSize } from "../api/adoptionsApi";
import { AGE_LABELS, SIZE_LABELS } from "../api/adoptionsApi";
import { toast } from "@/shared/lib/toast";

export default function OwnerAdoptionSubmissionPage() {
  const navigate = useNavigate();
  const { data: pets = [], isLoading } = usePets();
  const submission = useSubmitOwnerAdoption();
  const [petId, setPetId] = useState("");
  const [size, setSize] = useState<PetSize>("Medium");
  const [ageCategory, setAgeCategory] = useState<AgeCategory>("Adult");
  const [story, setStory] = useState("");
  const [requirements, setRequirements] = useState("");
  const [refLabel, setRefLabel] = useState("");
  const [responsibility, setResponsibility] = useState(false);
  const [transfer, setTransfer] = useState(false);

  const eligiblePets = pets.filter(
    (pet) => pet.status === "Active" && pet.photoUrl,
  );

  const submit = () => {
    if (!petId || !story.trim() || !responsibility || !transfer) return;
    submission.mutate(
      {
        petId,
        size,
        ageCategory,
        story,
        requirements: requirements || null,
        refLat: 9.9281,
        refLng: -84.0907,
        refLabel: refLabel || null,
        confirmsResponsibility: responsibility,
        confirmsTransfer: transfer,
      },
      {
        onSuccess: () => {
          toast.success("Solicitud enviada para revisión.");
          void navigate("/dashboard");
        },
      },
    );
  };

  return (
    <main className="mx-auto max-w-xl space-y-6 px-4 py-8">
      <Helmet>
        <title>Dar en adopción · PawTrack CR</title>
      </Helmet>
      <Link
        to="/dashboard"
        className="text-sm font-semibold text-brand-600 hover:underline"
      >
        Volver a mis mascotas
      </Link>
      <header>
        <h1 className="font-display text-2xl font-semibold text-ink-900">
          Dar una mascota en adopción
        </h1>
        <p className="mt-2 text-sm text-sand-600">
          Revisaremos tu solicitud antes de publicarla. Tu dirección y datos de
          contacto no se mostrarán públicamente.
        </p>
      </header>
      {eligiblePets.length === 0 && !isLoading ? (
        <Alert variant="error">
          Necesitas una mascota activa con foto para solicitar una adopción.
        </Alert>
      ) : (
        <section className="space-y-4">
          <label className="block text-sm font-medium text-sand-700">
            Mascota registrada
            <select
              value={petId}
              onChange={(event) => setPetId(event.target.value)}
              className="mt-1 w-full rounded-lg border border-sand-200 bg-surface px-3 py-2"
              required
            >
              <option value="">Selecciona una mascota</option>
              {eligiblePets.map((pet) => (
                <option key={pet.id} value={pet.id}>
                  {pet.name}
                </option>
              ))}
            </select>
          </label>
          <div className="grid grid-cols-2 gap-4">
            <label className="text-sm font-medium text-sand-700">
              Tamaño
              <select
                value={size}
                onChange={(event) => setSize(event.target.value as PetSize)}
                className="mt-1 w-full rounded-lg border border-sand-200 bg-surface px-3 py-2"
              >
                {Object.entries(SIZE_LABELS).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </label>
            <label className="text-sm font-medium text-sand-700">
              Edad
              <select
                value={ageCategory}
                onChange={(event) =>
                  setAgeCategory(event.target.value as AgeCategory)
                }
                className="mt-1 w-full rounded-lg border border-sand-200 bg-surface px-3 py-2"
              >
                {Object.entries(AGE_LABELS).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </label>
          </div>
          <label className="block text-sm font-medium text-sand-700">
            Motivo e historia
            <textarea
              value={story}
              onChange={(event) => setStory(event.target.value)}
              maxLength={2000}
              rows={5}
              className="mt-1 w-full rounded-lg border border-sand-200 px-3 py-2"
              required
            />
          </label>
          <label className="block text-sm font-medium text-sand-700">
            Requisitos para la familia
            <textarea
              value={requirements}
              onChange={(event) => setRequirements(event.target.value)}
              maxLength={500}
              rows={3}
              className="mt-1 w-full rounded-lg border border-sand-200 px-3 py-2"
            />
          </label>
          <label className="block text-sm font-medium text-sand-700">
            Zona de referencia
            <input
              value={refLabel}
              onChange={(event) => setRefLabel(event.target.value)}
              maxLength={100}
              className="mt-1 w-full rounded-lg border border-sand-200 px-3 py-2"
              placeholder="Ej. Escazú, San José"
            />
          </label>
          <label className="flex gap-2 text-sm text-sand-700">
            <input
              type="checkbox"
              checked={responsibility}
              onChange={(event) => setResponsibility(event.target.checked)}
            />
            Confirmo que soy responsable de esta mascota y que la información es
            correcta.
          </label>
          <label className="flex gap-2 text-sm text-sand-700">
            <input
              type="checkbox"
              checked={transfer}
              onChange={(event) => setTransfer(event.target.checked)}
            />
            Entiendo que la adopción implica una transferencia responsable, no
            una venta.
          </label>
          <Button
            type="button"
            fullWidth
            loading={submission.isPending}
            disabled={
              !petId ||
              !story.trim() ||
              !responsibility ||
              !transfer ||
              isLoading
            }
            onClick={submit}
          >
            Enviar a revisión
          </Button>
        </section>
      )}
    </main>
  );
}
