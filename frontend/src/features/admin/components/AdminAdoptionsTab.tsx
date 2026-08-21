import { useState } from "react";
import { Link } from "react-router-dom";
import { motion, AnimatePresence } from "framer-motion";
import {
  useAdoptionAdminStats,
  useAdminAdoptionAnimals,
  useAdminModerateAnimal,
} from "../hooks/useAdmin";
import type { AdoptablePetDto } from "@/features/adoptions/api/adoptionsApi";
import { SPECIES_LABELS, AGE_LABELS } from "@/features/adoptions/api/adoptionsApi";
import { toast } from "@/shared/lib/toast";

const STATUS_OPTIONS = [
  { value: "", label: "Todos" },
  { value: "Available", label: "Disponible" },
  { value: "InProcess", label: "En proceso" },
  { value: "Adopted", label: "Adoptado" },
  { value: "Paused", label: "Pausado" },
  { value: "Removed", label: "Removido" },
];

const STATUS_COLORS: Record<string, string> = {
  Available: "bg-green-100 text-green-700",
  InProcess: "bg-yellow-100 text-yellow-700",
  Adopted:   "bg-blue-100 text-blue-700",
  Paused:    "bg-orange-100 text-orange-700",
  Removed:   "bg-red-100 text-red-600",
};

function StatPill({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <div className={`rounded-2xl px-4 py-3 text-center ${color}`}>
      <p className="text-2xl font-black">{value}</p>
      <p className="text-xs font-medium mt-0.5">{label}</p>
    </div>
  );
}

function AnimalAdminRow({ animal }: { animal: AdoptablePetDto }) {
  const moderate = useAdminModerateAnimal();
  const [processing, setProcessing] = useState(false);

  const handle = async (action: "remove" | "pause" | "restore") => {
    setProcessing(true);
    try {
      await moderate.mutateAsync({ id: animal.id, action });
      toast.success(
        action === "remove" ? "Animal removido" :
        action === "pause"  ? "Animal pausado" :
                              "Animal restaurado"
      );
    } finally {
      setProcessing(false);
    }
  };

  const photo = animal.photoUrls[0];
  const st = STATUS_COLORS[animal.status] ?? "bg-sand-100 text-sand-500";

  return (
    <motion.li
      layout
      initial={{ opacity: 0, y: 6 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, scale: 0.97 }}
      transition={{ duration: 0.15 }}
      className="rounded-2xl border border-sand-200 bg-surface p-4 shadow-sm"
    >
      <div className="flex items-start gap-3">
        {/* Thumbnail */}
        <div className="h-14 w-14 shrink-0 rounded-xl overflow-hidden bg-sand-100 flex items-center justify-center">
          {photo
            ? <img src={photo} alt={animal.name} className="h-full w-full object-cover" />
            : <span className="text-xl">🐾</span>
          }
        </div>

        {/* Info */}
        <div className="flex-1 min-w-0 space-y-1">
          <div className="flex items-center gap-2 flex-wrap">
            <Link
              to={`/adopciones/${animal.id}`}
              className="font-semibold text-sand-900 hover:text-brand-600 truncate"
            >
              {animal.name}
            </Link>
            <span className={`text-[10px] font-bold px-2 py-0.5 rounded-full ${st}`}>
              {animal.status}
            </span>
          </div>
          <p className="text-xs text-sand-500">
            {SPECIES_LABELS[animal.species]}
            {animal.breed && ` · ${animal.breed}`}
            {" · "}{AGE_LABELS[animal.ageCategory]}
          </p>
          <p className="text-xs text-sand-400">
            🏠 {animal.organizationName}
            {animal.refLabel && ` · 📍 ${animal.refLabel}`}
          </p>
          <p className="text-xs text-sand-400">
            Publicado {new Date(animal.publishedAt).toLocaleDateString("es-CR")}
          </p>
        </div>
      </div>

      {/* Moderation actions */}
      <div className="flex gap-2 mt-3 flex-wrap">
        {animal.status !== "Removed" && (
          <button
            onClick={() => void handle("remove")}
            disabled={processing}
            className="rounded-lg border border-red-200 px-3 py-1.5 text-xs font-semibold text-red-600 hover:bg-red-50 disabled:opacity-50 transition-colors"
          >
            Remover
          </button>
        )}
        {animal.status === "Available" && (
          <button
            onClick={() => void handle("pause")}
            disabled={processing}
            className="rounded-lg border border-orange-200 px-3 py-1.5 text-xs font-semibold text-orange-600 hover:bg-orange-50 disabled:opacity-50 transition-colors"
          >
            Pausar
          </button>
        )}
        {(animal.status === "Paused" || animal.status === "Removed") && (
          <button
            onClick={() => void handle("restore")}
            disabled={processing}
            className="rounded-lg border border-green-200 px-3 py-1.5 text-xs font-semibold text-green-600 hover:bg-green-50 disabled:opacity-50 transition-colors"
          >
            Restaurar
          </button>
        )}
      </div>
    </motion.li>
  );
}

