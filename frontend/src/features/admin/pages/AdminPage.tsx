import { useState } from "react";
import { Navigate } from "react-router-dom";
import { motion, AnimatePresence } from "framer-motion";
import { useAuthStore } from "@/features/auth/store/authStore";
import { useHaptic } from "@/shared/hooks/useHaptic";
import {
  usePendingAllies,
  usePendingClinics,
  useReviewAlly,
  useReviewClinic,
} from "../hooks/useAdmin";
import type { PendingAllyDto, PendingClinicDto } from "../api/adminApi";

type Tab = "allies" | "clinics";

const ALLY_TYPE_LABELS: Record<string, string> = {
  VeterinaryClinic: "Veterinaria",
  Shelter: "Refugio",
  PetFriendlyBusiness: "Comercio pet-friendly",
  PrivateSecurity: "Seguridad privada",
  Municipality: "Municipalidad",
};

// ── Small helper components ────────────────────────────────────────────────────

function StatCard({
  icon,
  label,
  value,
  urgent,
}: {
  icon: string;
  label: string;
  value: number;
  urgent?: boolean;
}) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 6 }}
      animate={{ opacity: 1, y: 0 }}
      className={[
        "flex items-center gap-3 rounded-2xl border p-4",
        urgent && value > 0
          ? "border-warn-200 bg-warn-50"
          : "border-sand-200 bg-surface",
      ].join(" ")}
    >
      <span className="text-2xl shrink-0" aria-hidden="true">
        {icon}
      </span>
      <div>
        <p
          className={`text-2xl font-black tabular-nums ${urgent && value > 0 ? "text-warn-700" : "text-sand-900"}`}
        >
          {value}
        </p>
        <p className="text-xs text-sand-500">{label}</p>
      </div>
    </motion.div>
  );
}

function ReviewCard({
  children,
  onApprove,
  onReject,
  approveLabel,
  rejectLabel,
  loading,
}: {
  children: React.ReactNode;
  onApprove: () => void;
  onReject: () => void;
  approveLabel: string;
  rejectLabel: string;
  loading: boolean;
}) {
  return (
    <motion.li
      layout
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, scale: 0.97 }}
      transition={{ duration: 0.2 }}
      className="rounded-2xl border border-sand-200 bg-surface p-4 shadow-sm"
    >
      <div className="flex items-start justify-between gap-4">
        <div className="min-w-0 flex-1">{children}</div>
        <div className="flex shrink-0 gap-2">
          <button
            type="button"
            onClick={onApprove}
            disabled={loading}
            className="inline-flex items-center gap-1.5 rounded-xl bg-rescue-100 px-3 py-1.5 text-xs font-semibold text-rescue-800 transition-colors hover:bg-rescue-200 disabled:opacity-60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400"
          >
            {loading ? (
              <span className="h-3 w-3 rounded-full border-2 border-rescue-400 border-t-transparent animate-spin" />
            ) : (
              <span aria-hidden="true">✓</span>
            )}
            {approveLabel}
          </button>
          <button
            type="button"
            onClick={onReject}
            disabled={loading}
            className="inline-flex items-center gap-1.5 rounded-xl bg-danger-100 px-3 py-1.5 text-xs font-semibold text-danger-700 transition-colors hover:bg-danger-200 disabled:opacity-60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-danger-400"
          >
            {loading ? (
              <span className="h-3 w-3 rounded-full border-2 border-danger-400 border-t-transparent animate-spin" />
            ) : (
              <span aria-hidden="true">✕</span>
            )}
            {rejectLabel}
          </button>
        </div>
      </div>
    </motion.li>
  );
}

function LoadingSkeleton() {
  return (
    <ul className="space-y-3">
      {[1, 2, 3].map((i) => (
        <li key={i} className="h-20 animate-pulse rounded-2xl bg-sand-100" />
      ))}
    </ul>
  );
}

