"use client";

import { useEffect, useState } from "react";
import { planDescriptions, planFeatures } from "../lib/plan-descriptions";

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

type PublicPlansCatalogProps = {
  audience?: "all" | "household";
  variant?: "cards" | "summary";
};

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

export function PublicPlansCatalog({ audience = "all", variant = "cards" }: PublicPlansCatalogProps) {
  const configuredApiUrl = process.env.NEXT_PUBLIC_API_URL?.replace(/\/$/, "");
  const apiUrl = configuredApiUrl || (process.env.NODE_ENV === "development" ? "http://localhost:5199" : "");
  const [state, setState] = useState<CatalogState>(() =>
    apiUrl
      ? { status: "loading" }
      : { status: "unavailable", message: "El catálogo público no está configurado para este entorno." },
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
        setState({ status: "unavailable", message: "No fue posible consultar el catálogo público." });
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

  const plans =
    state.status === "ready" ? state.plans.filter((plan) => audience === "all" || plan.tier?.startsWith("User")) : [];

  if (state.status !== "ready" || plans.length === 0) {
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

  if (variant === "summary") {
    return (
      <div className="home-plan-list" aria-label="Planes publicados para hogares">
        {plans.map((plan) => {
          const features = plan.tier ? (planFeatures[plan.tier] ?? []) : [];

          return (
            <article className="home-plan-row" key={`${plan.tier ?? plan.displayName}-${plan.id}`}>
              <div className="home-plan-row-top">
                <span>{plan.tier ?? "PLAN"}</span>
                <strong>{formatPrice(plan)}</strong>
              </div>
              <h4>{plan.displayName}</h4>
              <p>
                {planDescriptions[plan.tier ?? ""] ?? plan.description ?? "Capacidades disponibles en PawTrack CR."}
              </p>
              {features.length > 0 ? (
                <ul>
                  {features.slice(0, 3).map((feature) => (
                    <li key={feature}>{feature}</li>
                  ))}
                </ul>
              ) : null}
            </article>
          );
        })}
      </div>
    );
  }

  return (
    <div className="catalog-grid" aria-label="Planes aprobados">
      {plans.map((plan, index) => {
        const features = plan.tier ? (planFeatures[plan.tier] ?? []) : [];
        const hasGpsAllowance = features.some((feature) => /collar(?:es)? GPS/i.test(feature));

        return (
          <article
            className="catalog-plan depth-surface"
            data-3d-depth="catalog"
            data-depth-strength="3"
            key={`${plan.tier ?? plan.displayName}-${plan.id ?? index}`}
            tabIndex={0}
          >
            <div className="catalog-plan-inner">
              <div className="catalog-plan-face catalog-plan-front">
                <span>{plan.tier ?? "PLAN"}</span>
                <h3>{plan.displayName}</h3>
                <p>{planDescriptions[plan.tier ?? ""] ?? "Capacidades disponibles en PawTrack CR."}</p>
                {features.length > 0 ? (
                  <ul aria-label={`Inclusiones principales de ${plan.displayName}`} className="catalog-plan-preview">
                    {features.slice(0, 3).map((feature) => (
                      <li key={feature}>{feature}</li>
                    ))}
                  </ul>
                ) : null}
                {hasGpsAllowance ? (
                  <p className="catalog-plan-limit-note">
                    La cuota GPS no incluye hardware ni garantiza un proveedor compatible.
                  </p>
                ) : null}
                <strong>{formatPrice(plan)}</strong>
                <small>Enfoca para consultar inclusiones adicionales.</small>
              </div>
              <div className="catalog-plan-face catalog-plan-back" aria-label={`Inclusiones de ${plan.displayName}`}>
                <span>INCLUYE</span>
                {features.length > 3 ? (
                  <ul>
                    {features.slice(3).map((feature) => (
                      <li key={feature}>{feature}</li>
                    ))}
                  </ul>
                ) : (
                  <p>Estas son las inclusiones publicadas para este plan.</p>
                )}
                <small>Los límites dependen del catálogo y del entorno conectado.</small>
              </div>
            </div>
          </article>
        );
      })}
    </div>
  );
}
