export type ConsentState = "accepted" | "rejected" | null;

const STORAGE_KEY = "pawtrack_cookie_consent";

export function getCookieConsent(): ConsentState {
  try {
    return (localStorage.getItem(STORAGE_KEY) as ConsentState) ?? null;
  } catch {
    return null;
  }
}
