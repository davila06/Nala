import { create } from 'zustand'
import type { UserRole } from '../api/authApi'

export interface AuthUser {
  id: string
  name: string
  email: string
  /** Decoded from the signed JWT — not trusted from the API response body. */
  role: UserRole
  isAdmin: boolean
}

interface AuthState {
  isAuthenticated: boolean
  user: AuthUser | null
  /** Access token lives IN MEMORY only — never in localStorage. */
  accessToken: string | null
  setAuth: (user: AuthUser, token: string) => void
  clearAuth: () => void
}

export const useAuthStore = create<AuthState>((set) => ({
  isAuthenticated: false,
  user: null,
  accessToken: null,
  setAuth: (user, accessToken) => set({ isAuthenticated: true, user, accessToken }),
  clearAuth: () => set({ isAuthenticated: false, user: null, accessToken: null }),
}))