export function AdminAdoptionsTab() {
  const [statusFilter, setStatusFilter] = useState("");
  const [page, setPage] = useState(1);
  const { data: stats, isLoading: statsLoading } = useAdoptionAdminStats();
  const { data: animalsPage, isLoading: animalsLoading } = useAdminAdoptionAnimals(
    statusFilter || undefined, page
  );

  return (
    <div className="space-y-6">
      {/* Stats grid */}
      {statsLoading ? (
        <div className="grid grid-cols-3 sm:grid-cols-4 gap-3 animate-pulse">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="h-16 rounded-2xl bg-sand-100" />
          ))}
        </div>
      ) : stats && (
        <div className="grid grid-cols-3 sm:grid-cols-4 gap-3">
          <StatPill label="Publicados" value={stats.totalPublished} color="bg-sand-50" />
          <StatPill label="Disponibles" value={stats.totalAvailable} color="bg-green-50 text-green-800" />
          <StatPill label="En proceso" value={stats.totalInProcess} color="bg-yellow-50 text-yellow-800" />
          <StatPill label="Adoptados" value={stats.totalAdopted} color="bg-blue-50 text-blue-800" />
          <StatPill label="Solicitudes" value={stats.totalApplications} color="bg-purple-50 text-purple-800" />
          <StatPill label="Ferias" value={stats.totalFairs} color="bg-pink-50 text-pink-800" />
        </div>
      )}

      {/* Filter */}
      <div className="flex items-center gap-3">
        <select
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
          className="rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
        >
          {STATUS_OPTIONS.map((o) => (
            <option key={o.value} value={o.value}>{o.label}</option>
          ))}
        </select>
        <span className="text-xs text-sand-500">
          {animalsPage?.totalCount ?? 0} animales
        </span>
        <Link
          to="/adopciones"
          className="ml-auto text-xs text-brand-600 hover:underline"
        >
          Ver directorio público →
        </Link>
      </div>

      {/* List */}
      {animalsLoading ? (
        <div className="space-y-3 animate-pulse">
          {[1, 2, 3].map((i) => <div key={i} className="h-24 rounded-2xl bg-sand-100" />)}
        </div>
      ) : !animalsPage?.items.length ? (
        <div className="py-12 text-center text-sand-400">
          <p className="text-3xl mb-2">🐾</p>
          <p className="text-sm">No hay animales con este filtro</p>
        </div>
      ) : (
        <ul className="space-y-3">
          <AnimatePresence>
            {animalsPage.items.map((animal) => (
              <AnimalAdminRow key={animal.id} animal={animal} />
            ))}
          </AnimatePresence>
        </ul>
      )}

      {/* Pagination */}
      {(animalsPage?.totalPages ?? 1) > 1 && (
        <div className="flex items-center justify-between pt-2 border-t border-sand-100">
          <button
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
            className="px-4 py-2 rounded-xl border border-sand-200 text-sm disabled:opacity-40"
          >
            ← Anterior
          </button>
          <span className="text-xs text-sand-400">
            {page} / {animalsPage?.totalPages}
          </span>
          <button
            disabled={!animalsPage?.hasNextPage}
            onClick={() => setPage((p) => p + 1)}
            className="px-4 py-2 rounded-xl border border-sand-200 text-sm disabled:opacity-40"
          >
            Siguiente →
          </button>
        </div>
      )}
    </div>
  );
}
