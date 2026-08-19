import { useState, useEffect } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { useBillboards } from "../hooks/useBillboards";
import type { BillboardDto, BillboardPlacement } from "../api/billboardsApi";

interface BillboardBannerProps {
  placement: BillboardPlacement;
  className?: string;
}

const DISMISS_KEY = (id: string) => `pawtrack:billboard:dismissed:${id}`;
const DISMISS_TTL_MS = 24 * 60 * 60_000; // 24h — persists across tab closes

function isDismissed(id: string): boolean {
  try {
    const raw = localStorage.getItem(DISMISS_KEY(id));
    if (!raw) return false;
    if (Date.now() > Number(raw)) {
      localStorage.removeItem(DISMISS_KEY(id));
      return false;
    }
    return true;
  } catch {
    return false;
  }
}
function setDismissed(id: string) {
  try {
    localStorage.setItem(DISMISS_KEY(id), String(Date.now() + DISMISS_TTL_MS));
  } catch {
    /* ignore */
  }
}

function BillboardCard({
  bill,
  onDismiss,
}: {
  bill: BillboardDto;
  onDismiss: () => void;
}) {
  const handleCta = () => {
    if (!bill.ctaUrl) return;
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
      {bill.imageUrl && (
        <img
          src={bill.imageUrl}
          alt={bill.title}
          className="h-28 w-full object-cover"
          loading="lazy"
        />
      )}

      {/* Content */}
      <div className={`px-4 py-3 space-y-1.5 ${!bill.imageUrl ? "pt-4" : ""}`}>
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
 * Dismissals persist 24h in localStorage — survive tab closes.
 */
export function BillboardBanner({
  placement,
  className = "",
}: BillboardBannerProps) {
  const { data: billboards = [] } = useBillboards(placement);
  const [dismissed, setDismissedState] = useState<Set<string>>(new Set());

  // Sync localStorage on mount — filter already-dismissed billboards
  useEffect(() => {
    const preFiltered = new Set<string>();
    billboards.forEach((b) => {
      if (isDismissed(b.id)) preFiltered.add(b.id);
    });
    if (preFiltered.size > 0) setDismissedState(preFiltered);
  }, [billboards]);

  const visible = billboards.filter((b) => !dismissed.has(b.id));
  const current = visible[0] ?? null;

  const dismiss = (id: string) => {
    setDismissed(id);
    setDismissedState((prev) => new Set([...prev, id]));
  };

  if (!current) return null;

  return (
    <div className={className}>
      <AnimatePresence mode="wait">
        <BillboardCard
          key={current.id}
          bill={current}
          onDismiss={() => dismiss(current.id)}
        />
      </AnimatePresence>
    </div>
  );
}
