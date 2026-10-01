"use client";

import { useEffect, useState } from "react";

type PublicPlan = {
  id: string;
  displayName: string;
  description?: string | null;
  tier?: string | null;
  monthlyPriceCrc?: number | null;
  annualPriceCrc?: number | null;
};

type CatalogState =
  | { status: "loading" }
  | { status: "unavailable"; message: string }
  | { status: "ready"; plans: PublicPlan[] };

const currency = new Intl.NumberFormat("es-CR", {
  style: "currency",
  currency: "CRC",
  maximumFractionDigits: 0,
});

function formatPrice(plan: PublicPlan): string {
  if (plan.monthlyPriceCrc != null) return `${currency.format(plan.monthlyPriceCrc)} / mes`;
  if (plan.annualPriceCrc != null) return `${currency.format(plan.annualPriceCrc)} / año`;
  return "Precio no publicado";
}

export function PublicPlansCatalog() {
  const apiUrl = process.env.NEXT_PUBLIC_API_URL?.replace(/\/$/, "");
  const [state, setState] = useState<CatalogState>(() =>
    apiUrl
      ? { status: "loading" }
      : { status: "unavailable", message: "El catálogo público todavía no está conectado." },
  );

  useEffect(() => {
    if (!apiUrl) {
      return;
    }

    const controller = new AbortController();
    fetch(`${apiUrl}/api/catalog/subscription-plans`, {
      signal: controller.signal,
      headers: { Accept: "application/json" },
    })
      .then(async (response) => {
        if (!response.ok) throw new Error("catalog-unavailable");
        const plans = (await response.json()) as PublicPlan[];
        setState({ status: "ready", plans });
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") return;
        setState({ status: "unavailable", message: "No hay planes aprobados disponibles para mostrar." });
      });

    return () => controller.abort();
  }, [apiUrl]);

  if (state.status === "loading") {
    return (
      <p className="inline-notice" role="status">
        Consultando el catálogo público…
      </p>
    );
  }

  if (state.status !== "ready" || state.plans.length === 0) {
    return (
      <div className="catalog-empty" role="status">
        <span className="catalog-empty-mark" aria-hidden="true">
          —
        </span>
        <strong>Catálogo no disponible todavía</strong>
        <p>
          {state.status === "unavailable"
            ? state.message
            : "Los planes existentes requieren aprobación antes de publicarse."}
        </p>
      </div>
    );
  }

  return (
    <div className="catalog-grid" aria-label="Planes aprobados">
      {state.plans.map((plan, index) => (
        <article
          className="catalog-plan depth-surface"
          data-3d-depth="catalog"
          data-depth-strength="3"
          key={`${plan.tier ?? plan.displayName}-${plan.id ?? index}`}
        >
          <span>{plan.tier ?? "PLAN"}</span>
          <h3>{plan.displayName}</h3>
          <p>{plan.description ?? "Capacidades disponibles en PawTrack CR."}</p>
          <strong>{formatPrice(plan)}</strong>
        </article>
      ))}
    </div>
  );
}
