import { useMutation } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { authApi, decodeRoleFromJwt } from "../api/authApi";
import { authenticateWithPasskey } from "../api/webauthn";
import { useAuthStore } from "../store/authStore";

export function getSafeReturnPath(returnTo?: string): string | undefined {
  if (!returnTo || !returnTo.startsWith("/") || returnTo.startsWith("//") || returnTo.startsWith("/login")) {
    return undefined;
  }
  return returnTo.includes("\\") ? undefined : returnTo;
}

export function useLogin(returnTo?: string) {
  const setAuth = useAuthStore((s) => s.setAuth);
  const navigate = useNavigate();

  return useMutation({
    mutationFn: authApi.login,
    onSuccess: ({ data }) => {
      const role = decodeRoleFromJwt(data.accessToken);
      setAuth(
        {
          id: data.user.id,
          name: data.user.name,
          email: data.user.email,
          role,
          isAdmin: data.user.isAdmin,
        },
        data.accessToken,
      );
      // Honour the ?return= param; fall back to role-based default
      const destination = getSafeReturnPath(returnTo) ?? (role === "Ally" ? "/allies/panel" : "/dashboard");
      void navigate(destination, { replace: true });
    },
  });
}

export function useRegister(returnTo?: string) {
  const navigate = useNavigate();
  const safeReturnPath = getSafeReturnPath(returnTo);

  return useMutation({
    mutationFn: authApi.register,
    onSuccess: () => {
      const query = new URLSearchParams({ registered: "true" });
      if (safeReturnPath) query.set("return", safeReturnPath);
      void navigate(`/login?${query.toString()}`);
    },
  });
}

export function usePasskeyLogin(returnTo?: string) {
  const setAuth = useAuthStore((s) => s.setAuth);
  const navigate = useNavigate();

  return useMutation({
    mutationFn: (email: string) => authenticateWithPasskey(email),
    onSuccess: ({ data }) => {
      const role = decodeRoleFromJwt(data.accessToken);
      setAuth(
        {
          id: data.user.id,
          name: data.user.name,
          email: data.user.email,
          role,
          isAdmin: data.user.isAdmin,
        },
        data.accessToken,
      );
      const destination = getSafeReturnPath(returnTo) ?? "/dashboard";
      void navigate(destination, { replace: true });
    },
  });
}

export function useForgotPassword() {
  return useMutation({
    mutationFn: authApi.forgotPassword,
  });
}

export function useResetPassword() {
  return useMutation({
    mutationFn: authApi.resetPassword,
  });
}

export function useLogout() {
  const clearAuth = useAuthStore((s) => s.clearAuth);
  const navigate = useNavigate();

  return useMutation({
    mutationFn: authApi.logout,
    onSettled: () => {
      clearAuth();
      void navigate("/login");
    },
  });
}
