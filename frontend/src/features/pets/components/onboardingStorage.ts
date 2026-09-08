const STORAGE_KEY = "pawtrack_onboarding_done";

export function shouldShowOnboarding(): boolean {
  return !localStorage.getItem(STORAGE_KEY);
}

export function markOnboardingDone(): void {
  localStorage.setItem(STORAGE_KEY, "1");
}
