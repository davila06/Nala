import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { motion, AnimatePresence } from "framer-motion";

const STORAGE_KEY = "pawtrack_cookie_consent";

type ConsentState = "accepted" | "rejected" | null;

export function CookieConsentBanner() {
  const [consent, setConsent] = useState<ConsentState>(() => {
    try {
      return (localStorage.getItem(STORAGE_KEY) as ConsentState) ?? null;
    } catch {
      return null;
    }
  });

  const handleAccept = () => {
    localStorage.setItem(STORAGE_KEY, "accepted");
    setConsent("accepted");
  };

  const handleReject = () => {
    localStorage.setItem(STORAGE_KEY, "rejected");
    setConsent("rejected");
  };

  // If already decided, don't render
  if (consent !== null) return null;

  return (
    <AnimatePresence>
      <motion.div
        initial={{ y: 80, opacity: 0 }}
        animate={{ y: 0, opacity: 1 }}
        exit={{ y: 80, opacity: 0 }}
        transition={{ type: "spring", stiffness: 300, damping: 30 }}
        role="dialog"
        aria-modal="false"
        aria-label="Aviso de cookies y privacidad"
        className="fixed bottom-0 left-0 right-0 z-[9000] border-t border-sand-200 bg-white/95 px-4 py-4 shadow-2xl backdrop-blur-sm sm:bottom-4 sm:left-4 sm:right-auto sm:max-w-sm sm:rounded-2xl sm:border"
      >
        <p className="mb-3 text-sm leading-relaxed text-sand-700">
          <span className="mr-1" aria-hidden="true">
            🍪
          </span>
          Usamos cookies esenciales para el funcionamiento de la app y cookies
          de analítica (Application Insights) para mejorar el servicio.{" "}
          <Link
            to="/legal/politica-de-privacidad"
            className="font-semibold text-brand-600 underline underline-offset-2 hover:text-brand-700"
          >
            Ver política de privacidad
          </Link>
          .
        </p>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={handleAccept}
            className="flex-1 rounded-xl bg-brand-500 px-3 py-2.5 text-sm font-bold text-white hover:bg-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
          >
            Aceptar todo
          </button>
          <button
            type="button"
            onClick={handleReject}
            className="flex-1 rounded-xl border border-sand-300 px-3 py-2.5 text-sm font-semibold text-sand-700 hover:bg-sand-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sand-400"
          >
            Solo esenciales
          </button>
        </div>
      </motion.div>
    </AnimatePresence>
  );
}

/** Returns the stored consent value without causing a re-render. */
export function getCookieConsent(): ConsentState {
  try {
    return (localStorage.getItem(STORAGE_KEY) as ConsentState) ?? null;
  } catch {
    return null;
  }
}
