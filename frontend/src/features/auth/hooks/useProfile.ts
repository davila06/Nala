import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { authApi } from "../api/authApi";
import { useAuthStore } from "../store/authStore";

export function useMyProfile() {
  return useQuery({
    queryKey: ["auth", "me"],
    queryFn: authApi.getMyProfile,
    staleTime: 5 * 60 * 1000,
  });
}

export function useUpdateProfile() {
  const queryClient = useQueryClient();
  const setAuth = useAuthStore((s) => s.setAuth);
  const user = useAuthStore((s) => s.user);
  const accessToken = useAuthStore((s) => s.accessToken);

  return useMutation({
    mutationFn: (data: { name: string }) => authApi.updateProfile(data),
    onSuccess: (_data, variables) => {
      if (user && accessToken) {
        setAuth({ ...user, name: variables.name }, accessToken);
      }
      void queryClient.invalidateQueries({ queryKey: ["auth", "me"] });
    },
  });
}

export function useChangePassword() {
  return useMutation({
    mutationFn: (data: { currentPassword: string; newPassword: string }) => authApi.changePassword(data),
  });
}

export function useDeleteAccount() {
  const clearAuth = useAuthStore((s) => s.clearAuth);

  return useMutation({
    mutationFn: (data: { confirmPassword: string }) => authApi.deleteAccount(data),
    onSuccess: () => {
      clearAuth();
    },
  });
}

export function useGrantHealthDataConsent() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => authApi.grantHealthDataConsent(),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["auth", "me"] });
    },
  });
}

/** Triggers a browser download of the user's full personal data export as JSON. */
export function useExportMyData() {
  return useMutation({
    mutationFn: async () => {
      const data = await authApi.exportMyData();
      const blob = new Blob([JSON.stringify(data, null, 2)], {
        type: "application/json",
      });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `pawtrack-mis-datos-${new Date().toISOString().slice(0, 10)}.json`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
    },
  });
}

export function useMySessions() {
  return useQuery({
    queryKey: ["auth", "sessions"],
    queryFn: authApi.getSessions,
    staleTime: 30_000,
  });
}

export function useRevokeMySession() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: authApi.revokeSession,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["auth", "sessions"] }),
  });
}

export function useMyTrustedDevices() {
  return useQuery({
    queryKey: ["auth", "trusted-devices"],
    queryFn: authApi.getTrustedDevices,
    staleTime: 30_000,
  });
}

export function useTrustCurrentDevice() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: authApi.trustDevice,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["auth", "trusted-devices"] }),
  });
}

export function useRevokeTrustedDevice() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: authApi.revokeTrustedDevice,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["auth", "trusted-devices"] }),
  });
}

export function useSetupMfa() {
  return useMutation({ mutationFn: authApi.setupMfa });
}

export function useEnableMfa() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: authApi.enableMfa,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["auth", "me"] }),
  });
}

export function useMfaStepUp() {
  const setAuth = useAuthStore((state) => state.setAuth);
  const user = useAuthStore((state) => state.user);
  return useMutation({
    mutationFn: authApi.stepUpMfa,
    onSuccess: ({ accessToken }) => {
      if (user) setAuth(user, accessToken);
    },
  });
}

export function useDisableMfa() {
  const clearAuth = useAuthStore((state) => state.clearAuth);
  return useMutation({
    mutationFn: authApi.disableMfa,
    onSuccess: () => clearAuth(),
  });
}