function EmptyState({ msg }: { msg: string }) {
  return (
    <div className="flex flex-col items-center rounded-2xl border border-dashed border-sand-200 py-12 text-center">
      <span className="mb-2 text-3xl" aria-hidden="true">
        ✅
      </span>
      <p className="text-sm font-semibold text-sand-700">Todo al día</p>
      <p className="mt-1 text-xs text-sand-400">{msg}</p>
    </div>
  );
}

function ErrorState({ msg }: { msg: string }) {
  return (
    <div className="rounded-2xl border border-danger-200 bg-danger-50 px-4 py-3 text-sm text-danger-700">
      {msg}
    </div>
  );
}

// ── Tab sections ───────────────────────────────────────────────────────────────

function AlliesTab() {
  const { data, isLoading, isError } = usePendingAllies();
  const { mutateAsync: review, isPending } = useReviewAlly();
  const [processingId, setProcessingId] = useState<string | null>(null);
  const { tap, warning } = useHaptic();

  if (isLoading) return <LoadingSkeleton />;
  if (isError)
    return (
      <ErrorState msg="No se pudieron cargar las solicitudes de aliados." />
    );
  if (!data || data.length === 0)
    return <EmptyState msg="No hay solicitudes de aliados pendientes." />;

  const handle = async (ally: PendingAllyDto, approve: boolean) => {
    approve ? tap() : warning();
    setProcessingId(ally.userId);
    try {
      await review({ userId: ally.userId, approve });
    } finally {
      setProcessingId(null);
    }
  };

  return (
    <ul className="space-y-3">
      <AnimatePresence>
        {data.map((ally) => (
          <ReviewCard
            key={ally.userId}
            onApprove={() => void handle(ally, true)}
            onReject={() => void handle(ally, false)}
            approveLabel="Aprobar"
            rejectLabel="Rechazar"
            loading={isPending && processingId === ally.userId}
          >
            <p className="truncate font-semibold text-sand-900">
              {ally.organizationName}
            </p>
            <div className="mt-1 flex flex-wrap gap-1.5">
              <span className="rounded-full bg-trust-100 px-2 py-0.5 text-[10px] font-semibold text-trust-700">
                {ALLY_TYPE_LABELS[ally.allyType] ?? ally.allyType}
              </span>
              <span className="rounded-full bg-sand-100 px-2 py-0.5 text-[10px] text-sand-600">
                📍 {ally.coverageLabel}
              </span>
              <span className="rounded-full bg-sand-100 px-2 py-0.5 text-[10px] text-sand-600">
                {(ally.coverageRadiusMetres / 1000).toFixed(1)} km radio
              </span>
            </div>
            <p className="mt-1.5 text-[11px] text-sand-400">
              Aplicó el{" "}
              {new Date(ally.appliedAt).toLocaleDateString("es-CR", {
                day: "numeric",
                month: "long",
                year: "numeric",
              })}
            </p>
          </ReviewCard>
        ))}
      </AnimatePresence>
    </ul>
  );
}

function ClinicsTab() {
  const { data, isLoading, isError } = usePendingClinics();
  const { mutateAsync: review, isPending } = useReviewClinic();
  const [processingId, setProcessingId] = useState<string | null>(null);
  const { tap, warning } = useHaptic();

  if (isLoading) return <LoadingSkeleton />;
  if (isError)
    return <ErrorState msg="No se pudieron cargar las clínicas pendientes." />;
  if (!data || data.length === 0)
    return <EmptyState msg="No hay clínicas pendientes de aprobación." />;

  const handle = async (clinic: PendingClinicDto, approve: boolean) => {
    approve ? tap() : warning();
    setProcessingId(clinic.id);
    try {
      await review({ clinicId: clinic.id, approve });
    } finally {
      setProcessingId(null);
    }
  };

  return (
    <ul className="space-y-3">
      <AnimatePresence>
        {data.map((clinic) => (
          <ReviewCard
            key={clinic.id}
            onApprove={() => void handle(clinic, true)}
            onReject={() => void handle(clinic, false)}
            approveLabel="Activar"
            rejectLabel="Suspender"
            loading={isPending && processingId === clinic.id}
          >
            <p className="truncate font-semibold text-sand-900">
              {clinic.name}
            </p>
            <div className="mt-1 flex flex-wrap gap-1.5">
              <span className="rounded-full bg-brand-100 px-2 py-0.5 text-[10px] font-semibold text-brand-700">
                🏥 SENASA {clinic.licenseNumber}
              </span>
              <span className="rounded-full bg-sand-100 px-2 py-0.5 text-[10px] text-sand-600">
                📍 {clinic.address}
              </span>
            </div>
            <p className="mt-1 text-[11px] text-sand-400">
              {clinic.contactEmail}
            </p>
            <p className="mt-0.5 text-[11px] text-sand-400">
              Registro:{" "}
              {new Date(clinic.registeredAt).toLocaleDateString("es-CR", {
                day: "numeric",
                month: "long",
                year: "numeric",
              })}
            </p>
          </ReviewCard>
        ))}
      </AnimatePresence>
    </ul>
  );
}

