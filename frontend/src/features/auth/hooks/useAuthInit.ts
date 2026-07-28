import { useEffect, useRef } from "react";
import { authApi, decodeRoleFromJwt } from "../api/authApi";
import { useAuthStore } from "../store/authStore";

/**
 * Runs once on app mount.
 * Attempts a silent token refresh using the HttpOnly cookie.
 * Sets isInitializing=false when done (success or failure) so
 * AuthenticatedLayout can safely decide whether to redirect.
 */
export function useAuthInit() {
  const setAuth = useAuthStore((s) => s.setAuth);
  const clearAuth = useAuthStore((s) => s.clearAuth);
  const attempted = useRef(false);

  useEffect(() => {
    if (attempted.current) return;
    attempted.current = true;

    authApi
      .refresh()
      .then(({ data }) => {
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
      })
      .catch(() => {
        // No valid cookie — clear any stale state and mark initialization done
        clearAuth();
      });
  }, []); // eslint-disable-line react-hooks/exhaustive-deps
}
