import type { ReactNode } from "react";
import type { SubscriptionTier } from "../api/subscriptionApi";
import { useMyTier } from "../hooks/useMyTier";
import { useSubscriptionCatalog } from "../hooks/useSubscription";

type RequiredPlan = "Plus" | "Familia";

interface PlanGateProps {
  requires: RequiredPlan;
  children: ReactNode;
  /** Override the default upgrade banner with a custom fallback. */
  fallback?: ReactNode;
}

/** Renders children only when the user's active plan meets the requirement. */
export function PlanGate({ requires, children, fallback }: PlanGateProps) {
  const { isPlus, isFamilia, isLoading } = useMyTier();

  if (isLoading) return null;

  const allowed = requires === "Plus" ? isPlus : isFamilia;
  if (allowed) return <>{children}</>;

  return fallback !== undefined ? <>{fallback}</> : <UpgradeBanner requires={requires} />;
}

// ── Upgrade banner ────────────────────────────────────────────────────────────

interface UpgradeBannerProps {
  requires: RequiredPlan;
  compact?: boolean;
}

const PLAN_LABELS: Record<RequiredPlan, { name: string; tier: SubscriptionTier }> = {
  Plus: { name: "Plus", tier: "UserPlus" },
  Familia: { name: "Familia", tier: "UserFamilia" },
};

export function UpgradeBanner({ requires, compact }: UpgradeBannerProps) {
  const { name, tier } = PLAN_LABELS[requires];
  const { data: catalog } = useSubscriptionCatalog();
  const plan = catalog?.find((item) => item.tier === tier && item.isActive);
  const price =
    plan?.monthlyPriceCrc == null ? "Precio no disponible" : `₡${plan.monthlyPriceCrc.toLocaleString("es-CR")}/mes`;

  if (compact) {
    return (
      <span className="inline-flex items-center gap-1.5 rounded-full border border-brand-200 bg-brand-50 px-2.5 py-1 text-xs font-semibold text-brand-700">
        🔒 Requiere {name}
      </span>
    );
  }

  return (
    <div className="rounded-2xl border border-brand-200 bg-brand-50 p-4 text-center">
      <p className="text-sm font-semibold text-brand-700">
        🔒 Esta función requiere el plan <span className="font-bold">{name}</span>{" "}
        <span className="font-normal text-brand-500">({price})</span>
      </p>
      <button
        type="button"
        onClick={() => {
          // Open FreemiumModal — dispatched via a global event so no prop-drilling needed
          window.dispatchEvent(new CustomEvent("pawtrack:open-upgrade-modal"));
        }}
        className="mt-2.5 rounded-xl bg-brand-500 px-4 py-2 text-xs font-bold text-white transition hover:bg-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
      >
        Conocer plan {name} →
      </button>
    </div>
  );
}
