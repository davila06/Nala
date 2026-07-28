import { RouterProvider } from "react-router-dom";
import { QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { Toaster } from "sonner";
import { router } from "./app/routes";
import { queryClient } from "./app/providers";
import { useTrackLocation } from "@/features/locations/hooks/useTrackLocation";
import { useAlertPreference } from "@/features/locations/hooks/useAlertPreference";
import { PWAInstallBanner } from "@/shared/ui/PWAInstallBanner";
import { OfflineIndicator } from "@/shared/ui/OfflineIndicator";
import { CookieConsentBanner } from "@/shared/ui/CookieConsentBanner";
import { useAuthInit } from "@/features/auth/hooks/useAuthInit";

function LocationTracker() {
  const { receiveNearbyAlerts } = useAlertPreference();
  useTrackLocation({ receiveNearbyAlerts });
  return null;
}

function AuthInitializer() {
  useAuthInit();
  return null;
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthInitializer />
      <LocationTracker />
      <RouterProvider router={router} />
      <Toaster
        position="top-right"
        richColors
        closeButton
        toastOptions={{
          classNames: {
            toast: "font-body text-sm rounded-xl border shadow-md",
            title: "font-semibold",
            description: "text-xs opacity-80",
          },
        }}
      />
      <ReactQueryDevtools initialIsOpen={false} />
      <PWAInstallBanner />
      <OfflineIndicator />
      <CookieConsentBanner />
    </QueryClientProvider>
  );
}
