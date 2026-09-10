import { useState, useEffect } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { useBillboards } from "../hooks/useBillboards";
import {
  billboardsApi,
  type BillboardDto,
  type BillboardPlacement,
} from "../api/billboardsApi";
import { trackProductEvent } from "@/shared/lib/telemetry";

interface BillboardBannerProps {
  placement: BillboardPlacement;
  className?: string;
}

function deliveryKey(billboardId: string, eventType: string): string {
  const visitorKey = "pawtrack:billboard:visitor";
  let visitorId = localStorage.getItem(visitorKey);
  if (!visitorId) {
    visitorId = crypto.randomUUID();
    localStorage.setItem(visitorKey, visitorId);
  }
  return `${visitorId}:${billboardId}:${eventType}:${new Date().toISOString().slice(0, 10)}`;
}

function trackDelivery(
  billboardId: string,
  eventType: "Impression" | "Click" | "Conversion",
) {
  try {
    void billboardsApi.trackDelivery(
      billboardId,
      eventType,
      deliveryKey(billboardId, eventType),
    );
  } catch {
    // Commercial telemetry must never block the customer journey.
  }
}

function hasAllowedImageSource(imageUrl: string | null): imageUrl is string {
  if (!imageUrl) return false;

  try {
    const url = new URL(imageUrl, window.location.origin);
    return (
      url.origin === window.location.origin ||
      url.protocol === "blob:" ||
      url.hostname.endsWith(".blob.core.windows.net")
    );
  } catch {
    return false;
  }
}

function BillboardCard({
  bill,
  placement,
  onDismiss,
}: {
  bill: BillboardDto;
  placement: BillboardPlacement;
  onDismiss: () => void;
}) {
  const handleCta = () => {
    if (!bill.ctaUrl) return;
    trackProductEvent("BillboardClicked", {
      source: "billboard",
      billboardId: bill.id,
      placement,
    });
    trackDelivery(bill.id, "Click");
    // Only open same-origin or https links
    try {
      const url = new URL(bill.ctaUrl);
      if (url.origin === window.location.origin || url.protocol === "https:")
        window.open(bill.ctaUrl, "_blank", "noopener,noreferrer");
    } catch {
      /* invalid URL */
    }
  };

  return (
    <motion.div
      layout
      initial={{ opacity: 0, y: -8 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -8 }}
      className="relative overflow-hidden rounded-2xl border border-sand-100 bg-surface shadow-sm"
      role="complementary"
      aria-label={`Anuncio: ${bill.title}`}
    >
      {/* Dismiss */}
      <button
        type="button"
        onClick={onDismiss}
        aria-label="Cerrar anuncio"
        className="absolute right-2 top-2 z-10 flex h-6 w-6 items-center justify-center rounded-full bg-black/20 text-white hover:bg-black/40 transition-colors text-xs"
      >
        ×
      </button>

      {/* Image */}
      {hasAllowedImageSource(bill.imageUrl) && (
        <img
          src={bill.imageUrl}
          alt={bill.title}
          className="h-28 w-full object-cover"
          loading="lazy"
        />
      )}

      {/* Content */}
      <div
        className={`px-4 py-3 space-y-1.5 ${!hasAllowedImageSource(bill.imageUrl) ? "pt-4" : ""}`}
      >
        <div className="flex items-center gap-2">
          <span className="text-[9px] font-bold uppercase tracking-widest text-sand-400">
            Publicidad
          </span>
        </div>
        <p className="font-semibold text-ink-900 text-sm leading-snug">
          {bill.title}
        </p>
        {bill.body && (
          <p className="text-xs text-sand-600 leading-relaxed">{bill.body}</p>
        )}
        {bill.ctaLabel && bill.ctaUrl && (
          <button
            type="button"
            onClick={handleCta}
            className="mt-1 inline-block rounded-xl bg-brand-500 px-4 py-1.5 text-xs font-semibold text-white hover:bg-brand-600 transition-colors"
          >
            {bill.ctaLabel}
          </button>
        )}
      </div>
    </motion.div>
  );
}

/**
 * Renders the highest-priority active billboard for the given placement.
 * Dismissals only affect the current mounted view. Returning to the page or
 * reloading it makes active billboards eligible to appear again.
 */
export function BillboardBanner({
  placement,
  className = "",
}: BillboardBannerProps) {
  const { data: billboards = [] } = useBillboards(placement);
  const [dismissed, setDismissedState] = useState<Set<string>>(new Set());
  const [rotationOffset, setRotationOffset] = useState(0);

  const visible = billboards.filter((b) => !dismissed.has(b.id));
  const current =
    visible.length > 0 ? visible[rotationOffset % visible.length] : null;

  useEffect(() => {
    if (billboards.length < 2) return;
    const cursorKey = `pawtrack:billboard:cursor:${placement}`;
    const next = Number(localStorage.getItem(cursorKey) ?? "0");
    setRotationOffset(Number.isFinite(next) ? next : 0);
    localStorage.setItem(
      cursorKey,
      String((Number.isFinite(next) ? next : 0) + 1),
    );
  }, [billboards, placement]);

  useEffect(() => {
    if (!current) return;
    trackProductEvent("BillboardImpression", {
      source: "billboard",
      billboardId: current.id,
      placement,
    });
    trackDelivery(current.id, "Impression");
  }, [current, placement]);

  const dismiss = (id: string) => {
    trackProductEvent("BillboardDismissed", {
      source: "billboard",
      billboardId: id,
      placement,
    });
    setDismissedState((prev) => new Set([...prev, id]));
  };

  if (!current) return null;

  return (
    <div className={className} data-billboard-placement={placement}>
      <AnimatePresence mode="wait">
        <BillboardCard
          key={current.id}
          bill={current}
          placement={placement}
          onDismiss={() => dismiss(current.id)}
        />
      </AnimatePresence>
    </div>
  );
}
