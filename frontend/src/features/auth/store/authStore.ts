import { create } from "zustand";
import type { UserRole } from "../api/authApi";
import { setAuthenticatedUser, clearAuthenticatedUser } from "@/shared/lib/telemetry";

export interface AuthUser {
  id: string;
  name: string;
  email: string;
  /** Decoded from the signed JWT — not trusted from the API response body. */
  role: UserRole;
  isAdmin: boolean;
}

interface AuthState {
  isAuthenticated: boolean;
  /** True while the initial silent refresh is in-flight on page load. */
  isInitializing: boolean;
  user: AuthUser | null;
  /** Access token lives IN MEMORY only — never in localStorage. */
  accessToken: string | null;
  setAuth: (user: AuthUser, token: string) => void;
  clearAuth: () => void;
  setInitialized: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  isAuthenticated: false,
  isInitializing: true,
  user: null,
  accessToken: null,
  setAuth: (user, accessToken) => {
    setAuthenticatedUser(user.id);
    set({ isAuthenticated: true, isInitializing: false, user, accessToken });
  },
  clearAuth: () => {
    clearAuthenticatedUser();
    set({
      isAuthenticated: false,
      isInitializing: false,
      user: null,
      accessToken: null,
    });
  },
  setInitialized: () => set({ isInitializing: false }),
}));
