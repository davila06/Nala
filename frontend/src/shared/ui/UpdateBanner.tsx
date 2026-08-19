import { useEffect, useState } from "react";
// @ts-expect-error — virtual module injected by vite-plugin-pwa at build time
import { useRegisterSW } from "virtual:pwa-register/react";

/**
 * Renders a dismissible bottom banner when a new Service Worker is waiting.
 * The user chooses when to reload — we never force an update mid-session.
 */
export function UpdateBanner() {
  const {
    needRefresh: [needRefresh],
    updateServiceWorker,
  } = useRegisterSW({
    onRegisteredSW(
      _swUrl: unknown,
      r: { update: () => Promise<void> } | undefined,
    ) {
      if (r) setInterval(() => void r.update(), 60 * 60_000);
    },
  });

  const [dismissed, setDismissed] = useState(false);

  // Reset dismissal when a new update becomes available
  useEffect(() => {
    if (needRefresh) setDismissed(false);
  }, [needRefresh]);

  if (!needRefresh || dismissed) return null;

  return (
    <div
      role="status"
      aria-live="polite"
      className="fixed bottom-20 left-1/2 z-[70] w-[calc(100%-2rem)] max-w-sm -translate-x-1/2 rounded-2xl border border-brand-200 bg-brand-50 px-4 py-3 shadow-lg flex items-center justify-between gap-3"
    >
      <p className="text-sm font-medium text-brand-800">
        🆕 Nueva versión disponible
      </p>
      <div className="flex gap-2 shrink-0">
        <button
          type="button"
          onClick={() => setDismissed(true)}
          className="rounded-lg px-2.5 py-1 text-xs text-brand-600 hover:bg-brand-100"
        >
          Después
        </button>
        <button
          type="button"
          onClick={() => void updateServiceWorker(true)}
          className="rounded-lg bg-brand-500 px-3 py-1 text-xs font-semibold text-white hover:bg-brand-600"
        >
          Actualizar
        </button>
      </div>
    </div>
  );
}