// ── Page ───────────────────────────────────────────────────────────────────────

export default function AdminPage() {
  const user = useAuthStore((s) => s.user);
  const [activeTab, setActiveTab] = useState<Tab>("allies");
  const { data: alliesData } = usePendingAllies();
  const { data: clinicsData } = usePendingClinics();

  const allyCount = alliesData?.length ?? 0;
  const clinicCount = clinicsData?.length ?? 0;

  if (!user || user.role !== "Admin") {
    return <Navigate to="/dashboard" replace />;
  }

  return (
    <div className="mx-auto max-w-3xl px-4 py-8 animate-fade-in-up">
      {/* ── Header ── */}
      <div className="mb-6">
        <p className="text-xs font-semibold uppercase tracking-[0.3em] text-sand-400">
          PawTrack CR
        </p>
        <h1 className="mt-1 text-2xl font-black tracking-tight text-sand-900">
          Panel de administración
        </h1>
        <p className="mt-1 text-sm text-sand-500">
          Revisa, aprueba o rechaza solicitudes de aliados y clínicas.
        </p>
      </div>

      {/* ── Stats row ── */}
      <div className="mb-6 grid grid-cols-2 gap-3 sm:grid-cols-4">
        <StatCard
          icon="🤝"
          label="Aliados pendientes"
          value={allyCount}
          urgent
        />
        <StatCard
          icon="🏥"
          label="Clínicas pendientes"
          value={clinicCount}
          urgent
        />
        <StatCard icon="✅" label="Aprobados hoy" value={0} />
        <StatCard
          icon="📋"
          label="Total en revisión"
          value={allyCount + clinicCount}
          urgent
        />
      </div>

      {/* ── Tabs ── */}
      <div className="mb-6 flex gap-1 rounded-2xl bg-surface-warm p-1.5">
        {(["allies", "clinics"] as const).map((tab) => {
          const count = tab === "allies" ? allyCount : clinicCount;
          const label = tab === "allies" ? "Aliados" : "Clínicas";
          return (
            <button
              key={tab}
              type="button"
              role="tab"
              aria-selected={activeTab === tab}
              onClick={() => setActiveTab(tab)}
              className={[
                "flex flex-1 items-center justify-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400",
                activeTab === tab
                  ? "bg-surface text-sand-900 shadow-sm"
                  : "text-sand-500 hover:text-sand-700",
              ].join(" ")}
            >
              {label}
              {count > 0 && (
                <span
                  className={`rounded-full px-1.5 py-0.5 text-[10px] font-bold ${activeTab === tab ? "bg-warn-100 text-warn-700" : "bg-sand-200 text-sand-600"}`}
                >
                  {count}
                </span>
              )}
            </button>
          );
        })}
      </div>

      {/* ── Tab content ── */}
      <AnimatePresence mode="wait">
        <motion.div
          key={activeTab}
          initial={{ opacity: 0, x: 8 }}
          animate={{ opacity: 1, x: 0 }}
          exit={{ opacity: 0, x: -8 }}
          transition={{ duration: 0.16 }}
        >
          {activeTab === "allies" ? <AlliesTab /> : <ClinicsTab />}
        </motion.div>
      </AnimatePresence>
    </div>
  );
}
