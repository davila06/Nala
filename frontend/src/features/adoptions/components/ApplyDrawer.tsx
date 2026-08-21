import { useState } from "react";
import { Drawer } from "@/shared/ui/Drawer";
import { Button } from "@/shared/ui/Button";
import { useApplyToAdopt } from "../hooks/useAdoptions";

interface ApplyDrawerProps {
  animalId: string;
  animalName: string;
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export function ApplyDrawer({ animalId, animalName, isOpen, onClose, onSuccess }: ApplyDrawerProps) {
  const [note, setNote] = useState("");
  const apply = useApplyToAdopt();

  const handleSubmit = () => {
    if (!note.trim()) return;
    apply.mutate(
      { animalId, note: note.trim() },
      { onSuccess },
    );
  };

  return (
    <Drawer isOpen={isOpen} onClose={onClose} title={`Adoptar a ${animalName}`}>
      <div className="space-y-4 p-4">
        <p className="text-sm text-sand-500">
          Cuéntale a la organización un poco sobre ti y por qué quieres adoptar a {animalName}.
        </p>

        <textarea
          value={note}
          onChange={(e) => setNote(e.target.value)}
          placeholder="Ej: Tengo patio, experiencia con perros y mucho amor para dar…"
          maxLength={500}
          rows={5}
          className="w-full rounded-xl border border-sand-200 bg-surface px-4 py-3 text-sm text-ink-800 focus:outline-none focus:ring-2 focus:ring-brand-400 resize-none"
        />
        <p className="text-right text-xs text-sand-400">{note.length}/500</p>

        <Button
          onClick={handleSubmit}
          disabled={!note.trim() || apply.isPending}
          className="w-full"
        >
          {apply.isPending ? "Enviando…" : "Enviar solicitud"}
        </Button>
      </div>
    </Drawer>
  );
}
