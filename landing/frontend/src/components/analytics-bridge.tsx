"use client";

import { useEffect } from "react";

const allowedEvents = new Set([
  "landing_viewed",
  "hero_primary_cta_clicked",
  "hero_secondary_cta_clicked",
  "audience_selected",
  "report_lost_pet_clicked",
  "report_found_pet_clicked",
  "report_welfare_case_clicked",
  "pricing_viewed",
  "service_directory_clicked",
  "faq_opened",
]);

export function AnalyticsBridge() {
  useEffect(() => {
    const handleClick = (event: MouseEvent) => {
      const target = event.target;
      if (!(target instanceof Element)) return;

      const trigger = target.closest<HTMLElement>("[data-analytics-event]");
      const eventName = trigger?.dataset.analyticsEvent;
      if (!eventName || !allowedEvents.has(eventName)) return;

      window.dispatchEvent(
        new CustomEvent("nala:analytics", {
          detail: { event: eventName },
        }),
      );
    };

    document.addEventListener("click", handleClick, { passive: true });
    window.dispatchEvent(new CustomEvent("nala:analytics", { detail: { event: "landing_viewed" } }));

    return () => document.removeEventListener("click", handleClick);
  }, []);

  return null;
}
